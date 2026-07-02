using System;

namespace Sql.Models
{

    public class Similarity
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int SimilarProductId { get; set; }
        public decimal Rate { get; set; }  
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }

        
        public Product Product { get; set; } = null!;
        public Product SimilarProduct { get; set; } = null!;
    }


    public class SimilarityResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int SimilarProductId { get; set; }
        public string SimilarProductName { get; set; } = "";
        public decimal Rate { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class ImportSimilarityRequest
    {
        public string FilePath { get; set; } = "similarity_results.json";
    }
}