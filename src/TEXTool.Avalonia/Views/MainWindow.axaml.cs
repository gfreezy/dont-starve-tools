using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using TEXTool.Avalonia.ViewModels;

namespace TEXTool.Avalonia.Views;

public partial class MainWindow : Window
{
    private bool _isDragging;
    private Point _dragStartPoint;
    private Point _lastDragPoint;
    private const double DragThreshold = 5.0; // pixels

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Border_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (sender is not Border border || DataContext is not MainWindowViewModel viewModel)
            return;

        // Get mouse position relative to Border (viewport coordinates)
        var mouseInBorder = e.GetPosition(border);

        // Calculate Canvas position in Border (it's centered)
        var borderWidth = border.Bounds.Width;
        var borderHeight = border.Bounds.Height;
        var canvasWidth = viewModel.ImageWidth;
        var canvasHeight = viewModel.ImageHeight;

        // Canvas is centered in Border
        var canvasPosX = (borderWidth - canvasWidth) / 2.0;
        var canvasPosY = (borderHeight - canvasHeight) / 2.0;

        // Apply inverse transform to get Canvas coordinate
        // Forward: screen = canvasPos + (canvas + pan) * zoom
        // Inverse: canvas = (screen - canvasPos) / zoom - pan
        var currentZoom = viewModel.ZoomLevel;
        var mouseInCanvas = new Point(
            (mouseInBorder.X - canvasPosX) / currentZoom - viewModel.PanX,
            (mouseInBorder.Y - canvasPosY) / currentZoom - viewModel.PanY
        );

        // Handle zoom with mouse wheel centered at cursor position
        viewModel.HandleMouseWheel(e.Delta.Y, mouseInCanvas.X, mouseInCanvas.Y);
        e.Handled = true; // Prevent default scrolling
    }

    private void Border_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border border)
            return;

        var properties = e.GetCurrentPoint(border).Properties;

        // Left click - record start point for potential drag
        if (properties.IsLeftButtonPressed)
        {
            _dragStartPoint = e.GetPosition(border);
            _lastDragPoint = _dragStartPoint;
            e.Pointer.Capture(border);
            e.Handled = true;
        }
    }

    private void Border_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not Border border || DataContext is not MainWindowViewModel viewModel)
            return;

        // Only process if left button is pressed
        if (!e.GetCurrentPoint(border).Properties.IsLeftButtonPressed)
            return;

        var currentPoint = e.GetPosition(border);

        // Check if we should start dragging
        if (!_isDragging)
        {
            var distance = Math.Sqrt(
                Math.Pow(currentPoint.X - _dragStartPoint.X, 2) +
                Math.Pow(currentPoint.Y - _dragStartPoint.Y, 2));

            if (distance > DragThreshold)
            {
                _isDragging = true;
                _lastDragPoint = currentPoint;
            }
            return; // Don't pan until drag starts
        }

        // Update pan
        var delta = currentPoint - _lastDragPoint;
        if (delta.X != 0 || delta.Y != 0)
        {
            viewModel.HandlePan(delta.X, delta.Y);
            _lastDragPoint = currentPoint;
        }

        e.Handled = true;
    }

    private void Border_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (sender is not Border border || DataContext is not MainWindowViewModel viewModel)
            return;

        // If we didn't drag (just clicked), handle element selection
        if (!_isDragging)
        {
            // Get mouse position relative to Border
            var mouseInBorder = e.GetPosition(border);

            // Calculate Canvas position in Border (it's centered)
            var borderWidth = border.Bounds.Width;
            var borderHeight = border.Bounds.Height;
            var canvasWidth = viewModel.ImageWidth;
            var canvasHeight = viewModel.ImageHeight;

            var canvasPosX = (borderWidth - canvasWidth) / 2.0;
            var canvasPosY = (borderHeight - canvasHeight) / 2.0;

            // Apply inverse transform to get Canvas coordinate
            var currentZoom = viewModel.ZoomLevel;
            var clickInCanvas = new Point(
                (mouseInBorder.X - canvasPosX) / currentZoom - viewModel.PanX,
                (mouseInBorder.Y - canvasPosY) / currentZoom - viewModel.PanY
            );

            viewModel.SelectAtlasElementAtPosition(clickInCanvas.X, clickInCanvas.Y);
        }

        _isDragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void ZoomToFitButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.ImageWidth == 0 || viewModel.ImageHeight == 0)
            return;

        // Get the actual size of the border (viewport)
        var viewportWidth = ImageBorder.Bounds.Width;
        var viewportHeight = ImageBorder.Bounds.Height;

        if (viewportWidth <= 0 || viewportHeight <= 0)
            return;

        // Calculate zoom to fit while maintaining aspect ratio
        var scaleX = viewportWidth / viewModel.ImageWidth;
        var scaleY = viewportHeight / viewModel.ImageHeight;
        var fitZoom = Math.Min(scaleX, scaleY);

        // Apply some padding (95% of available space)
        fitZoom *= 0.95;

        viewModel.ZoomToFit(fitZoom);
    }

    private async void OpenFileMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open TEX File",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("TEX Files")
                {
                    Patterns = new[] { "*.tex" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0 && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.LoadFile(files[0].Path.LocalPath);
        }
    }

    private async void ExportFullImageMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Full Image",
            SuggestedFileName = "texture.png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG Image")
                {
                    Patterns = new[] { "*.png" }
                }
            }
        });

        if (file != null)
        {
            viewModel.ExportFullImageToPath(file.Path.LocalPath);
        }
    }

    private async void ExportSelectedElementMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel || viewModel.SelectedAtlasElement == null)
            return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Atlas Element",
            SuggestedFileName = $"{viewModel.SelectedAtlasElement.Name}.png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG Image")
                {
                    Patterns = new[] { "*.png" }
                }
            }
        });

        if (file != null)
        {
            viewModel.ExportSelectedElementToPath(file.Path.LocalPath);
        }
    }

    private async void AboutMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        var aboutDialog = new Window
        {
            Title = "About TEX Viewer",
            Width = 400,
            Height = 200,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        aboutDialog.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "TEX Viewer",
                    FontSize = 18,
                    FontWeight = FontWeight.Bold
                },
                new TextBlock
                {
                    Text = "Cross-platform TEX file viewer and exporter",
                    TextWrapping = TextWrapping.Wrap
                },
                new TextBlock
                {
                    Text = "Powered by Avalonia UI and .NET 9",
                    Margin = new Thickness(0, 10, 0, 0)
                },
                new TextBlock
                {
                    Text = "Migrated from Windows Forms to support macOS/Linux",
                    TextWrapping = TextWrapping.Wrap
                }
            }
        };

        await aboutDialog.ShowDialog(this);
    }

    private void ExitMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
