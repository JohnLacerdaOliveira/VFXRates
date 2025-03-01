using Microsoft.AspNetCore.Mvc;
using VFXRates.Application.DTOs;
using VFXRates.Application.Interfaces;

namespace VFXRates.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogService _logService;

        public AuthController(IAuthService authService, ILogService logService)
        {
            _authService = authService;
            _logService = logService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDto userDto)
        {
            await _logService.LogDebug($"Trying to authenticate user username:{userDto.Username}");

            var token = await _authService.Login(userDto);

            await _logService.LogDebug($"Successful authentication for user username:{userDto.Username}");
            return Ok(new { Token = token });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDto userDto)
        {
            await _logService.LogDebug($"Registering new user username:{userDto.Username}");
            var user = await _authService.Register(userDto);

            return CreatedAtAction(nameof(Register), new { username = user.Username }, user);
        }
    }
}