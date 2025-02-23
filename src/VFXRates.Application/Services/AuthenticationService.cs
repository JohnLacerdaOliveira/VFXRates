using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VFXRates.Application.DTOs;
using VFXRates.Application.Interfaces;
using VFXRates.Domain.Entities;

namespace VFXRates.Application.Services
{
    public class AuthenticationService : IAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly IAuthRepository _authRepository;
        private readonly ILogService _logger;

        public AuthenticationService(
            IConfiguration configuration,
            IAuthRepository authRepository,
            ILogService logger)
        {
            _configuration = configuration;
            _authRepository = authRepository;
            _logger = logger;
        }

        public async Task<string> Login(UserDto userDto)
        {
            var user = await _authRepository.GetUserByUsernameAsync(userDto.Username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash))
            {
                await _logger.LogWarning($"Login failed for {userDto.Username}: Invalid credentials.");
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userDto.Username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Secret"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            var expires = DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiresInMinutes"]));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), SecurityAlgorithms.HmacSha256)
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            await _logger.LogInformation($"User with username: {userDto.Username} logged in successfully.");
            return tokenString;
        }

        public async Task<UserDto> Register(UserDto userDto)
        {
            var existingUser = await _authRepository.GetUserByUsernameAsync(userDto.Username);
            if (existingUser != null)
            {
                await _logger.LogWarning($"Registration failed: username: {userDto.Username} already exists.");
                throw new InvalidOperationException("Username already taken.");
            }

            var user = new User
            {
                Username = userDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
            };

            await _authRepository.AddUserAsync(user);
            await _logger.LogInformation($"User {userDto.Username} registered successfully.");

            return new UserDto { Username = user.Username };
        }
    }
}
