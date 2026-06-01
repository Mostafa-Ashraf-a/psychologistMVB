namespace MentalHealthAssessment.Application.Dtos
{
    public class RegisterRequestDto
    {
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } // "Admin", "Consultant", "Patient"
    }

    public class AuthResponseDto
    {
        public string Uid { get; set; }
        public string Username { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
    }
}
