namespace Back.Conf
{
    public class ArcaOptions
    {
        public const string SectionName = "Arca";

        public string WsaaUrl { get; set; } = null!;
        public string WsfeUrl { get; set; } = null!;
        public string Service { get; set; } = "wsfe";
        public long CuitEmisor { get; set; }
        public int PuntoDeVenta { get; set; }
        public string DomicilioComercial { get; set; } = null!;
        public string TicketCachePath { get; set; } = ".runtime/arca-ticket-wsfe.dat";
        public string CertificatePath { get; set; } = null!;
        public string PrivateKeyPath { get; set; } = null!;
    }
}