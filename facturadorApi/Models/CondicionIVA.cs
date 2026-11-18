namespace Back.Models
{
    public enum CondicionIVA
    {
        IVA_Sujeto_Exento,
        ResponsableInscripto,
        Consumidor_Final
    }

    public static class CondicionIVAExtensions
    {
        public static int ToArcaId(this CondicionIVA condicion) =>
            condicion switch
            {
                CondicionIVA.IVA_Sujeto_Exento => 4,
                CondicionIVA.ResponsableInscripto => 1,
                CondicionIVA.Consumidor_Final => 5,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(condicion),
                    condicion,
                    "La condición de IVA no tiene un código ARCA válido.")
            };

        public static string ToDisplayName(this CondicionIVA condicion) =>
            condicion switch
            {
                CondicionIVA.IVA_Sujeto_Exento => "IVA Sujeto Exento",
                CondicionIVA.ResponsableInscripto => "Responsable Inscripto",
                CondicionIVA.Consumidor_Final => "Consumidor Final",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(condicion),
                    condicion,
                    "La condición de IVA no tiene una descripción válida.")
            };
    }
}