using System.ComponentModel.DataAnnotations;

namespace Sql.Services
{
  
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 255 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; }
    }


    public class LoginRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }

  
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }

    public class ResetRequest
    {
        [Required]
        public string Password { get; set; }
    }

  public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    
    public UserResponse User { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }

    public int? UserId { get; set; }
    public string Email { get; set; }
    public string Name { get; set; }
    public bool IsStaff { get; set; }
    public bool IsAdmin { get; set; }
}


    public class UserResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public bool IsStaff { get; set; }
        public bool IsAdmin { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }
    
}