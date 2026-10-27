using Microsoft.EntityFrameworkCore;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Business.Events;
using SmartInventoryManager.Business.Interfaces;
using SmartInventoryManager.Business.Inventory;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;
using SmartInventoryManager.Data.Queries;
using InventoryEntity = SmartInventoryManager.Data.Entities.Inventory;

namespace SmartInventoryManager.Business.Services;

public sealed class InventoryService : IInventoryService
{
    private const int MaxReasonLength = 500;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;
    private readonly StockMutationService _stockMutation;
    private readonly StockEventPublisher _publisher;

    // Khởi tạo service tồn kho với phân quyền và bộ phát sự kiện nghiệp vụ.
    public InventoryService(InventoryDbContext dbContext, AuthorizationService authorization, StockEventPublisher? publisher = null)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
        _stockMutation = new StockMutationService(_dbContext);
        _publisher = publisher ?? new StockEventPublisher();
    }

    public event EventHandler<InventoryChangedEvent>? InventoryChanged
    { add => _publisher.InventoryChanged += value; remove => _publisher.InventoryChanged -= value; }

    // Ghi nhận biến động tồn cho một dòng phiếu trong giao dịch đang xử lý.
    internal async Task<InventoryChangedEvent> PostReceiptLineAsync(UserSession session, int warehouseId, int productId, int quantity,
        bool import, int receiptId, string? note, CancellationToken ct)
    {
        if (_dbContext.Database.IsRelational() && _dbContext.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Stock posting requires the receipt transaction.");
        if (!await _authorization.CanCompleteReceiptAsync(session, warehouseId))
            throw new UnauthorizedAccessException("Bạn không có quyền ghi nhận tồn kho này.");
        var stock = await _stockMutation.GetOrCreateAsync(warehouseId, productId, ct);
        var before = stock.Quantity;
        stock = import ? await _stockMutation.IncreaseAsync(warehouseId, productId, quantity, ct)
            : await _stockMutation.DecreaseAsync(warehouseId, productId, quantity, ct);
        var type = import ? InventoryTransactionTypes.Import : InventoryTransactionTypes.Export;
        _dbContext.InventoryTransactions.Add(new InventoryTransaction { ProductId = productId, WarehouseId = warehouseId,
            TransactionType = type, Quantity = quantity, ReferenceType = import ? InventoryTransactionTypes.ImportReceipt : InventoryTransactionTypes.ExportReceipt,
            ReferenceId = receiptId, CreatedBy = session.CurrentUser!.UserId, CreatedAt = DateTime.Now, Note = note,
            BeforeQuantity = before, AfterQuantity = stock.Quantity, SignedDifference = stock.Quantity - before });
        return new InventoryChangedEvent(productId, warehouseId, before, stock.Quantity, type, session.CurrentUser!.UserId);
    }

    // Lấy tồn kho theo kho và phạm vi truy cập của người dùng.
    public async Task<IReadOnlyList<InventoryDto>> GetByWarehouseAsync(
        UserSession session,
        int warehouseId,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateWarehouseId(warehouseId);
        if (!await _authorization.CanViewWarehouseAsync(session, warehouseId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem tồn kho tại kho này.");
        }

        return await _dbContext.Inventories
            .AsNoTracking()
            .Where(inventory => inventory.WarehouseId == warehouseId)
            .OrderBy(inventory => inventory.Product.ProductCode)
            .Select(inventory => new InventoryDto(
                inventory.InventoryId,
                inventory.ProductId,
                inventory.Product.ProductCode,
                inventory.Product.ProductName,
                inventory.Product.Unit,
                inventory.WarehouseId,
                inventory.Quantity,
                inventory.Product.ReorderPoint,
                inventory.Product.MaximumStock,
                StockStatusCalculator.Calculate(
                    inventory.Quantity,
                    inventory.Product.ReorderPoint,
                    inventory.Product.MaximumStock),
                inventory.UpdatedAt))
            .ToListAsync(cancellationToken);
    }

    // Tra cứu lịch sử biến động tồn theo điều kiện lọc và quyền truy cập.
    public async Task<IReadOnlyList<InventoryHistoryDto>> GetHistoryAsync(
        UserSession session,
        int warehouseId,
        int? productId = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        ValidateSession(session);
        ValidateWarehouseId(warehouseId);
        if (productId.HasValue && productId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
        if (from.HasValue && to.HasValue && to.Value.Date < from.Value.Date)
        {
            throw new ArgumentException("Khoảng ngày không hợp lệ.", nameof(to));
        }
        if (!await _authorization.CanViewWarehouseAsync(session, warehouseId, cancellationToken))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền xem lịch sử tồn kho tại kho này.");
        }

        var query = _dbContext.InventoryTransactions
            .AsNoTracking()
            .Where(transaction => transaction.WarehouseId == warehouseId)
            .AsQueryable();
        if (productId.HasValue)
        {
            query = query.Where(transaction => transaction.ProductId == productId.Value);
        }
        if (from.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt >= from.Value.Date);
        }
        if (to.HasValue)
        {
            query = query.Where(transaction => transaction.CreatedAt < to.Value.Date.AddDays(1));
        }
        return await query
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Select(transaction => new InventoryHistoryDto(
                transaction.TransactionId,
                transaction.ProductId,
                transaction.Product.ProductCode,
                transaction.Product.ProductName,
                transaction.WarehouseId,
                transaction.TransactionType,
                transaction.Quantity,
                transaction.ReferenceType,
                transaction.ReferenceId,
                transaction.Creator.FullName,
                transaction.CreatedAt,
                transaction.Note)
            {
                BeforeQuantity = transaction.BeforeQuantity, AfterQuantity = transaction.AfterQuantity, SignedDifference = transaction.SignedDifference
            })
            .ToListAsync(cancellationToken);
    }

    // Điều chỉnh tồn có kiểm tra quyền và cơ chế thử lại khi gặp deadlock.
    public Task AdjustAsync(UserSession session, int warehouseId, int productId, int quantityDelta, string reason, CancellationToken cancellationToken = default)
    {
        var committed = false;
        return DeadlockRetry.RunAsync(() => AdjustCoreAsync(session, warehouseId, productId, quantityDelta, reason, cancellationToken, () => committed = true),
            () => _dbContext.ChangeTracker.Clear(), () => committed, cancellationToken);
    }

    // Ghi số tồn sau điều chỉnh cùng lý do, lịch sử và cảnh báo trong giao dịch.
    private async Task AdjustCoreAsync(
        UserSession session,
        int warehouseId,
        int productId,
        int quantityDelta,
        string reason,
        CancellationToken cancellationToken, Action onCommitted)
    {
        ValidateSession(session);
        ValidateWarehouseId(warehouseId);
        ValidateProductId(productId);
        if (quantityDelta == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantityDelta), "Số lượng điều chỉnh phải khác 0.");
        }
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > MaxReasonLength)
        {
            throw new ArgumentException("Lý do điều chỉnh là bắt buộc và không quá 500 ký tự.", nameof(reason));
        }
        if (!await _authorization.CanAdjustInventoryAsync(session, warehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền điều chỉnh tồn kho tại kho này.");
        }
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var productExists = await _dbContext.Products.AnyAsync(
            product => product.ProductId == productId && product.IsActive,
            cancellationToken);
        if (!productExists)
        {
            throw new KeyNotFoundException("Sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }

        var inventory = await _stockMutation.GetOrCreateAsync(warehouseId, productId, cancellationToken);
        var previousQuantity = inventory.Quantity;
        var newQuantity = checked(inventory.Quantity + quantityDelta);
        if (newQuantity < 0)
        {
            throw new InvalidOperationException("Số lượng điều chỉnh không được làm tồn kho âm.");
        }
        inventory.Quantity = newQuantity;
        inventory.UpdatedAt = DateTime.Now;
        _dbContext.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            TransactionType = InventoryTransactionTypes.Adjustment,
            Quantity = Math.Abs(quantityDelta),
            ReferenceType = InventoryTransactionTypes.Manual,
            CreatedBy = session.CurrentUser!.UserId,
            CreatedAt = DateTime.Now,
            Note = reason.Trim(), BeforeQuantity = previousQuantity, AfterQuantity = newQuantity, SignedDifference = quantityDelta
        });
        await new StockAlertService(_dbContext, _authorization, _publisher)
            .RefreshAsync(productId, warehouseId, cancellationToken, persistChanges: false, publishEvent: false);
        await StockWorkflowEffects.CommitAsync(_dbContext, transaction, session, "ADJUST", "Product", productId, _publisher, onCommitted, cancellationToken);
        _publisher.Publish(new InventoryChangedEvent(
            productId,
            warehouseId,
            previousQuantity,
            newQuantity,
            InventoryTransactionTypes.Adjustment,
            session.CurrentUser!.UserId));
    }

    // Chuyển hàng giữa hai kho với cơ chế thử lại khi gặp deadlock.
    public Task TransferAsync(UserSession session, int sourceWarehouseId, int destinationWarehouseId, int productId, int quantity, string note, CancellationToken cancellationToken = default)
    {
        var committed = false;
        return DeadlockRetry.RunAsync(() => TransferCoreAsync(session, sourceWarehouseId, destinationWarehouseId, productId, quantity, note, cancellationToken, () => committed = true),
            () => _dbContext.ChangeTracker.Clear(), () => committed, cancellationToken);
    }

    // Giảm tồn kho nguồn và tăng tồn kho đích cùng lịch sử trong một giao dịch.
    private async Task TransferCoreAsync(
        UserSession session,
        int sourceWarehouseId,
        int destinationWarehouseId,
        int productId,
        int quantity,
        string note,
        CancellationToken cancellationToken, Action onCommitted)
    {
        ValidateSession(session);
        ValidateWarehouseId(sourceWarehouseId);
        ValidateWarehouseId(destinationWarehouseId);
        ValidateProductId(productId);
        if (sourceWarehouseId == destinationWarehouseId)
        {
            throw new ArgumentException("Kho nguồn và kho đích phải khác nhau.");
        }
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Số lượng chuyển phải lớn hơn 0.");
        }
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("Lý do chuyển kho là bắt buộc.", nameof(note));
        }
        if (note.Trim().Length > 500)
        {
            throw new ArgumentException("Ghi chú chuyển kho không quá 500 ký tự.", nameof(note));
        }
        if (!await _authorization.CanTransferInventoryAsync(session, sourceWarehouseId, destinationWarehouseId))
        {
            throw new UnauthorizedAccessException("Bạn không có quyền chuyển hàng giữa hai kho này.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var productExists = await _dbContext.Products.AnyAsync(
            product => product.ProductId == productId && product.IsActive,
            cancellationToken);
        if (!productExists)
        {
            throw new KeyNotFoundException("Sản phẩm không tồn tại hoặc đã ngừng hoạt động.");
        }
        var source = await _dbContext.Inventories
            .SingleOrDefaultAsync(item => item.WarehouseId == sourceWarehouseId && item.ProductId == productId, cancellationToken)
            ?? throw new InvalidOperationException("Sản phẩm chưa có tồn tại kho nguồn.");
        if (source.Quantity < quantity)
        {
            throw new InvalidOperationException("Tồn kho tại kho nguồn không đủ để chuyển.");
        }
        var previousSourceQuantity = source.Quantity;
        var destinationInventory = await _dbContext.Inventories
            .SingleOrDefaultAsync(item => item.WarehouseId == destinationWarehouseId && item.ProductId == productId, cancellationToken);
        var previousDestinationQuantity = destinationInventory?.Quantity ?? 0;
        await _stockMutation.DecreaseAsync(sourceWarehouseId, productId, quantity, cancellationToken);
        destinationInventory ??= await _stockMutation.GetOrCreateAsync(destinationWarehouseId, productId, cancellationToken);
        destinationInventory.Quantity = checked(destinationInventory.Quantity + quantity);
        destinationInventory.UpdatedAt = DateTime.Now;
        var now = DateTime.Now;
        var transferReference = await TransferReferenceAllocator.NextAsync(_dbContext, cancellationToken);
        _dbContext.InventoryTransactions.AddRange(
            new InventoryTransaction
            {
                ProductId = productId,
                WarehouseId = sourceWarehouseId,
                TransactionType = InventoryTransactionTypes.TransferOut,
                ReferenceId = transferReference, BeforeQuantity = previousSourceQuantity,
                AfterQuantity = previousSourceQuantity - quantity, SignedDifference = -quantity,
                Quantity = quantity,
                ReferenceType = InventoryTransactionTypes.Manual,
                CreatedBy = session.CurrentUser!.UserId,
                CreatedAt = now,
                Note = note.Trim()
            },
            new InventoryTransaction
            {
                ProductId = productId,
                WarehouseId = destinationWarehouseId,
                TransactionType = InventoryTransactionTypes.TransferIn,
                ReferenceId = transferReference, BeforeQuantity = previousDestinationQuantity,
                AfterQuantity = checked(previousDestinationQuantity + quantity), SignedDifference = quantity,
                Quantity = quantity,
                ReferenceType = InventoryTransactionTypes.Manual,
                CreatedBy = session.CurrentUser!.UserId,
                CreatedAt = now,
                Note = note.Trim()
            });
        var alertService = new StockAlertService(_dbContext, _authorization, _publisher);
        await alertService.RefreshAsync(productId, sourceWarehouseId, cancellationToken, persistChanges: false, publishEvent: false);
        await alertService.RefreshAsync(productId, destinationWarehouseId, cancellationToken, persistChanges: false, publishEvent: false);
        await StockWorkflowEffects.CommitAsync(_dbContext, transaction, session, "TRANSFER", "Product", productId, _publisher, onCommitted, cancellationToken);
        _publisher.Publish(new InventoryChangedEvent(
            productId,
            sourceWarehouseId,
            previousSourceQuantity,
            previousSourceQuantity - quantity,
            InventoryTransactionTypes.TransferOut,
            session.CurrentUser!.UserId));
        _publisher.Publish(new InventoryChangedEvent(
            productId,
            destinationWarehouseId,
            previousDestinationQuantity,
            previousDestinationQuantity + quantity,
            InventoryTransactionTypes.TransferIn,
            session.CurrentUser!.UserId));
    }

    // Lấy bản ghi tồn theo sản phẩm và kho hoặc khởi tạo nếu chưa có.
    private async Task<InventoryEntity> GetOrCreateInventoryAsync(int warehouseId, int productId, CancellationToken cancellationToken)
    {
        var inventory = await _dbContext.Inventories
            .SingleOrDefaultAsync(item => item.WarehouseId == warehouseId && item.ProductId == productId, cancellationToken);
        if (inventory is not null)
        {
            return inventory;
        }
        inventory = new InventoryEntity
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            Quantity = 0,
            UpdatedAt = DateTime.Now
        };
        _dbContext.Inventories.Add(inventory);
        return inventory;
    }

    // Kiểm tra mã định danh sản phẩm là số dương.
    private static void ValidateProductId(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(productId));
        }
    }

    // Kiểm tra mã định danh kho là số dương.
    private static void ValidateWarehouseId(int warehouseId)
    {
        if (warehouseId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(warehouseId));
        }
    }

    // Kiểm tra phiên đăng nhập hợp lệ trước khi xử lý yêu cầu.
    private static void ValidateSession(UserSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentUser is null)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để thực hiện thao tác.");
        }
    }
}
