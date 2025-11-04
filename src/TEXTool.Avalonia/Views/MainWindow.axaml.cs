using System.Linq;
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
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Image_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Image image || DataContext is not MainWindowViewModel viewModel)
            return;

        // Get the position relative to the image
        var position = e.GetPosition(image);

        // The position is already in image coordinates because the Grid
        // has Width/Height set to match the image dimensions
        viewModel.SelectAtlasElementAtPosition(position.X, position.Y);
    }

    private void ScrollViewer_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        // Handle zoom with mouse wheel
        viewModel.HandleMouseWheel(e.Delta.Y);
        e.Handled = true; // Prevent default scrolling
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
