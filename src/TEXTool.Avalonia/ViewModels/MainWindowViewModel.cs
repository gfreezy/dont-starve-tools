using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TEXTool.Avalonia.Models;
using TEXTool.Avalonia.Services;

namespace TEXTool.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly TexLoader _loader = new();
    private Image<Rgba32>? _loadedImage;

    [ObservableProperty]
    private Bitmap? _displayImage;

    [ObservableProperty]
    private string _fileName = "";

    [ObservableProperty]
    private string _platform = "";

    [ObservableProperty]
    private string _pixelFormat = "";

    [ObservableProperty]
    private string _textureType = "";

    [ObservableProperty]
    private string _imageSize = "";

    [ObservableProperty]
    private int _mipmapCount = 0;

    [ObservableProperty]
    private ObservableCollection<AtlasElement> _atlasElements = new();

    [ObservableProperty]
    private AtlasElement? _selectedAtlasElement;

    [ObservableProperty]
    private bool _hasAtlas = false;

    [ObservableProperty]
    private string _statusMessage = "No file loaded";

    // Image dimensions
    [ObservableProperty]
    private int _imageWidth = 0;

    [ObservableProperty]
    private int _imageHeight = 0;

    // Highlight box properties
    [ObservableProperty]
    private bool _highlightVisible = false;

    [ObservableProperty]
    private double _highlightX = 0;

    [ObservableProperty]
    private double _highlightY = 0;

    [ObservableProperty]
    private double _highlightWidth = 0;

    [ObservableProperty]
    private double _highlightHeight = 0;

    // Zoom properties
    [ObservableProperty]
    private double _zoomLevel = 1.0;

    private const double MinZoom = 0.1;
    private const double MaxZoom = 10.0;
    private const double ZoomStep = 0.1;

    // Pan properties for image dragging
    [ObservableProperty]
    private double _panX = 0;

    [ObservableProperty]
    private double _panY = 0;

    public void LoadFile(string filePath)
    {
        try
        {
            StatusMessage = "Loading...";

            var result = _loader.LoadTexFile(filePath);

            // Store the loaded image
            _loadedImage = result.Image;

            // Convert to Avalonia Bitmap
            DisplayImage = ConvertToAvaloniaBitmap(result.Image);

            // Set file info
            FileName = Path.GetFileName(filePath);
            Platform = result.Platform;
            PixelFormat = result.PixelFormat;
            TextureType = result.TextureType;
            ImageSize = $"{result.Width}x{result.Height}";
            MipmapCount = result.MipmapCount;

            // Store image dimensions
            ImageWidth = result.Width;
            ImageHeight = result.Height;

            // Set atlas elements
            AtlasElements.Clear();
            foreach (var element in result.AtlasElements)
            {
                AtlasElements.Add(element);
            }
            HasAtlas = AtlasElements.Count > 0;

            StatusMessage = $"Loaded: {FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    // Called when SelectedAtlasElement changes
    partial void OnSelectedAtlasElementChanged(AtlasElement? value)
    {
        if (value == null)
        {
            HighlightVisible = false;
        }
        else
        {
            HighlightVisible = true;
            HighlightX = value.X;
            HighlightY = value.Y;
            HighlightWidth = value.Width;
            HighlightHeight = value.Height;
        }
    }

    // Select atlas element by clicking on the image
    public void SelectAtlasElementAtPosition(double x, double y)
    {
        if (!HasAtlas) return;

        // Find the element that contains this point
        var element = AtlasElements.FirstOrDefault(e =>
            x >= e.X && x <= e.X + e.Width &&
            y >= e.Y && y <= e.Y + e.Height);

        if (element != null)
        {
            SelectedAtlasElement = element;
            StatusMessage = $"Selected: {element.Name}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportFullImage))]
    private void ExportFullImage()
    {
        // This command is handled in the View with file dialog
        // The actual export happens in ExportFullImageToPath
    }

    private bool CanExportFullImage()
    {
        return _loadedImage != null && !string.IsNullOrEmpty(FileName);
    }

    public void ExportFullImageToPath(string outputPath)
    {
        if (_loadedImage == null) return;

        try
        {
            _loader.ExportImage(_loadedImage, outputPath);
            StatusMessage = $"Exported to: {Path.GetFileName(outputPath)}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    [RelayCommand(CanExecute = nameof(CanExportSelectedElement))]
    private void ExportSelectedElement()
    {
        // This command is handled in the View with file dialog
        // The actual export happens in ExportSelectedElementToPath
    }

    private bool CanExportSelectedElement()
    {
        return _loadedImage != null && SelectedAtlasElement != null;
    }

    public void ExportSelectedElementToPath(string outputPath)
    {
        if (_loadedImage == null || SelectedAtlasElement == null) return;

        try
        {
            _loader.ExportAtlasElement(_loadedImage, SelectedAtlasElement, outputPath);
            StatusMessage = $"Exported: {SelectedAtlasElement.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export error: {ex.Message}";
        }
    }

    private Bitmap ConvertToAvaloniaBitmap(Image<Rgba32> sourceImage)
    {
        using var memoryStream = new MemoryStream();
        sourceImage.SaveAsPng(memoryStream);
        memoryStream.Position = 0;
        return new Bitmap(memoryStream);
    }

    // Zoom controls
    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel + ZoomStep, MaxZoom);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel - ZoomStep, MinZoom);
    }

    [RelayCommand]
    private void ZoomReset()
    {
        ZoomLevel = 1.0;
        ResetPan();
    }

    public void ZoomToFit(double zoom)
    {
        ZoomLevel = zoom;
        ResetPan();
    }

    public void HandleMouseWheel(double delta, double mouseX, double mouseY)
    {
        // Use multiplicative zoom for smoother feel
        const double zoomFactor = 1.1; // 10% change per step

        var oldZoom = ZoomLevel;

        // Calculate new zoom using delta directly for smoother scaling
        var zoomChange = Math.Pow(zoomFactor, delta);
        var newZoom = Math.Clamp(oldZoom * zoomChange, MinZoom, MaxZoom);

        // Adjust pan to keep the point under the cursor stationary
        // Transform: screen = (canvas + pan) * zoom
        // We want: (mousePos + panOld) * oldZoom = (mousePos + panNew) * newZoom
        // So: panNew = (mousePos + panOld) * oldZoom / newZoom - mousePos
        if (Math.Abs(newZoom - oldZoom) > 0.001)
        {
            PanX = (mouseX + PanX) * oldZoom / newZoom - mouseX;
            PanY = (mouseY + PanY) * oldZoom / newZoom - mouseY;
        }

        ZoomLevel = newZoom;
    }

    // Handle pan gesture / mouse drag
    public void HandlePan(double deltaX, double deltaY)
    {
        // Adjust delta by zoom level so pan follows mouse exactly
        PanX += deltaX / ZoomLevel;
        PanY += deltaY / ZoomLevel;
    }

    // Reset pan to center
    public void ResetPan()
    {
        PanX = 0;
        PanY = 0;
    }
}
