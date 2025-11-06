using Rascor.Application.DTOs;
using Rascor.Domain;
using Rascor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Rascor.Application.Services;

public class GeofenceService : IGeofenceService
{
    private readonly IGeofenceEventRepository _eventRepository;
    private readonly ISiteRepository _siteRepository;
    private readonly ILogger<GeofenceService> _logger;

    public GeofenceService(
        IGeofenceEventRepository eventRepository,
        ISiteRepository siteRepository,
        ILogger<GeofenceService> logger)
    {
        _eventRepository = eventRepository;
        _siteRepository = siteRepository;
        _logger = logger;
    }

    public async Task<ManualCheckInResponse> ManualCheckInAsync(ManualCheckInRequest request)
    {
        try
        {
            // 1. Get the site
            var site = await _siteRepository.GetByIdAsync(request.SiteId);
            if (site == null)
            {
                return new ManualCheckInResponse
                {
                    Success = false,
                    Message = "Site not found"
                };
            }

            // 2. Calculate distance
            var distance = CalculateDistance(
                request.Latitude, request.Longitude,
                site.Latitude, site.Longitude
            );

            // 3. Validate distance
            if (distance > site.ManualTriggerRadiusMeters)
            {
                return new ManualCheckInResponse
                {
                    Success = false,
                    Message = $"Too far from site. Distance: {distance:F0}m, Required: <{site.ManualTriggerRadiusMeters}m",
                    Distance = distance,
                    RequiredDistance = site.ManualTriggerRadiusMeters
                };
            }

            // 4. Validate GPS accuracy
            if (request.Accuracy > 200)
            {
                return new ManualCheckInResponse
                {
                    Success = false,
                    Message = "GPS signal too weak. Please wait for better signal."
                };
            }

            // 5. Check for duplicate entry (within 5 minutes)
            var recentEntry = await _eventRepository.GetLastEventForDeviceAtSiteAsync(
                request.UserId,
                request.SiteId
            );

            if (recentEntry != null &&
                recentEntry.EventType == "Enter" &&
                (DateTime.UtcNow - recentEntry.Timestamp).TotalMinutes < 5)
            {
                return new ManualCheckInResponse
                {
                    Success = false,
                    Message = "Already checked in recently"
                };
            }

            // 6. Create entry event
            var geofenceEvent = new GeofenceEvent
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                SiteId = request.SiteId,
                EventType = "Enter",
                TriggerMethod = "manual",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Timestamp = DateTime.UtcNow
            };

            await _eventRepository.AddAsync(geofenceEvent);

            _logger.LogInformation($"Manual check-in: {request.UserId} to {site.Name}");

            return new ManualCheckInResponse
            {
                Success = true,
                Message = $"Checked in to {site.Name}",
                EventId = geofenceEvent.Id,
                Distance = distance
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual check-in failed");
            return new ManualCheckInResponse
            {
                Success = false,
                Message = "Server error occurred"
            };
        }
    }

    public async Task<ManualCheckOutResponse> ManualCheckOutAsync(ManualCheckOutRequest request)
    {
        try
        {
            // 1. Verify user is checked in
            var lastEntry = await _eventRepository.GetLastEventForDeviceAtSiteAsync(
                request.UserId,
                request.SiteId
            );

            if (lastEntry == null || lastEntry.EventType != "Enter")
            {
                return new ManualCheckOutResponse
                {
                    Success = false,
                    Message = "Not currently checked in to this site"
                };
            }

            // 2. Check minimum time (prevent immediate check-out)
            var timeSinceEntry = DateTime.UtcNow - lastEntry.Timestamp;
            if (timeSinceEntry.TotalMinutes < 2)
            {
                return new ManualCheckOutResponse
                {
                    Success = false,
                    Message = "Just checked in. Wait at least 2 minutes before checking out."
                };
            }

            // 3. Create exit event
            var exitEvent = new GeofenceEvent
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                SiteId = request.SiteId,
                EventType = "Exit",
                TriggerMethod = "manual",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Timestamp = DateTime.UtcNow
            };

            await _eventRepository.AddAsync(exitEvent);

            var site = await _siteRepository.GetByIdAsync(request.SiteId);
            _logger.LogInformation($"Manual check-out: {request.UserId} from {site?.Name ?? request.SiteId}");

            return new ManualCheckOutResponse
            {
                Success = true,
                Message = $"Checked out from {site?.Name ?? request.SiteId}",
                EventId = exitEvent.Id
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual check-out failed");
            return new ManualCheckOutResponse
            {
                Success = false,
                Message = "Server error occurred"
            };
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3; // Earth radius in meters
        var φ1 = lat1 * Math.PI / 180;
        var φ2 = lat2 * Math.PI / 180;
        var Δφ = (lat2 - lat1) * Math.PI / 180;
        var Δλ = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                Math.Cos(φ1) * Math.Cos(φ2) *
                Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }
}