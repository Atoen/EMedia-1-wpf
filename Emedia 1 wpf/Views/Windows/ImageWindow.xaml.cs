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
            
            // prevent blocking the filestream by loading the image data indirectly
            var data = File.ReadAllBytes(imagePath);
            var image = LoadImage(data);
            
            ImageControl.Source = image;
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error while displaying image: {e.Message}");
        }
    }
    
    private static BitmapImage LoadImage(byte[] imageData)
    {
        var image = new BitmapImage();
        using var mem = new MemoryStream(imageData);
        mem.Position = 0;
        
        image.BeginInit();
        image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = mem;
        image.EndInit();
        image.Freeze();
        
        return image;
    }
}