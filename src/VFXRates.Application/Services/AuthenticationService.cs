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

            if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user?.PasswordHash))
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

            if (string.IsNullOrEmpty(tokenString))
                throw new InvalidOperationException("Token generation failed.");

            await _logger.LogInformation($"User with username: {userDto.Username} logged in successfully.");

            return tokenString;
        }

        public async Task<UserDto> Register(UserDto userDto)
        {
            // Check if the user already exists
            var existingUser = await _authRepository.GetUserByUsernameAsync(userDto.Username);
            if (existingUser != null)
            {
                await _logger.LogWarning($"Registration failed: username {userDto.Username} already exists.");
                throw new InvalidOperationException($"Username {userDto.Username} already taken.");
            }

            var newUser = new User
            {
                Username = userDto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
            };

            bool success = await _authRepository.AddUserAsync(newUser);
            if (!success)
            {
                await _logger.LogError($"Registration failed: unable to add user {userDto.Username} to the database.");
                throw new Exception("Registration failed due to an unexpected error.");
            }

            await _logger.LogInformation($"User {userDto.Username} registered successfully.");
            return new UserDto { Username = newUser.Username };
        }

    }
}
