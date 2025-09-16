using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace aimware;

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
}