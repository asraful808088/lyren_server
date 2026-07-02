using Microsoft.EntityFrameworkCore;
using Sql.Models;

namespace Sql.Services
{
    public class ProductContext : DbContext
    {
        public ProductContext(DbContextOptions<ProductContext> options)
            : base(options)
        {
        }

     
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductCategory> ProductCategories { get; set; }
        public DbSet<ProductCollection> ProductCollections { get; set; }
        public DbSet<ProductColor> ProductColors { get; set; }
        public DbSet<ProductSize> ProductSizes { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Similarity> Similarities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("product");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasColumnName("name")
                    .HasMaxLength(255);
                entity.Property(e => e.Count)
                    .HasColumnName("count")
                    .HasDefaultValue(0);
                entity.Property(e => e.MiniDesc)
                    .IsRequired()
                    .HasColumnName("mini_desc")
                    .HasMaxLength(255);
                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasColumnName("description");
                entity.Property(e => e.CareDetails)
                    .HasColumnName("care_details");
                entity.Property(e => e.Price)
                    .HasColumnName("price")
                    .HasPrecision(10, 2);
                entity.Property(e => e.Discount)
                    .HasColumnName("discount")
                    .HasPrecision(5, 2)
                    .HasDefaultValue(0);
                entity.Property(e => e.InjectorUser)
                    .IsRequired()
                    .HasColumnName("injector_user")
                    .HasMaxLength(255);
                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                
                entity.HasMany(p => p.Categories)
                    .WithOne(pc => pc.Product)
                    .HasForeignKey(pc => pc.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Collections)
                    .WithOne(pc => pc.Product)
                    .HasForeignKey(pc => pc.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Colors)
                    .WithOne(pc => pc.Product)
                    .HasForeignKey(pc => pc.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Sizes)
                    .WithOne(ps => ps.Product)
                    .HasForeignKey(ps => ps.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Images)
                    .WithOne(pi => pi.Product)
                    .HasForeignKey(pi => pi.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Reviews)
                    .WithOne(r => r.Product)
                    .HasForeignKey(r => r.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.Similarities)
                    .WithOne(s => s.Product)
                    .HasForeignKey(s => s.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(p => p.SimilarToMe)
                    .WithOne(s => s.SimilarProduct)
                    .HasForeignKey(s => s.SimilarProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        
            modelBuilder.Entity<ProductCategory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("product_category");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("type")
                    .HasMaxLength(50);
                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.ProductId);
            });

            
            modelBuilder.Entity<ProductCollection>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("product_collection");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.Collection)
                    .IsRequired()
                    .HasColumnName("collection")
                    .HasMaxLength(50);
                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.ProductId);
            });

            
            modelBuilder.Entity<ProductColor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("product_color");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.ColorName)
                    .IsRequired()
                    .HasColumnName("color_name")
                    .HasMaxLength(100);
                entity.Property(e => e.ColorCode)
                    .IsRequired()
                    .HasColumnName("color_code")
                    .HasMaxLength(7);
                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.ProductId);
            });

            
            modelBuilder.Entity<ProductSize>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("product_size");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.Size)
                    .IsRequired()
                    .HasColumnName("size")
                    .HasMaxLength(10);
                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.ProductId);
            });

           
            modelBuilder.Entity<ProductImage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("product_image");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.Image)
                    .IsRequired()
                    .HasColumnName("image");
                entity.Property(e => e.Type)
                    .IsRequired()
                    .HasColumnName("type")
                    .HasMaxLength(20);
                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.ProductId);
            });

     
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("review");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.Username)
                    .IsRequired()
                    .HasColumnName("username")
                    .HasMaxLength(255);
                entity.Property(e => e.Rating)
                    .HasColumnName("rating")
                    .HasDefaultValue(0);
                entity.Property(e => e.Comment)
                    .HasColumnName("comment");
                entity.Property(e => e.React)
                    .HasColumnName("react")
                    .HasDefaultValue(0);
                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.ProductId);
            });

            
            modelBuilder.Entity<Similarity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("similarity");

                entity.Property(e => e.Id).HasColumnName("id");
                entity.Property(e => e.ProductId).HasColumnName("product_id");
                entity.Property(e => e.SimilarProductId).HasColumnName("similar_product_id");
                entity.Property(e => e.Rate)
                    .HasColumnName("rate")
                    .HasPrecision(5, 4);
                entity.Property(e => e.CreateTime)
                    .HasColumnName("create_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(e => e.UpdateTime)
                    .HasColumnName("update_time")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasIndex(e => e.ProductId);
                entity.HasIndex(e => e.SimilarProductId);
                entity.HasIndex(e => new { e.ProductId, e.SimilarProductId }).IsUnique();
            });
        }
    }
}