using Microsoft.EntityFrameworkCore;
using Sql.Models;

namespace Sql.Services
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.ToTable("user");

                entity.Property(e => e.Id)
                    .HasColumnName("id");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasMaxLength(255);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasColumnName("email")
                    .HasMaxLength(255);

                entity.Property(e => e.Password)
                    .IsRequired()
                    .HasColumnName("password");

                entity.Property(e => e.IsStaff)
                    .HasColumnName("isStaff")
                    .HasDefaultValue(false);

                entity.Property(e => e.IsAdmin)
                    .HasColumnName("isAdmin")
                    .HasDefaultValue(false);

                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.Property(e => e.RefreshToken)
                    .HasColumnName("refreshToken")
                    .HasMaxLength(500);

                entity.Property(e => e.RefreshTokenExpiryTime)
                    .HasColumnName("refreshTokenExpiryTime");

                entity.HasIndex(e => e.Email).IsUnique();
            });
        }
    }


    public class DatabaseService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DatabaseService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            RunMigrations();
        }

        private void RunMigrations()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var pending = db.Database.GetPendingMigrations().ToList();

                if (pending.Count > 0)
                {
                    Console.WriteLine($"⏳ Applying {pending.Count} pending migration(s)...");
                    db.Database.Migrate();
                    Console.WriteLine("✅ Migrations applied successfully");
                }
                else
                {
                    Console.WriteLine("✅ Database is up to date — no migrations needed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Migration failed: {ex.Message}");
                throw; 
            }
        }

        public AppDbContext CreateContext()
        {
            var scope = _scopeFactory.CreateScope();
            return scope.ServiceProvider.GetRequiredService<AppDbContext>();
        }
    }
}