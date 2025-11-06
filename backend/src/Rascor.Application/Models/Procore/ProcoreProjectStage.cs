using System.Text.Json.Serialization;

namespace Rascor.Application.Models.Procore;

public class ProcoreProjectStage
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
}
