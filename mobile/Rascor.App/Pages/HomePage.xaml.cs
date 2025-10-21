namespace Rascor.App.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        LoadDashboard();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        RefreshDashboard();
    }

    private void LoadDashboard()
    {
        // Set greeting based on time of day
        var hour = DateTime.Now.Hour;
        var greeting = hour < 12 ? "Good morning! 👋" : hour < 18 ? "Good afternoon! 👋" : "Good evening! 👋";
        GreetingLabel.Text = greeting;

        // TODO: Load real data from services
        UpdateLocationStatus();
        UpdateWorkAssignments();
        UpdateSyncStatus();
        UpdateWeeklyStats();
    }

    private void RefreshDashboard()
    {
        // Refresh data when page appears
        LoadDashboard();
    }

    private void UpdateLocationStatus()
    {
        // TODO: Get actual location from geofencing service
        LocationLabel.Text = "Not at any site";
        CheckInTimeLabel.Text = "";
        CheckInTimeLabel.IsVisible = false;
    }

    private void UpdateWorkAssignments()
    {
        // TODO: Load from API/local storage
        WorkItemsContainer.Clear();
        TodaysWorkLabel.Text = "No work assignments today";
        TodaysWorkLabel.TextColor = Colors.Gray;
    }

    private void UpdateSyncStatus()
    {
        // TODO: Check pending queue
        SyncStatusLabel.Text = "All synced ✓";
        SyncStatusLabel.TextColor = Colors.Green;
    }

    private void UpdateWeeklyStats()
    {
        // TODO: Query local database
        SitesVisitedLabel.Text = "0";
        RamsSignedLabel.Text = "0";
    }

    private async void OnSignNowClicked(object sender, EventArgs e)
    {
        // TODO: Navigate to RAMS viewer
        await DisplayAlert("Sign RAMS", "This will navigate to RAMS signing", "OK");
    }

    private async void OnSyncNowClicked(object sender, EventArgs e)
    {
        SyncButton.IsEnabled = false;
        SyncButton.Text = "Syncing...";

        // TODO: Trigger actual sync
        await Task.Delay(1000); // Simulate sync

        SyncButton.Text = "Sync Now";
        SyncButton.IsEnabled = true;
        
        await DisplayAlert("Sync Complete", "All data synced successfully", "OK");
    }
}
