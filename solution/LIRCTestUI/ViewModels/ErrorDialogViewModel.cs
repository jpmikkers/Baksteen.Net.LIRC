using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LIRCTestUI.Tools;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace LIRCTestUI.ViewModels;

public partial class ErrorDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    string _title = "Title";

    [ObservableProperty]
    string _message = "Message";

    [ObservableProperty]
    string _details = string.Empty;

    [RelayCommand]
    private void Okay()
    {
        ViewCloser.Close(this);
    }
}
