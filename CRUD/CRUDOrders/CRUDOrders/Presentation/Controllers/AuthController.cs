using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using CRUDOrders.Application.DTOs;
using CRUDOrders.Infrastructure.Identity;
using CRUDOrders.Infrastructure.Data.Repositories;
using Microsoft.Extensions.Logging;

namespace CRUDOrders.Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[ApiExplorerSettings(GroupName = "auth")]
[Tags("1. Phân hệ Xác thực & Phân quyền")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILogger<AuthController> _logger;
    private readonly CRUDOrders.Infrastructure.Services.IIdGenerator _idGenerator;

    public AuthController(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        CRUDOrders.Infrastructure.Services.IIdGenerator idGenerator,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _idGenerator = idGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Đăng nhập lấy JWT Bearer token
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(Result<object>.Error("INVALID_INPUT", "Tên đăng nhập và mật khẩu là bắt buộc"));
        }

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user {Username}", request.Username);
            return Unauthorized(Result<object>.Error("INVALID_CREDENTIALS", "Tên đăng nhập hoặc mật khẩu không chính xác"));
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user {Username}", request.Username);
            return Unauthorized(Result<object>.Error("USER_INACTIVE", "Tài khoản đã bị vô hiệu hóa"));
        }

        var token = _jwtTokenService.GenerateToken(user);
        var response = new LoginResponse
        {
            AccessToken = token,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role
        };

        _logger.LogInformation("User {Username} logged in successfully", request.Username);
        return Ok(Result<LoginResponse>.Ok(response));
    }

    /// <summary>
    /// Tạo tài khoản quản trị viên (Role: Admin)
    /// </summary>
    [HttpPost("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        return await CreateUserWithRole(request, "Admin", cancellationToken);
    }

    /// <summary>
    /// Tạo tài khoản nhân viên (Role: Staff) - Yêu cầu tài khoản Admin gọi
    /// </summary>
    [HttpPost("register-staff")]
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterStaff([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        return await CreateUserWithRole(request, "Staff", cancellationToken);
    }

    private async Task<IActionResult> CreateUserWithRole(RegisterRequest request, string role, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Email))
        {
            return BadRequest(Result<object>.Error("INVALID_INPUT", "Username, Email và Password là bắt buộc"));
        }

        var existingUser = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (existingUser != null)
        {
            return BadRequest(Result<object>.Error("USER_EXISTS", "Tên đăng nhập đã tồn tại trong hệ thống"));
        }

        var existingEmail = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existingEmail != null)
        {
            return BadRequest(Result<object>.Error("EMAIL_EXISTS", "Email đã được sử dụng"));
        }

        var newUser = new CRUDOrders.Core.Entities.User
        {
            Id = _idGenerator.NewId(),
            Username = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            Role = role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(7)
        };

        await _userRepository.AddAsync(newUser, cancellationToken);
        _logger.LogInformation("Created new user {Username} with role {Role}", newUser.Username, role);

        return Ok(Result<object>.Ok(new
        {
            UserId = newUser.Id,
            Username = newUser.Username,
            Email = newUser.Email,
            Role = newUser.Role
        }));
    }

    private static string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    private static bool VerifyPassword(string password, string hash)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            var computed = Convert.ToBase64String(hashedBytes);
            return computed == hash;
        }
    }
}

