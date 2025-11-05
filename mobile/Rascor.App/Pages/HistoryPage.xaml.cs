using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Shapes;
using Rascor.App.Services;

namespace Rascor.App.Pages;

public partial class HistoryPage : ContentPage
{
    private bool _showingAttendance = true;
    private readonly ILogger<HistoryPage> _logger;  // FIX: was ILogger<HomePage>
    private readonly BackendApi _backendApi;
    private readonly DeviceIdentityService _deviceIdentity;

    public HistoryPage(
        ILogger<HistoryPage> logger,
        BackendApi backendApi,
        DeviceIdentityService deviceIdentity)
    {
        InitializeComponent();
        _logger = logger;
        _backendApi = backendApi;
        _deviceIdentity = deviceIdentity;

        LoadHistory();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshHistory();
    }

    private void LoadHistory()
    {
        // Load both histories
        _ = LoadAttendanceHistory();  // Fire and forget
    }

    private void RefreshHistory()
    {
        _ = LoadAttendanceHistory();  // Fire and forget
    }

    private async Task LoadAttendanceHistory()  // FIX: Make async
    {
        await LoadCheckInsAsync();
    }

    private void OnAttendanceTabClicked(object sender, EventArgs e)
    {
        _showingAttendance = true;

        // Update button styles
        AttendanceTabButton.BackgroundColor = Colors.DodgerBlue;
        AttendanceTabButton.TextColor = Colors.White;

        // Show/hide containers
        AttendanceHistoryContainer.IsVisible = true;

        // Reload data
        _ = LoadAttendanceHistory();
    }

    private async Task LoadCheckInsAsync()
    {
        try
        {
            var userId = _deviceIdentity.GetUserId();

            // Get today's events from backend
            var response = await _backendApi.GetTodaysEventsAsync(userId);

            if (response != null && response.Count > 0)
            {
                CheckInsCollection.ItemsSource = response;
            }
            else
            {
                CheckInsCollection.ItemsSource = new List<object>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load check-ins");
            // Show empty if offline
            CheckInsCollection.ItemsSource = new List<object>();
        }
    }

    // TODO: Add methods to create history item cards
    private Border CreateAttendanceHistoryCard(string siteName, DateTime entryTime, DateTime? exitTime)
    {
        var frame = new Border
        {
            Stroke = Colors.LightGray,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Padding = 15,
            Shadow = new Shadow
            {
                Brush = Colors.Black,
                Offset = new Point(0, 2),
                Radius = 8,
                Opacity = 0.3f
            }
        };

        var layout = new VerticalStackLayout { Spacing = 5 };
        
        layout.Children.Add(new Label 
        { 
            Text = $"📍 {siteName}",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold
        });
        
        layout.Children.Add(new Label 
        { 
            Text = $"Entry: {entryTime:g}",
            FontSize = 12,
            TextColor = Colors.Gray
        });

        if (exitTime.HasValue)
        {
            layout.Children.Add(new Label 
            { 
                Text = $"Exit: {exitTime.Value:g}",
                FontSize = 12,
                TextColor = Colors.Gray
            });
            
            var duration = exitTime.Value - entryTime;
            layout.Children.Add(new Label 
            { 
                Text = $"Duration: {duration.Hours}h {duration.Minutes}m",
                FontSize = 12,
                TextColor = Colors.Green
            });
        }
        else
        {
            layout.Children.Add(new Label 
            { 
                Text = "Still on site",
                FontSize = 12,
                TextColor = Colors.Orange
            });
        }

        frame.Content = layout;
        return frame;
    }
}
