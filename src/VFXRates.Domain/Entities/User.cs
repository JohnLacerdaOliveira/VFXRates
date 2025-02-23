namespace VFXRates.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Username { get; init; }
        public required string PasswordHash { get; init; }
    }
}