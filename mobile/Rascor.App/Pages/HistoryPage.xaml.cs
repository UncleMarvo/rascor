namespace Rascor.App.Pages;

public partial class HistoryPage : ContentPage
{
    private bool _showingAttendance = true;

    public HistoryPage()
    {
        InitializeComponent();
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
        LoadAttendanceHistory();
        LoadRamsHistory();
    }

    private void RefreshHistory()
    {
        if (_showingAttendance)
            LoadAttendanceHistory();
        else
            LoadRamsHistory();
    }

    private void LoadAttendanceHistory()
    {
        // TODO: Load from local database
        AttendanceItemsContainer.Clear();
        
        // Show empty state for now
        AttendanceEmptyState.IsVisible = true;
        AttendanceItemsContainer.IsVisible = false;
    }

    private void LoadRamsHistory()
    {
        // TODO: Load from local database
        RamsItemsContainer.Clear();
        
        // Show empty state for now
        RamsEmptyState.IsVisible = true;
        RamsItemsContainer.IsVisible = false;
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
    }

    // TODO: Add methods to create history item cards
    private Frame CreateAttendanceHistoryCard(string siteName, DateTime entryTime, DateTime? exitTime)
    {
        var frame = new Frame
        {
            BorderColor = Colors.LightGray,
            CornerRadius = 10,
            Padding = 15,
            HasShadow = true
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

    private Frame CreateRamsHistoryCard(string workType, string siteName, DateTime signedAt)
    {
        var frame = new Frame
        {
            BorderColor = Colors.Green,
            CornerRadius = 10,
            Padding = 15,
            HasShadow = true
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
