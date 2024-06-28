using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;
using System.ComponentModel;
using System.Linq;

namespace LIRCTestUI.Tools;

public static class ViewResolver
{
    public static Window LocateView(INotifyPropertyChanged vm)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            return lifetime.Windows.Where(x => x.DataContext == vm).First();
        }
        throw new InvalidOperationException("Only support desktop apps");
    }
}
