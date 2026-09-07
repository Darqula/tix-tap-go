namespace TixTapGo.Shared.Converters;

public struct CaseInsensitiveEnum<T> : IParsable<CaseInsensitiveEnum<T>> where T : struct, Enum
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
