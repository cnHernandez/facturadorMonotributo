namespace Back.DTOs
{
    public class ArcaTestConexionResponseDTO
    {
        public string Ambiente { get; set; } = string.Empty;
        public long CuitEmisor { get; set; }
        public int PuntoDeVenta { get; set; }
        public string WsaaUrl { get; set; } = string.Empty;
        public string WsfeUrl { get; set; } = string.Empty;
        public bool WsaaConexion { get; set; }
        public bool WsfeConexion { get; set; }
        public string MensajeWsaa { get; set; } = string.Empty;
        public string MensajeWsfe { get; set; } = string.Empty;
        public DateTime FechaValidacion { get; set; }
        public string CertificadoThumbprint { get; set; } = string.Empty;

        // FEDummy (diagnostico de servidores AFIP)
        public FEDummyResponse? FEDummy { get; set; }

        // FECompUltimoAutorizado (estado de numeración)
        public FECompUltimoAutorizadoInfo? UltimoComprobante { get; set; }
    }

    public class FEDummyResponse
    {
        public string AppServer { get; set; } = string.Empty;
        public string DbServer { get; set; } = string.Empty;
        public string AuthServer { get; set; } = string.Empty;
    }

    public class FECompUltimoAutorizadoInfo
    {
        public int PuntoVenta { get; set; }
        public int CbteTipo { get; set; }
        public int UltimoAutorizado { get; set; }
        public int ProximoNumero { get; set; }
    }
}
