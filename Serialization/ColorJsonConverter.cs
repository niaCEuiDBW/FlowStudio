using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Drawing;

namespace FlowStudio.Serialization;

public sealed class ColorJsonConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Цвет должен быть строкой в формате ARGB.");
        }

        string? value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value) || !uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argb))
        {
            throw new JsonException("Не удалось прочитать цвет.");
        }

        return Color.FromArgb(unchecked((int)argb));
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToArgb().ToString("X8", CultureInfo.InvariantCulture));
    }
}
