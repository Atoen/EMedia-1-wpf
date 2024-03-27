using System.Windows.Input;

namespace Emedia_1_wpf.Views.Windows;

public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void TitleBar_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }
}