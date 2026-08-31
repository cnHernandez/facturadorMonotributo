using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Back.Data
{
    public class CapitalizeStringConverter : ValueConverter<string, string>
    {
        public CapitalizeStringConverter()
            : base(
                value => Capitalize(value),
                value => value
            )
        {
        }

        private static string Capitalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            return char.ToUpper(value[0]) + value.Substring(1);
        }
    }
}