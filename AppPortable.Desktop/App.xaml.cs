using System.Windows;
using AppPortable.Desktop.ViewModels;
using AppPortable.Infrastructure;
using AppPortable.Search;

namespace AppPortable.Desktop;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        base.OnStartup(e);

        var appDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppPortable");
        Directory.CreateDirectory(appDataDirectory);

        IDocumentPipelineService pipelineService = new DocumentPipelineService(appDataDirectory);
        ISearchService searchService = new SearchService(appDataDirectory);
        var mainViewModel = new MainViewModel(pipelineService, searchService);

        var mainWindow = new MainWindow(mainViewModel);
        mainWindow.Show();
    }
}
