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

    /// <summary>
    /// true when the blinking animation should be running
    /// </summary>
    [ObservableProperty]
    private bool _isBlinking;

    /// <summary>
    /// true when the IR command was received, so the UI can checkmark this button
    /// </summary>
    [ObservableProperty]
    private bool _isSeen;

    public async Task TriggerAnimation()
    {
        // cancel the possibly running animation stopping timer
        _stopAnimationTimer?.Dispose();
        // stop the animation if it was running
        IsBlinking = false;
        // I think this task yield is needed to let the UI stop the animation
        await Task.Delay(0);
        // start the blink animation from the top
        IsBlinking = true;
        // automatically stop it in one second
        _stopAnimationTimer = DispatcherTimer.RunOnce(() => { IsBlinking = false; }, TimeSpan.FromSeconds(1));
    }
}
