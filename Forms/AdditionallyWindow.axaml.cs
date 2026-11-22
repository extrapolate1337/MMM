using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MMM;

public partial class SecondWindow : Window
{
    public SecondWindow()
    {
        InitializeComponent();

        if (DragImage != null)
        {
            DragImage.PointerPressed += DragImage_PointerPressed;
        }
    }

    private void DragImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            this.BeginMoveDrag(e);
        }
    }

    private void CloseAdditionally_OnClick(object? sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void GitHubLink_OnClick(object? sender, RoutedEventArgs e)
    {
        TopLevel.GetTopLevel(this)?
            .Launcher
            .LaunchUriAsync(new System.Uri("https://github.com/extrapolate1337/MMM"));
    }
}