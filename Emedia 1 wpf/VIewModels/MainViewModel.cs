﻿using System.Collections.ObjectModel;
using System.IO;
using System.Numerics;
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
    [ObservableProperty] private bool _useLibrary;
    
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ShowImageCommand), nameof(AnonymizeImageCommand), nameof(EncryptImageCommand), nameof(DecryptImageCommand))]
    private bool _fileIsValid;
    
    [ObservableProperty] private ObservableCollection<Log> _logs = [];
    [ObservableProperty] private ObservableCollection<Metadata> _metadata = [];
    
    private readonly PngService _pngService = new();
    private readonly List<PngChunk> _chunks = [];

    private void ClearData()
    {
        SuccessMessage = string.Empty;
        ErrorMessage = string.Empty;
        FileIsValid = false;
        
        Logs.Clear();
        Metadata.Clear();
        _chunks.Clear();
    }
    
    [RelayCommand(CanExecute = nameof(FileIsValid))]
    private async Task DecryptImageAsync(CancellationToken cancellationToken)
    {
        if (GetWriteFilePath() is not { } path) return;
        
        try
        {
            await _pngService.DecryptAsync(_chunks, path, UseLibrary);
            MessageBox.Show("Successfully decrypted file", "Emedia");
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error while decrypting file: {e.Message}", "Emedia", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(FileIsValid))]
    private async Task EncryptImageAsync(CancellationToken cancellationToken)
    {
        if (GetWriteFilePath() is not { } path) return;
        
        try
        {
            await _pngService.EncryptAsync(_chunks, path, UseLibrary);
            MessageBox.Show("Successfully encrypted file", "Emedia");
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error while encrypting file: {e.Message}", "Emedia", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(FileIsValid))]
    private async Task AnonymizeImageAsync(CancellationToken cancellationToken)
    {
        if (GetWriteFilePath() is not { } path) return;
        
        try
        {
            await _pngService.AnonymizeImageAsync(_chunks, path);
            MessageBox.Show("Successfully anonymized file", "Emedia");
        }
        catch (Exception e)
        {
            MessageBox.Show($"Error while anonymizing file: {e.Message}", "Emedia", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(FileIsValid))]
    private void ShowImage()
    {
        new ImageWindow(Filename).Show();
    }

    [RelayCommand]
    private async Task SelectFileAsync(CancellationToken cancellationToken)
    {
        var dialog = new OpenFileDialog { Multiselect = false };
        if (dialog.ShowDialog() is not true) return;
        
        ClearData();

        Filename = dialog.FileName;

        var isValid = await _pngService.VerifyPngAsync(dialog.FileName);
        SetVerificationResultMessage(dialog.SafeFileName, isValid);

        FileIsValid = isValid;
        if (FileIsValid)
        {
            await ReadFileAsync(dialog.FileName);
        }
    }

    private void SetVerificationResultMessage(string filename, bool isValid)
    {
        if (isValid)
        {
            SuccessMessage = $"{filename} is valid PNG file";
        }
        else
        {
            ErrorMessage = $"{filename} is not valid PNG file";
        }
    }

    private string? GetWriteFilePath()
    {
        var dialog = new SaveFileDialog { Filter = "PNG *.png|*.png" };
        return dialog.ShowDialog() is true ? dialog.FileName : null;
    }
    
    private async Task ReadFileAsync(string filepath)
    {
        void Callback(Log log) => Logs.Add(log);
        var chunks = await _pngService.ReadDataAsync(filepath, Callback);
        
        _chunks.AddRange(chunks);
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
                    new Metadata("White Point", $"({cHrmChunk.WhitePointX}, {cHrmChunk.WhitePointY})"),
                    new Metadata("Red point", $"({cHrmChunk.RedX}, {cHrmChunk.RedY})"),
                    new Metadata("Green point", $"({cHrmChunk.GreenX}, {cHrmChunk.GreenY})"),
                    new Metadata("Blue Point", $"({cHrmChunk.BlueX}, {cHrmChunk.BlueY})")
                ],
                gAMAChunk gAmaChunk => [new Metadata("Gamma", gAmaChunk.Gamma)],
                iTXtChunk iTXtChunk =>
                    [new Metadata("International text", $"Compressed:{iTXtChunk.Compressed}\n{iTXtChunk.Keyword}: {iTXtChunk
                        .Text}")],
                oFFsChunk oFFsChunk => [new Metadata("Offset", $"({oFFsChunk.OffsetX}, {oFFsChunk.OffsetY}) {oFFsChunk.Unit}")],
                pHYsChunk pHYsChunk =>
                    [new Metadata("Physical dimensions", $"({pHYsChunk.PixelsPerUnitX}, {pHYsChunk.PixelsPerUnitY}) pixels per {pHYsChunk.UnitSpecifier}")],
                sPLTChunk sPltChunk => [new Metadata("Suggested palette", sPltChunk.PaletteName)],
                sRGBChunk sRgbChunk => [new Metadata("Rendering intent", sRgbChunk.RenderingIntent)],
                sTERChunk sTerChunk => [new Metadata("Stereo indicator", sTerChunk.Indicator)],
                tEXtChunk tEXtChunk => [new Metadata("Text", $"{tEXtChunk.Keyword}: {tEXtChunk.Text}")],
                tIMEChunk tImeChunk => [new Metadata("Date of modification", tImeChunk.LastModificationTime)],
                zTXtChunk zTXtChunk => [new Metadata("Compressed text", $"Compression method: {zTXtChunk.CompressionMethod}\n{zTXtChunk.Keyword}: {zTXtChunk.Text}")],
                _ => Array.Empty<Metadata>()
            };
            
            allMetadata.AddRange(metadata);
        }

        return allMetadata;
    }
}