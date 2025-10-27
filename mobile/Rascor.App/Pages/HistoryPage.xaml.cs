using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Shapes;
using Rascor.App.Services;

namespace Rascor.App.Pages;

public partial class HistoryPage : ContentPage
{
    private bool _showingAttendance = true;
    private readonly ILogger<HistoryPage> _logger;  // FIX: was ILogger<HomePage>
    private readonly RamsPhotoService _ramsPhotoService;
    private readonly BackendApi _backendApi;
    private readonly DeviceIdentityService _deviceIdentity;

    public HistoryPage(
        ILogger<HistoryPage> logger,
        RamsPhotoService ramsPhotoService,
        BackendApi backendApi,
        DeviceIdentityService deviceIdentity)
    {
        InitializeComponent();
        _logger = logger;
        _ramsPhotoService = ramsPhotoService;
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
        LoadRamsHistory();
    }

    private void RefreshHistory()
    {
        if (_showingAttendance)
            _ = LoadAttendanceHistory();  // Fire and forget
        else
            LoadRamsHistory();
    }

    private async Task LoadAttendanceHistory()  // FIX: Make async
    {
        await LoadCheckInsAsync();
    }

    private void LoadRamsHistory()
    {
        try
        {
            // Load RAMS photos
            var photos = _ramsPhotoService.GetTodaysPhotos();
            RamsPhotosCollection.ItemsSource = photos;

            _logger.LogInformation("Loaded {Count} RAMS photos", photos.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load RAMS photos");
        }
    }

    private void OnAttendanceTabClicked(object sender, EventArgs e)
    {
        _showingAttendance = true;

        // Update button styles
        AttendanceTabButton.BackgroundColor = Colors.DodgerBlue;
        AttendanceTabButton.TextColor = Colors.White;
        RamsTabButton.BackgroundColor = Colors.LightGray;
        RamsTabButton.TextColor = Colors.Black;

        // Show/hide containers
        AttendanceHistoryContainer.IsVisible = true;
        RamsHistoryContainer.IsVisible = false;

        // Reload data
        _ = LoadAttendanceHistory();
    }

    private void OnRamsTabClicked(object sender, EventArgs e)
    {
        _showingAttendance = false;

        // Update button styles
        RamsTabButton.BackgroundColor = Colors.DodgerBlue;
        RamsTabButton.TextColor = Colors.White;
        AttendanceTabButton.BackgroundColor = Colors.LightGray;
        AttendanceTabButton.TextColor = Colors.Black;

        // Show/hide containers
        AttendanceHistoryContainer.IsVisible = false;
        RamsHistoryContainer.IsVisible = true;

        // Reload data
        LoadRamsHistory();
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

    private Border CreateRamsHistoryCard(string workType, string siteName, DateTime signedAt)
    {
        var frame = new Border
        {
            Stroke = Colors.Green,
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
        
        var header = new HorizontalStackLayout { Spacing = 10 };
        header.Children.Add(new Label 
        { 
            Text = "✅",
            FontSize = 20,
            VerticalOptions = LayoutOptions.Center
        });
        header.Children.Add(new Label 
        { 
            Text = workType,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        });

        layout.Children.Add(header);
        layout.Children.Add(new Label { Text = $"📍 {siteName}", FontSize = 12, TextColor = Colors.Gray });
        layout.Children.Add(new Label { Text = $"Signed: {signedAt:g}", FontSize = 12, TextColor = Colors.Gray });

        var viewButton = new Button 
        { 
            Text = "View Document",
            FontSize = 12,
            Padding = new Thickness(10, 5),
            Margin = new Thickness(0, 5, 0, 0)
        };
        // TODO: Wire up view document handler

        layout.Children.Add(viewButton);

        frame.Content = layout;
        return frame;
    }
}
