namespace LIRCTestUI.ViewModels;

using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class IRRemote : ObservableObject
{
    public required string Name { get; init; }

    [ObservableProperty]
    private AvaloniaList<IRButton> _buttonList = new();
}
