namespace Back.Services
{
    public class ArcaAuthenticationTicket
    {
        public string Token { get; init; } = null!;
        public string Sign { get; init; } = null!;
        public DateTime ExpirationTimeUtc { get; init; }
    }
}