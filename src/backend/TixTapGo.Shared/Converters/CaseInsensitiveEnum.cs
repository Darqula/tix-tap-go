using System.Diagnostics.CodeAnalysis;

namespace TixTapGo.Shared.Converters;

[SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix")]
[SuppressMessage("Design", "CA1000:Do not declare static members on generic types")]
public readonly struct CaseInsensitiveEnum<T> : IParsable<CaseInsensitiveEnum<T>> where T : struct, Enum
{
    public T Value { get; }
    
    public CaseInsensitiveEnum(T value) => Value = value;
    
    public static bool TryParse(string? s, IFormatProvider? provider, out CaseInsensitiveEnum<T> result)
    {
        if (Enum.TryParse<T>(s, true, out var enumValue))
        {
            result = new CaseInsensitiveEnum<T>(enumValue);
            return true;
        }
        result = default;
        return false;
    }

    public static CaseInsensitiveEnum<T> Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out var result) ? result : throw new FormatException();

    public static implicit operator T(CaseInsensitiveEnum<T> wrapper) => wrapper.Value;
}
