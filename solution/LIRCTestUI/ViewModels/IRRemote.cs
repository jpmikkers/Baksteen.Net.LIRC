namespace LIRCTestUI.ViewModels;

using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;

public partial class IRRemote : ObservableObject
{
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private AvaloniaList<IRButton> _buttonList = new();
}
