using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Back.Extensions
{
    /// <summary>
    /// Validador para DNI: debe contener exactamente 8 dígitos
    /// </summary>
    public class DniAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            string dni = value.ToString() ?? string.Empty;

            // Debe ser exactamente 8 dígitos
            return Regex.IsMatch(dni, @"^\d{8}$");
        }

        public override string FormatErrorMessage(string name)
        {
            return $"El {name} debe contener exactamente 8 dígitos.";
        }
    }

    /// <summary>
    /// Validador para CUIT: debe contener exactamente 11 dígitos
    /// </summary>
    public class CuitAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            string cuit = value.ToString() ?? string.Empty;

            // Debe ser exactamente 11 dígitos
            return Regex.IsMatch(cuit, @"^\d{11}$");
        }

        public override string FormatErrorMessage(string name)
        {
            return $"El {name} debe contener exactamente 11 dígitos.";
        }
    }

    /// <summary>
    /// Validador para nombres y apellidos: solo acepta letras (incluyendo tildes), espacios y guiones
    /// No permite números ni caracteres especiales
    /// </summary>
    public class LettersOnlyAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            string text = value.ToString() ?? string.Empty;

            // Permite: letras (incluyendo acentuadas), espacios y guiones
            // Rechaza: números y otros caracteres especiales
            return Regex.IsMatch(text, @"^[a-záéíóúñàèìòùäëïöüA-ZÁÉÍÓÚÑÀÈÌÒÙÄËÏÖÜ\s\-]+$");
        }

        public override string FormatErrorMessage(string name)
        {
            return $"El {name} solo puede contener letras, espacios y guiones (sin números ni caracteres especiales).";
        }
    }

    /// <summary>
    /// Validador para email: valida que sea un email válido
    /// </summary>
    public class EmailValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                return true;

            string email = value.ToString() ?? string.Empty;

            // Validación básica de email
            return Regex.IsMatch(email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        }

        public override string FormatErrorMessage(string name)
        {
            return $"El {name} debe ser una dirección de correo válida.";
        }
    }

    /// <summary>
    /// Validador para números de afiliado: permite dígitos, guiones y caracteres alfanuméricos
    /// </summary>
    public class NumAfiliadoAttribute : ValidationAttribute
    {
        public override bool IsValid(object? value)
        {
            if (value == null)
                return true;

            string numAfiliado = value.ToString() ?? string.Empty;

            // Permite números, letras y guiones (común en números de afiliado)
            return Regex.IsMatch(numAfiliado, @"^[a-zA-Z0-9\-]+$") && numAfiliado.Length > 0;
        }

        public override string FormatErrorMessage(string name)
        {
            return $"El {name} contiene caracteres inválidos. Solo se permiten números, letras y guiones.";
        }
    }
}
