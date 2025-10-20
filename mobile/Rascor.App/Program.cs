namespace Rascor.App;

public static class Program
{
    static void Main(string[] args)
    {
        var app = MauiProgram.CreateMauiApp();
        var mauiApp = app.Services.GetRequiredService<IApplication>();
        mauiApp.CreateWindow(null);
    }
}
