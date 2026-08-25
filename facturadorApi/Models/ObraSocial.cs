namespace Back.Models
{
    public class ObraSocial
    {
        public int Id { get; set; }
        public string Cuit { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string DomicilioComercial { get; set; } = null!;
        public CondicionObraSocial Condicion { get; set; }
        public bool Estado { get; set; }
        public ICollection<Paciente> Pacientes { get; set; } = new List<Paciente>();
    }
}