using BCrypt.Net;
using Sql.Models;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Sql.Services
{
    public interface IAuthService
    {
        AppDbContext GetDbContext();
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken);
        Task<User> GetUserByIdAsync(int id);
    }

    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        // How old an account must be, with zero logins since registration, before it's stale
        private static readonly TimeSpan StaleAccountAge = TimeSpan.FromDays(2);

        public AuthService(AppDbContext context, ITokenService tokenService, ILogger<AuthService> logger)
        {
            _context = context;
            _tokenService = tokenService;
            _logger = logger;
        }

        public AppDbContext GetDbContext() => _context;

       
        private async Task CleanupStaleAccountsAsync()
        {
            try
            {
                var cutoff = DateTime.UtcNow - StaleAccountAge;

                var staleUsers = await _context.Users
                    .Where(u => !u.IsAdmin
                             && !u.IsStaff
                             && u.UpdateTime == u.CreateTime   // never logged in since registering
                             && u.CreateTime < cutoff)
                    .ToListAsync();

                if (staleUsers.Count == 0)
                    return;

                _context.Users.RemoveRange(staleUsers);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"🧹 Removed {staleUsers.Count} stale, never-logged-in account(s) older than {StaleAccountAge.TotalDays} days.");
            }
            catch (Exception ex)
            {
                // Cleanup failures should never block registration/login
                _logger.LogError($"⚠️ Stale account cleanup failed: {ex.Message}");
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                await CleanupStaleAccountsAsync();

                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User with this email already exists"
                    };
                }

                var refreshToken = _tokenService.GenerateRefreshToken();
                var refreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                var now = DateTime.UtcNow;

                var user = new User
                {
                    Name = request.Name,
                    Email = request.Email,
                    Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiryTime = refreshTokenExpiryTime,
                    IsStaff = false,
                    IsAdmin = false,
                    CreateTime = now,
                    UpdateTime = now   
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                var accessToken = _tokenService.GenerateAccessToken(user);

                _logger.LogInformation($"✅ User registered successfully: {user.Email}");

                return new AuthResponse
                {
                    Success = true,
                    Message = "User registered successfully",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsAdmin = user.IsAdmin,
                    IsStaff = user.IsStaff,
                    Email = user.Email,
                    Name = user.Name,
                    UserId = user.Id,
                    User = new UserResponse
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        IsStaff = user.IsStaff,
                        IsAdmin = user.IsAdmin,
                        CreateTime = user.CreateTime,
                        UpdateTime = user.UpdateTime
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Registration error: {ex.Message}");
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                await CleanupStaleAccountsAsync();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                var accessToken = _tokenService.GenerateAccessToken(user);
                var refreshToken = _tokenService.GenerateRefreshToken();

                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                user.UpdateTime = DateTime.UtcNow;  

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ User logged in successfully: {user.Email}");

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    IsAdmin = user.IsAdmin,
                    IsStaff = user.IsStaff,
                    Email = user.Email,
                    Name = user.Name,
                    UserId = user.Id,
                    User = new UserResponse
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email,
                        IsStaff = user.IsStaff,
                        IsAdmin = user.IsAdmin,
                        CreateTime = user.CreateTime,
                        UpdateTime = user.UpdateTime
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Login error: {ex.Message}");
                return new AuthResponse
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}"
                };
            }
        }

        public async Task<bool> ValidateRefreshTokenAsync(int userId, string refreshToken)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return false;

            if (user.RefreshToken != refreshToken)
                return false;

            if (user.RefreshTokenExpiryTime < DateTime.UtcNow)
                return false;

            return true;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}