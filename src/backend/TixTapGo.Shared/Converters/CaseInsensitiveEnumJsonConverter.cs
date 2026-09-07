using System.Text.Json;
using System.Text.Json.Serialization;

namespace TixTapGo.Shared.Converters;

/// <summary>
/// Serializes and deserializes <see cref="CaseInsensitiveEnum{T}"/> as its wrapped enum for OpenAPI spec
/// </summary>
public class CaseInsensitiveEnumJsonConverter<T> : JsonConverter<CaseInsensitiveEnum<T>> where T : struct, Enum
{
    public override CaseInsensitiveEnum<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(JsonSerializer.Deserialize<T>(ref reader, options));

    public override void Write(Utf8JsonWriter writer, CaseInsensitiveEnum<T> value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Value, options);
}

public class CaseInsensitiveEnumConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(CaseInsensitiveEnum<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var enumType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(CaseInsensitiveEnumJsonConverter<>).MakeGenericType(enumType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}
