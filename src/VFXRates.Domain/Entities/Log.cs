namespace VFXRates.Domain.Entities
{
    public class Log
    {
        public int Id { get; set; }
        public required DateTime Timestamp { get; init; }
        public required string Level { get; init; }
        public required string Category { get; init; }
        public required string Message { get; init; }
        public string? Exception { get; init; }
    }
}