using System.Text.Json.Serialization;

namespace Rascor.Application.Models.Procore;

public class ProcoreCompany
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
