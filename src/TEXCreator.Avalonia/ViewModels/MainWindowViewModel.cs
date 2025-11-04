using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KleiLib;
using TEXCreator.Avalonia.Services;

namespace TEXCreator.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TexConverter _converter = new();

    [ObservableProperty]
    private string _outputDirectory = "";

    [ObservableProperty]
    private ObservableCollection<string> _selectedFiles = new();

    [ObservableProperty]
    private ObservableCollection<string> _logMessages = new();

    [ObservableProperty]
    private string _selectedPixelFormat = "DXT5";

    [ObservableProperty]
    private string _selectedTextureType = "2D";

    [ObservableProperty]
    private bool _generateMipmaps = false;

    [ObservableProperty]
    private bool _preMultiplyAlpha = false;

    [ObservableProperty]
    private bool _isConverting = false;

    public ObservableCollection<string> PixelFormats { get; } = new()
    {
        "DXT1", "DXT3", "DXT5", "ARGB"
    };

    public ObservableCollection<string> TextureTypes { get; } = new()
    {
        "1D", "2D", "3D", "Cubemap"
    };

    public MainWindowViewModel()
    {
        // Set default output directory to current directory
        OutputDirectory = Directory.GetCurrentDirectory();
    }

    [RelayCommand]
    private void AddFiles()
    {
        // This will be called from the View with file paths
    }

    public void AddFilesFromPicker(string[] filePaths)
    {
        foreach (var file in filePaths)
        {
            if (!SelectedFiles.Contains(file))
            {
                SelectedFiles.Add(file);
            }
        }
    }

    [RelayCommand]
    private void RemoveSelectedFile(string? filePath)
    {
        if (filePath != null && SelectedFiles.Contains(filePath))
        {
            SelectedFiles.Remove(filePath);
        }
    }

    [RelayCommand]
    private void ClearFiles()
    {
        SelectedFiles.Clear();
    }

    [RelayCommand]
    private void BrowseOutputDirectory()
    {
        // This will be handled by the View
    }

    public void SetOutputDirectory(string directory)
    {
        OutputDirectory = directory;
    }

    [RelayCommand]
    private async Task Convert()
    {
        if (SelectedFiles.Count == 0)
        {
            AddLog("Error: No files selected");
            return;
        }

        if (string.IsNullOrEmpty(OutputDirectory) || !Directory.Exists(OutputDirectory))
        {
            AddLog("Error: Invalid output directory");
            return;
        }

        IsConverting = true;
        LogMessages.Clear();

        try
        {
            var options = new TexConverter.ConversionOptions
            {
                PixelFormat = ParsePixelFormat(SelectedPixelFormat),
                TextureType = ParseTextureType(SelectedTextureType),
                GenerateMipmaps = GenerateMipmaps,
                PreMultiplyAlpha = PreMultiplyAlpha
            };

            foreach (var inputFile in SelectedFiles.ToList())
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(inputFile);
                    var outputFile = Path.Combine(OutputDirectory, fileName + ".tex");

                    await Task.Run(() => _converter.ConvertPngToTex(inputFile, outputFile, options));

                    AddLog($"✓ Converted: {fileName}.tex");
                }
                catch (Exception ex)
                {
                    AddLog($"✗ Error converting {Path.GetFileName(inputFile)}: {ex.Message}");
                }
            }

            AddLog($"\nConversion complete! {SelectedFiles.Count} file(s) processed.");
        }
        finally
        {
            IsConverting = false;
        }
    }

    private void AddLog(string message)
    {
        LogMessages.Add(message);
    }

    private TEXFile.PixelFormat ParsePixelFormat(string format)
    {
        return format switch
        {
            "DXT1" => TEXFile.PixelFormat.DXT1,
            "DXT3" => TEXFile.PixelFormat.DXT3,
            "DXT5" => TEXFile.PixelFormat.DXT5,
            "ARGB" => TEXFile.PixelFormat.ARGB,
            _ => TEXFile.PixelFormat.DXT5
        };
    }

    private TEXFile.TextureType ParseTextureType(string type)
    {
        return type switch
        {
            "1D" => TEXFile.TextureType.OneD,
            "2D" => TEXFile.TextureType.TwoD,
            "3D" => TEXFile.TextureType.ThreeD,
            "Cubemap" => TEXFile.TextureType.Cubemap,
            _ => TEXFile.TextureType.TwoD
        };
    }
}
