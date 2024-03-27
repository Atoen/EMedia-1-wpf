using System.Windows;
using System.Windows.Input;

namespace Emedia_1_wpf.Views.User_Controls;

public partial class TitleBar
{
    public TitleBar()
    {
        InitializeComponent();
    }

    public static readonly DependencyProperty ProgressValueProperty = DependencyProperty.Register(
        nameof(ProgressValue), typeof(double), typeof(TitleBar), new PropertyMetadata(20.0, PropertyChangedCallback));

    private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var titleBar = (TitleBar) d;
        var value = (double) e.NewValue;

        var width = titleBar.DockPanel.ActualWidth * value;
        titleBar.ProgressBar.Width = width;
    }

    public double ProgressValue
    {
        get => (double) GetValue(ProgressValueProperty);
        set => SetValue(ProgressValueProperty, value);
    }
    
    
    private void TitleBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var width = DockPanel.ActualWidth * ProgressValue / 100;
        ProgressBar.Width = width;
    }

    private void MaximizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        var window = Window.GetWindow(this)!;

        if (window.WindowState == WindowState.Maximized)
        {
            MaximizeButton.Content = "🗖";
            window.WindowState = WindowState.Normal;
            window.BorderThickness = new Thickness(0);
        }
        else
        {
            window.WindowState = WindowState.Maximized;
            MaximizeButton.Content = "🗗";
            window.BorderThickness = new Thickness(8);
        }
    }

    private void MinimizeButton_OnClick(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.WindowState = WindowState.Minimized;
    }

    private void CloseButton_OnClick(object sender, RoutedEventArgs e)
    {
        Window.GetWindow(this)!.Close();
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && MaximizeButton.IsEnabled)
        {
            MaximizeButton_OnClick(sender, e);
        }
    }
}