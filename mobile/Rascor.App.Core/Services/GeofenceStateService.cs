namespace Rascor.App.Core.Services;

public enum GeofenceState
{
    NotAtSite,
    AtSite
}

public class GeofenceStateService
{
    private GeofenceState _currentState = GeofenceState.NotAtSite;
    private string? _currentSiteId;
    private DateTime? _checkInTime;
    private DateTime? _awayFromSiteStartTime;
    private int _consecutiveOutOfRangeCount;
    private DateTime? _notAtSiteDisplayStartTime;

    public event EventHandler<GeofenceState>? StateChanged;

    public GeofenceState CurrentState => _currentState;
    public string? CurrentSiteId => _currentSiteId;
    public DateTime? CheckInTime => _checkInTime;
    public DateTime? NotAtSiteDisplayStartTime => _notAtSiteDisplayStartTime;
    public DateTime? AwayFromSiteStartTime => _awayFromSiteStartTime;
    public int ConsecutiveOutOfRangeCount => _consecutiveOutOfRangeCount;

    public void SetCheckedIn(string siteId)
    {
        _currentState = GeofenceState.AtSite;
        _currentSiteId = siteId;
        _checkInTime = DateTime.Now;
        _awayFromSiteStartTime = null;
        _consecutiveOutOfRangeCount = 0;
        _notAtSiteDisplayStartTime = null;
        StateChanged?.Invoke(this, _currentState);
    }

    public void SetNotAtSite()
    {
        _currentState = GeofenceState.NotAtSite;
        _currentSiteId = null;
        _checkInTime = null;
        _awayFromSiteStartTime = null;
        _consecutiveOutOfRangeCount = 0;

        if (_notAtSiteDisplayStartTime == null)
            _notAtSiteDisplayStartTime = DateTime.Now;

        StateChanged?.Invoke(this, _currentState);
    }

    public void RecordOutOfRange()
    {
        _consecutiveOutOfRangeCount++;
        if (_awayFromSiteStartTime == null)
            _awayFromSiteStartTime = DateTime.Now;
    }

    public void RecordBackInRange()
    {
        _consecutiveOutOfRangeCount = 0;
        _awayFromSiteStartTime = null;
    }
}