using System.ComponentModel.DataAnnotations.Schema;

namespace Rascor.Domain.Entities;
public class GeofenceEvent
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SiteId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "Enter" or "Exit"
    public string TriggerMethod { get; set; } = "auto"; // "auto" or "manual" - ADD THIS
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
}
