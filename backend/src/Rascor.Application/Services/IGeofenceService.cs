using Rascor.Application.DTOs;

namespace Rascor.Application.Services;

public interface IGeofenceService
{
    Task<ManualCheckInResponse> ManualCheckInAsync(ManualCheckInRequest request);
    Task<ManualCheckOutResponse> ManualCheckOutAsync(ManualCheckOutRequest request);
}