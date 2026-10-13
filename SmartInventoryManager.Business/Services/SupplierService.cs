using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using SmartInventoryManager.Business.Authorization;
using SmartInventoryManager.Business.DTOs;
using SmartInventoryManager.Data.Context;
using SmartInventoryManager.Data.Entities;

namespace SmartInventoryManager.Business.Services;

public sealed class SupplierService
{
    private const int MaxLookupResults = 200;
    private readonly InventoryDbContext _dbContext;
    private readonly AuthorizationService _authorization;

    public SupplierService(InventoryDbContext dbContext, AuthorizationService authorization)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetActiveAsync(
        UserSession session,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!session.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Bạn cần đăng nhập để tra cứu nhà cung cấp.");
        }

        var query = _dbContext.Suppliers.AsNoTracking().Where(supplier => supplier.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var value = search.Trim();
            query = query.Where(supplier => supplier.SupplierCode.Contains(value) || supplier.SupplierName.Contains(value));
        }
        return await query
            .OrderBy(supplier => supplier.SupplierCode)
            .Take(MaxLookupResults)
            .Select(supplier => new LookupItemDto(supplier.SupplierId, supplier.SupplierCode, supplier.SupplierName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierDto>> GetAsync(UserSession session, bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated(session);
        var query = _dbContext.Suppliers.AsNoTracking();
        if (!includeInactive) query = query.Where(item => item.IsActive);
        return await query.OrderBy(item => item.SupplierCode)
            .Select(item => new SupplierDto(item.SupplierId, item.SupplierCode, item.SupplierName, item.Phone, item.Email, item.Address, item.TaxCode, item.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CreateAsync(UserSession session, SupplierRequestDto request, CancellationToken cancellationToken = default)
    {
        EnsureCanManage(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        Validate(request);
        var code = request.SupplierCode.Trim();
        if (await _dbContext.Suppliers.AnyAsync(item => item.SupplierCode == code, cancellationToken)) throw new InvalidOperationException("Mã nhà cung cấp đã tồn tại.");
        var entity = new Supplier { SupplierCode = code, SupplierName = request.SupplierName.Trim(), Phone = request.Phone?.Trim(), Email = request.Email?.Trim(), Address = request.Address?.Trim(), TaxCode = request.TaxCode?.Trim(), IsActive = request.IsActive, CreatedAt = DateTime.Now };
        _dbContext.Suppliers.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity.SupplierId;
    }

    public async Task UpdateAsync(UserSession session, int supplierId, SupplierRequestDto request, CancellationToken cancellationToken = default)
    {
        ValidateId(supplierId, nameof(supplierId));
        EnsureCanManage(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        Validate(request);
        var entity = await _dbContext.Suppliers.SingleOrDefaultAsync(item => item.SupplierId == supplierId, cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");
        var code = request.SupplierCode.Trim();
        if (await _dbContext.Suppliers.AnyAsync(item => item.SupplierId != supplierId && item.SupplierCode == code, cancellationToken)) throw new InvalidOperationException("Mã nhà cung cấp đã tồn tại.");
        entity.SupplierCode = code; entity.SupplierName = request.SupplierName.Trim(); entity.Phone = request.Phone?.Trim(); entity.Email = request.Email?.Trim(); entity.Address = request.Address?.Trim(); entity.TaxCode = request.TaxCode?.Trim(); entity.IsActive = request.IsActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(UserSession session, int supplierId, bool isActive, CancellationToken cancellationToken = default)
    {
        ValidateId(supplierId, nameof(supplierId));
        EnsureCanManage(session);
        await _authorization.EnsureActiveActorAsync(session, cancellationToken);
        var entity = await _dbContext.Suppliers.SingleOrDefaultAsync(item => item.SupplierId == supplierId, cancellationToken) ?? throw new KeyNotFoundException("Không tìm thấy nhà cung cấp.");
        entity.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private void EnsureCanManage(UserSession session) { if (!_authorization.CanManageSupplier(session)) throw new UnauthorizedAccessException("Bạn không có quyền quản lý nhà cung cấp."); }
    private static void EnsureAuthenticated(UserSession session) { if (session is null || !session.IsAuthenticated) throw new UnauthorizedAccessException("Bạn cần đăng nhập."); }
    private static void Validate(SupplierRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.SupplierCode) || request.SupplierCode.Trim().Length > 50)
            throw new ArgumentException("Mã nhà cung cấp là bắt buộc và không quá 50 ký tự.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.SupplierName) || request.SupplierName.Trim().Length > 200)
            throw new ArgumentException("Tên nhà cung cấp là bắt buộc và không quá 200 ký tự.", nameof(request));
        if (request.Phone?.Trim().Length > 20)
            throw new ArgumentException("Số điện thoại không quá 20 ký tự.", nameof(request));
        if (request.Email?.Trim() is { Length: > 100 } email ||
            (!string.IsNullOrWhiteSpace(request.Email) && !MailAddress.TryCreate(request.Email.Trim(), out _)))
            throw new ArgumentException("Email không hợp lệ hoặc vượt quá 100 ký tự.", nameof(request));
        if (request.Address?.Trim().Length > 255)
            throw new ArgumentException("Địa chỉ không quá 255 ký tự.", nameof(request));
        if (request.TaxCode?.Trim().Length > 50)
            throw new ArgumentException("Mã số thuế không quá 50 ký tự.", nameof(request));
    }

    private static void ValidateId(int id, string parameterName)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(parameterName);
    }
}
