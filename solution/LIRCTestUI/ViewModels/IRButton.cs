namespace LIRCTestUI.ViewModels;

using System;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using LIRCTestUI.Views;

public partial class IRButton : ObservableObject
{
    public string Name { get; set; } = "boink";

    private IDisposable? _stopAnimationTimer;

    [ObservableProperty]
    private bool _triggered;

    [ObservableProperty]
    private bool _seen;

    [RelayCommand]
    private async Task Clicked()
    {
        if(App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if(lifetime.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ScrollRelatedButtonIntoView(this);
            }
        }
        await Blink();
    }

    public async Task Blink()
    {
        Seen = true;

        _stopAnimationTimer?.Dispose();

        Triggered = false;
        await Task.Delay(0);
        Triggered = true;
        _stopAnimationTimer = DispatcherTimer.RunOnce(() => { Triggered = false; }, TimeSpan.FromSeconds(1));

        if(App.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            if(lifetime.MainWindow is MainWindow mainWindow)
            {
                mainWindow.ScrollRelatedButtonIntoView(this);
            }
        }
    }
}
