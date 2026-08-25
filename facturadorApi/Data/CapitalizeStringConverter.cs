using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Back.Data
{
    public class CapitalizeStringConverter : ValueConverter<string, string>
    {
        public CapitalizeStringConverter()
            : base(
                v => v == null ? v! : string.Concat(v[0].ToString().ToUpper(), v.Substring(1)),
                v => v)
        {
        }
    }
}
