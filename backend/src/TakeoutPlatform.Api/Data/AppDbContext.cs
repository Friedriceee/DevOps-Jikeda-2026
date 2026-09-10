using Microsoft.EntityFrameworkCore;

namespace TakeoutPlatform.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 第一周由「后端功能」同学补上实体和 DbSet：
    //   public DbSet<Merchant> Merchants => Set<Merchant>();
    //   public DbSet<Dish> Dishes => Set<Dish>();
    //
    // 实体放在 Features/<功能>/ 下，在这里注册 DbSet，然后：
    //   dotnet ef migrations add <Name>
    //   dotnet ef database update

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
