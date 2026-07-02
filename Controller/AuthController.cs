using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sql.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Sql.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ITokenService tokenService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _tokenService = tokenService;
            _logger = logger;
        }




        [HttpPost("admin-login")]
public async Task<ActionResult<AuthResponse>> AdminLogin([FromBody] LoginRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    var result = await _authService.LoginAsync(request);
    Console.WriteLine(result.IsAdmin);
    Console.WriteLine(result.IsStaff);
    if (!result.Success)
        return Unauthorized(result);

    if (!result.IsAdmin)
    {
        return Unauthorized(new AuthResponse
        {
            Success = false,
            Message = "Access denied. Admin privileges required."
        });
    }

    return Ok(result);
}



        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.RegisterAsync(request);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _authService.LoginAsync(request);

            if (!result.Success)
            {
                return Unauthorized(result);
            }

            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<AuthResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token is required" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var isValid = await _authService.ValidateRefreshTokenAsync(userId, request.RefreshToken);
            if (!isValid)
            {
                return Unauthorized(new { message = "Invalid refresh token" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User not found" });
            }

            var newAccessToken = _tokenService.GenerateAccessToken(user);

            return Ok(new
            {
                success = true,
                message = "Token refreshed successfully",
                accessToken = newAccessToken,
                refreshToken = request.RefreshToken
            });
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<UserResponse>> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsStaff = user.IsStaff,
                IsAdmin = user.IsAdmin,
                CreateTime = user.CreateTime,
                UpdateTime = user.UpdateTime
            };

            return Ok(userResponse);
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserResponse>> GetProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var userResponse = new UserResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsStaff = user.IsStaff,
                IsAdmin = user.IsAdmin,
                CreateTime = user.CreateTime,
                UpdateTime = user.UpdateTime
            };

            return Ok(userResponse);
        }

        [HttpPost("logout")]
        public ActionResult Logout()
        {
            _logger.LogInformation("✅ User logged out");
            return Ok(new { message = "Logged out successfully" });
        }




        [HttpPost("reset-all-data")]
public async Task<ActionResult> ResetAllData([FromBody] ResetRequest request)
{
    if (request.Password != "PPOOPP%%")
    {
        return Unauthorized(new { message = "Invalid password" });
    }

    try
    {
        var db = _authService.GetDbContext();
        
        db.Users.RemoveRange(db.Users);
        await db.SaveChangesAsync();

        return Ok(new { 
            success = true, 
            message = "✅ All data has been reset successfully" 
        });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { 
            success = false, 
            message = $"❌ Reset failed: {ex.Message}" 
        });
    }
}
    }


    
}
