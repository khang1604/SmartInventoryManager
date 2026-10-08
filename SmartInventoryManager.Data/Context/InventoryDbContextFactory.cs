using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartInventoryManager.Data.Context
{
    public sealed class InventoryDbContextFactory
        : IDesignTimeDbContextFactory<InventoryDbContext>
    {
        public InventoryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();

            optionsBuilder.UseSqlServer(
                "Server=.\\SQLEXPRESS;Database=SmartInventoryManagerDbDesign;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True");

            return new InventoryDbContext(optionsBuilder.Options);
        }
    }
}
