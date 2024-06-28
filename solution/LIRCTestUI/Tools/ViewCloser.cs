namespace LIRCTestUI.Tools;
using System.ComponentModel;

public static class ViewCloser
{
    public static void Close(INotifyPropertyChanged vm)
    {
        ViewResolver.LocateView(vm).Close();
    }

    public static void Close<TResult>(INotifyPropertyChanged vm, TResult result)
    {
        ViewResolver.LocateView(vm).Close(result);
    }
}
