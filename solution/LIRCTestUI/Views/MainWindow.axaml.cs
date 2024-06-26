namespace LIRCTestUI.Views;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using System.Linq;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void ScrollRelatedButtonIntoView(object vmObject)
    {
        foreach(var sv in this.GetVisualDescendants()
            .OfType<Button>()
            .Where(x => x.DataContext == vmObject))
        {
            var itemsControl = sv.GetVisualAncestors().OfType<ItemsControl>().FirstOrDefault();
            itemsControl?.ScrollIntoView(vmObject);
        }
    }

    protected override void IsVisibleChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.IsVisibleChanged(e);
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {

        base.OnAttachedToVisualTree(e);
    }


    private void CompiledBinding(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }
}