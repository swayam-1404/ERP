using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace VHEI.Infrastructure;
public class DesignTimeFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<ErpDbContext>().UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__Erp") ?? "Host=127.0.0.1;Port=54329;Database=vhei;Username=vhei").Options);
}
