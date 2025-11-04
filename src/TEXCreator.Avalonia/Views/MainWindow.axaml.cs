using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TEXCreator.Avalonia.ViewModels;

namespace TEXCreator.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private async void AddFilesButton_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select PNG Files",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("PNG Images")
                {
                    Patterns = new[] { "*.png" }
                },
                new FilePickerFileType("All Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0 && DataContext is MainWindowViewModel viewModel)
        {
            var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
            viewModel.AddFilesFromPicker(filePaths);
        }
    }

    private async void BrowseOutputButton_Click(object? sender, RoutedEventArgs e)
    {
        var folder = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Output Directory",
            AllowMultiple = false
        });

        if (folder.Count > 0 && DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SetOutputDirectory(folder[0].Path.LocalPath);
        }
    }
}
