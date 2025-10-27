﻿namespace Rascor.Domain.Entities;

public class Site
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int AutoTriggerRadiusMeters { get; set; } = 50;      // ADD THIS
    public int ManualTriggerRadiusMeters { get; set; } = 150;   // ADD THIS
}