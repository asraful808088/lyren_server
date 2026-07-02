using System;
using System.Collections.Generic;

namespace Sql.Models
{


    
    // ========================================
    // PRODUCT
    // ========================================
    public class Product
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int Count { get; set; }
        public required string MiniDesc { get; set; }
        public required string Description { get; set; }
        public string? CareDetails { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public required string InjectorUser { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

        // Navigation properties
        public ICollection<ProductCategory> Categories { get; set; } = new List<ProductCategory>();
        public ICollection<ProductCollection> Collections { get; set; } = new List<ProductCollection>();
        public ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();
        public ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();
        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
         public ICollection<Similarity> Similarities { get; set; } = new List<Similarity>();
        public ICollection<Similarity> SimilarToMe { get; set; } = new List<Similarity>();
    }

    // ========================================
    // PRODUCT CATEGORY
    // ========================================
    public class ProductCategory
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string Type { get; set; } // "For Him", "For Her", "Seasonal"
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

        // Foreign key
        public Product Product { get; set; } = null!;
    }

    // ========================================
    // PRODUCT COLLECTION
    // ========================================
    public class ProductCollection
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string Collection { get; set; } // "Essential", "New Arrival", "Limited"
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

        // Foreign key
        public Product Product { get; set; } = null!;
    }

    // ========================================
    // PRODUCT COLOR
    // ========================================
    public class ProductColor
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string ColorName { get; set; }
        public required string ColorCode { get; set; } // hex color "#FF0000"
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

        // Foreign key
        public Product Product { get; set; } = null!;
    }

    // ========================================
    // PRODUCT SIZE
    // ========================================
    public class ProductSize
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string Size { get; set; } // "XS", "S", "M", "L", "XL"
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

        // Foreign key
        public Product Product { get; set; } = null!;
    }

   
    public class ProductImage
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string Image { get; set; } 
        public required string Type { get; set; } 
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

  
        public Product Product { get; set; } = null!;
    }

    // ========================================
    // REVIEW
    // ========================================
    public class Review
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public required string Username { get; set; }
        public int Rating { get; set; } // 1-5 stars
        public string? Comment { get; set; }
        public int React { get; set; } // /likes=1,dislike=-1,no react =0
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

        // Foreign key
        public Product Product { get; set; } = null!;
    }
}
