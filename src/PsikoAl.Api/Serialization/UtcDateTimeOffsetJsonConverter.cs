using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PsikoAl.Api.Serialization;

// System.Text.Json'ın DateTimeOffset için varsayılan çıktısı "+00:00" ofset sonekiyle biter
// (örn. 2026-08-06T21:31:04.239788+00:00); frontend'deki Zod z.string().datetime() ise
// varsayılan olarak yalnızca "Z" sonekini kabul eder ("invalid_string: datetime" hatası).
// Tüm zaman damgalarımız zaten UTC olduğu için burada tek noktadan "Z" sonekine zorluyoruz.
public sealed class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTimeOffset();

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.UtcDateTime.ToString(Format, CultureInfo.InvariantCulture));
}
