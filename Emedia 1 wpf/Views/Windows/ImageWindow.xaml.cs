using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Emedia_1_wpf.Views.Windows;

public partial class ImageWindow
{
    public ImageWindow(string imagePath)
    {
        InitializeComponent();
        DisplayImage(imagePath);
    }
    
    private void DisplayImage(string imagePath)
    {
        try
        {
            Title = Path.GetFileName(imagePath);
            
            var bitmap = new BitmapImage(new Uri(imagePath));
            ImageControl.Source = bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }
}