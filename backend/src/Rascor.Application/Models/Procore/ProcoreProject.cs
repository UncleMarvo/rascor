using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rascor.Application.Models.Procore;

public class ProcoreProject
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [JsonPropertyName("project_number")]
    public string? ProjectNumber { get; set; }

    // CHANGE THIS - Remove the converter, make it nullable object
    [JsonPropertyName("address")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? AddressRaw { get; set; }  // Just ignore it for now

    // Helper to get address as string if needed
    [JsonIgnore]
    public string? Address => AddressRaw?.ToString();

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("county")]
    public string? County { get; set; }

    [JsonPropertyName("country_code")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("zip")]
    public string? Zip { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
}

// Custom converter to handle address as either string or object
public class AddressConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        else if (reader.TokenType == JsonTokenType.StartObject)
        {
            // Parse the object and try to extract a meaningful address string
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            // Try common address properties
            if (root.TryGetProperty("street", out var street) && street.ValueKind == JsonValueKind.String)
                return street.GetString();
            if (root.TryGetProperty("address", out var addr) && addr.ValueKind == JsonValueKind.String)
                return addr.GetString();
            if (root.TryGetProperty("street_address", out var streetAddr) && streetAddr.ValueKind == JsonValueKind.String)
                return streetAddr.GetString();
            if (root.TryGetProperty("line1", out var line1) && line1.ValueKind == JsonValueKind.String)
                return line1.GetString();

            // If no recognizable property, return null (we have city/county/zip anyway)
            return null;
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} for address field");
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}