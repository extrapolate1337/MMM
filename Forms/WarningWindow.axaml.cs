using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MMM;

public partial class WarningWindow : Window
{
    public WarningWindow()
    {
        InitializeComponent();
        NewMessage();

        if (DragImage != null)
            DragImage.PointerPressed += DragImage_PointerPressed;
    }
    public void NewMessage()
    {
        Label.Content = "Are you sure?";
    }

    private void DragImage_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void Yes_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private void No_OnClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}