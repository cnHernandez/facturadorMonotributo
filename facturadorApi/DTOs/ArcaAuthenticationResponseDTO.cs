namespace Back.DTOs
{
    public class ArcaAuthenticationResponseDTO
    {
        public string Service { get; set; } = null!;
        public DateTime ExpirationTimeUtc { get; set; }
    }
}