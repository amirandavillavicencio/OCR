using System.Windows;
using AppPortable.Desktop.Services;
using AppPortable.Desktop.ViewModels;

namespace AppPortable.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var pipelineService = new DocumentPipelineService();
        var indexService = new SqliteIndexService();
        var searchService = new SqliteSearchService();

        DataContext = new MainViewModel(pipelineService, searchService, indexService);
    }
}
