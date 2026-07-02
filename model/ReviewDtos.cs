namespace MReview.Models
{
    
public class CreateReviewRequest
    {
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }

    public class ReviewDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = "";
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public int React { get; set; }
        public DateTime CreateTime { get; set; }
    }

}
