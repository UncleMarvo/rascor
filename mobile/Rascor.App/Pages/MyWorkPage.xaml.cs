using Microsoft.Maui.Controls.Shapes;

namespace Rascor.App.Pages;

public partial class MyWorkPage : ContentPage
{
    public MyWorkPage()
    {
        InitializeComponent();
        LoadWorkAssignments();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshWorkAssignments();
    }

    private void LoadWorkAssignments()
    {
        // TODO: Load from API/local storage
        WorkAssignmentsContainer.Clear();
        
        // Show empty state for now
        EmptyStateContainer.IsVisible = true;
        SubtitleLabel.Text = "No work assignments available";
    }

    private void RefreshWorkAssignments()
    {
        LoadWorkAssignments();
    }

    private async void OnManualCheckInClicked(object sender, EventArgs e)
    {
        // TODO: Show site selection modal
        await DisplayAlert("Manual Check-In", "This will show a list of sites to manually check in", "OK");
    }

    private async void OnRefreshWorkClicked(object sender, EventArgs e)
    {
        var button = (Button)sender;
        button.IsEnabled = false;
        button.Text = "🔄 Refreshing...";

        // TODO: Call API to refresh work assignments
        await Task.Delay(1000); // Simulate network call

        RefreshWorkAssignments();
        
        button.Text = "🔄 Refresh Work";
        button.IsEnabled = true;
    }

    // TODO: Add method to create work assignment cards dynamically
    private Border CreateWorkAssignmentCard(string workType, string site, bool isSigned)
    {
        var frame = new Border
        {
            Stroke = isSigned ? Colors.Green : Colors.Orange,
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

        var layout = new VerticalStackLayout { Spacing = 10 };

        // Work type header
        var header = new HorizontalStackLayout { Spacing = 10 };
        header.Children.Add(new Label 
        { 
            Text = GetWorkTypeIcon(workType),
            FontSize = 24,
            VerticalOptions = LayoutOptions.Center
        });
        header.Children.Add(new Label 
        { 
            Text = workType,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        });
        header.Children.Add(new Label 
        { 
            Text = isSigned ? "✅" : "❌",
            FontSize = 20,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Center
        });

        layout.Children.Add(header);
        layout.Children.Add(new Label { Text = $"📍 {site}", FontSize = 14, TextColor = Colors.Gray });
        layout.Children.Add(new Label 
        { 
            Text = isSigned ? "Signed today at 8:05 AM" : "Not signed yet",
            FontSize = 12,
            TextColor = isSigned ? Colors.Green : Colors.Orange
        });

        frame.Content = layout;
        return frame;
    }

    private string GetWorkTypeIcon(string workType)
    {
        return workType.ToLower() switch
        {
            "electrical" => "⚡",
            "plumbing" => "🔧",
            "hvac" => "❄️",
            "scaffolding" => "🏗️",
            _ => "🔨"
        };
    }
}
