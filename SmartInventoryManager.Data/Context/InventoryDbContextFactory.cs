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

            var connectionString = Environment.GetEnvironmentVariable("SMART_INVENTORY_CONNECTION_STRING");
            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = "Server=localhost;Database=SmartInventoryManagerDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";
            optionsBuilder.UseSqlServer(connectionString);

            return new InventoryDbContext(optionsBuilder.Options);
        }
    }
}
