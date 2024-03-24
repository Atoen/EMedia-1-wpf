using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Emedia_1_wpf.Models;
using Emedia_1_wpf.Services;
using Emedia_1_wpf.Services.Chunks;
using Emedia_1_wpf.Views.Windows;
using Microsoft.Win32;

namespace Emedia_1_wpf.VIewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _filename = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _successMessage = string.Empty;
    
    [ObservableProperty] private ObservableCollection<string> _logs = [];
    [ObservableProperty] private ObservableCollection<Metadata> _metadata = [];
    
    private readonly PngService _pngService = new();
    private readonly Dispatcher _dispatcher = Application.Current.Dispatcher;

    [RelayCommand]
    private async Task SelectFileAsync(CancellationToken cancellationToken)
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;
        
        var dialog = new OpenFileDialog { Multiselect = false };
        if (dialog.ShowDialog() is not true) return;
        
        Filename = dialog.FileName;
        var valid = await _pngService.VerifyPngAsync(Filename);
        if (valid)
        {
            SuccessMessage = $"{dialog.SafeFileName} is valid PNG file";
        }
        else
        {
            ErrorMessage = $"{dialog.SafeFileName} is not valid PNG file";
            return;
        }

        await ReadFileAsync(dialog.FileName);

        // var imageWindow = new ImageWindow(dialog.FileName);
        // imageWindow.Show();
    }

    private async Task ReadFileAsync(string filename)
    {
        Logs.Clear();
        
        void Callback(string message) => _dispatcher.Invoke(() => Logs.Add(message));
        var chunks = await _pngService.ReadDataAsync(filename, Callback);
        foreach (var metadata in ReadMetadata(chunks))
        {
            Metadata.Add(metadata);
        }
    }

    private List<Metadata> ReadMetadata(List<PngChunk> chunks)
    {
        var allMetadata = new List<Metadata>();
        
        foreach (var chunk in chunks)
        {
            var metadata = chunk switch
            {
                IHDRChunk ihdrChunk =>
                [
                    new Metadata("Color type", ihdrChunk.ColorType),
                    new Metadata("Image width", ihdrChunk.Width),
                    new Metadata("Image height", ihdrChunk.Height),
                    new Metadata("Bit depth", ihdrChunk.BitDepth),
                    new Metadata("Compression method", ihdrChunk.CompressionMethod),
                    new Metadata("Filter method", ihdrChunk.FilterMethod),
                    new Metadata("Interlace method", ihdrChunk.InterlaceMethod)
                ],
                cHRMChunk cHrmChunk =>
                [
                    new Metadata("White Point", $"({cHrmChunk.WhitePointX} {cHrmChunk.WhitePointY})"),
                    new Metadata("Red point", $"({cHrmChunk.RedX} {cHrmChunk.RedY})"),
                    new Metadata("Green point", $"({cHrmChunk.GreenX} {cHrmChunk.GreenY})"),
                    new Metadata("Blue Point", $"({cHrmChunk.BlueX} {cHrmChunk.BlueY})")
                ],
                gAMAChunk gAmaChunk => [new Metadata("Gamma", gAmaChunk.Gamma)],
                iTXtChunk iTXtChunk =>
                    [new Metadata("International text", $"{iTXtChunk.Keyword}: {iTXtChunk.Text}")],
                oFFsChunk oFFsChunk => [new Metadata("Offset", $"({oFFsChunk.OffsetX} {oFFsChunk.OffsetY}) {oFFsChunk.Unit}")],
                pHYsChunk pHYsChunk =>
                    [new Metadata("Physical dimensions", $"({pHYsChunk.PixelsPerUnitX} {pHYsChunk.PixelsPerUnitY}) pixels per {pHYsChunk.UnitSpecifier}")],
                sPLTChunk sPltChunk => [new Metadata("Suggested palette", sPltChunk.PaletteName)],
                sRGBChunk sRgbChunk => [new Metadata("Rendering intent", sRgbChunk.RenderingIntent)],
                sTERChunk sTerChunk => [new Metadata("Stereo indicator", sTerChunk.Indicator)],
                tEXtChunk tEXtChunk => [new Metadata("Text", $"{tEXtChunk.Keyword}: {tEXtChunk.Text}")],
                tIMEChunk tImeChunk => [new Metadata("Date of modification", tImeChunk.LastModificationTime)],
                zTXtChunk zTXtChunk => [new Metadata("Compressed text", $"{zTXtChunk.Keyword}: {zTXtChunk.Text}, compression method: {zTXtChunk.CompressionMethod}")],
                _ => Array.Empty<Metadata>()
            };
            
            allMetadata.AddRange(metadata);
        }

        return allMetadata;
    }
}