using VFXRates.Application.DTOs;

namespace VFXRates.Application.Interfaces
{
    public interface IAuthService
    {
        public Task<string> Login(UserDto userDto);
        Task<UserDto> Register(UserDto userDto);
    }
}
