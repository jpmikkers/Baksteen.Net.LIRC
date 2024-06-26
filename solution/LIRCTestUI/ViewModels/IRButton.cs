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
    public required string RemoteName { get; init; }

    public required string Name { get; init; }

    private IDisposable? _stopAnimationTimer;

    [ObservableProperty]
    private bool _triggered;

    [ObservableProperty]
    private bool _seen;

    public async Task TriggerAnimation()
    {
        _stopAnimationTimer?.Dispose();
        Triggered = false;
        await Task.Delay(0);
        Triggered = true;
        _stopAnimationTimer = DispatcherTimer.RunOnce(() => { Triggered = false; }, TimeSpan.FromSeconds(1));
    }
}
