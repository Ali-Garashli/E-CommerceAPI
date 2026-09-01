using ECommerceAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Data;

public class DataContext : DbContext
{
    public DbSet<AppUser> Users { get; set; } = null!;

    public DbSet<HttpLog> HttpLogs { get; set; } = null!;

    public DbSet<Product> Products { get; set; }
    public DbSet<Category> Categories { get; set; }

    public DbSet<RateLimitPolicy> RateLimitPolicies { get; set; }
    public DbSet<RateLimitCounter> RateLimitCounters { get; set; }

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public DataContext(DbContextOptions<DataContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // APP USER
        modelBuilder.Entity<AppUser>()
                    .HasIndex(u => u.Email)
                    .IsUnique();

        // PRODUCT
        modelBuilder.Entity<Product>()
                    .ToTable(t => t.HasCheckConstraint("CK_Product_Stock_Nonnegative",
                                                       "stock >= 0"));

        modelBuilder.Entity<Product>()
                    .ToTable(t => t.HasCheckConstraint("CK_Product_Price_Positive",
                                                       "price > 0"));

        // use transaction id to detect optimistic concurrency conflicts
        modelBuilder.Entity<Product>()
                    .Property<uint>("xmin")
                    .HasColumnType("xid")
                    .IsRowVersion();

        // CATEGORY
        // seed categories
        modelBuilder.Entity<Category>()
                    .HasData(new Category { Id = 1, Name = "Electronics" },
                             new Category { Id = 2, Name = "Clothing" },
                             new Category { Id = 3, Name = "Books" },
                             new Category { Id = 4, Name = "Home & Kitchen" },
                             new Category { Id = 5, Name = "Sports & Outdoors" }
                         );

        // ORDER
        // order *-1 product
        modelBuilder.Entity<OrderItem>()
                    .HasOne(oi => oi.Product)
                    .WithMany();

        // RATE LIMIT
        // make counter unique per policy/client/window
        modelBuilder.Entity<RateLimitCounter>()
                    .HasIndex(i => new {
                        i.PolicyName,
                        i.Client,
                        i.WindowStart
                    })
                    .IsUnique();

        modelBuilder.Entity<RateLimitPolicy>()
                    .HasIndex(i => i.Name)
                    .IsUnique();

        // seed policies
        modelBuilder.Entity<RateLimitPolicy>()
                    .HasData
                    (
                        new RateLimitPolicy
                        {
                            Id = 1,
                            Name = "ProductReadPolicy",
                            PermitLimit = 100,
                            WindowSeconds = 60,
                            Enabled = true
                        },
                        new RateLimitPolicy
                        {
                            Id = 2,
                            Name = "ProductWritePolicy",
                            PermitLimit = 20,
                            WindowSeconds = 60,
                            Enabled = true
                        },
                        new RateLimitPolicy
                        {
                            Id = 3,
                            Name = "UserReadPolicy",
                            PermitLimit = 100,
                            WindowSeconds = 60,
                            Enabled = true
                        },
                        new RateLimitPolicy
                        {
                            Id = 4,
                            Name = "UserWritePolicy",
                            PermitLimit = 20,
                            WindowSeconds = 60,
                            Enabled = true
                        },
                        new RateLimitPolicy
                        {
                            Id = 5,
                            Name = "LoginPolicy",
                            PermitLimit = 5,
                            WindowSeconds = 300,
                            Enabled = true
                        },
                        new RateLimitPolicy
                        {
                            Id = 6,
                            Name = "RegisterPolicy",
                            PermitLimit = 20,
                            WindowSeconds = 120,
                            Enabled = true
                        },
                        new RateLimitPolicy
                        {
                            Id = 7,
                            Name = "OrderReadPolicy",
                            PermitLimit = 30,
                            WindowSeconds = 60,
                            Enabled = true
                        },
                        new RateLimitPolicy
                        {
                            Id = 8,
                            Name = "OrderWritePolicy",
                            PermitLimit = 5,
                            WindowSeconds = 60,
                            Enabled = true
                        },
                        new RateLimitPolicy
                        {
                            Id = 9,
                            Name = "OrderPatchPolicy",
                            PermitLimit = 10,
                            WindowSeconds = 60,
                            Enabled = true
                        }
                    );

    }
}
