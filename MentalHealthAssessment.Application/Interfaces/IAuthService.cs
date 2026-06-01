using System.Threading.Tasks;
using MentalHealthAssessment.Application.Dtos;

namespace MentalHealthAssessment.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto> LoginAsync(string usernameOrPhone, string password);
    }
}
