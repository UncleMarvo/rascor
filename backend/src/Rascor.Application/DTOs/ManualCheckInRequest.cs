namespace Rascor.Application.DTOs;

public class ManualCheckInRequest
{
    public string UserId { get; set; }
    public string SiteId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
}

public class ManualCheckInResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string EventId { get; set; }
    public double? Distance { get; set; }
    public int? RequiredDistance { get; set; }
}

public class ManualCheckOutRequest
{
    public string UserId { get; set; }
    public string SiteId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class ManualCheckOutResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string EventId { get; set; }
}
