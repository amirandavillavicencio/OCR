using AppPortable.Core.Interfaces;

namespace AppPortable.Infrastructure.Storage;

public sealed class LocalStorageService : ILocalStorageService
{
    public string RootPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AppPortable");
    public string DocumentsPath => Path.Combine(RootPath, "documents");
    public string JsonPath => Path.Combine(RootPath, "json");
    public string ChunksPath => Path.Combine(RootPath, "chunks");
    public string IndexPath => Path.Combine(RootPath, "index");
    public string TempPath => Path.Combine(RootPath, "temp");
    public string LogsPath => Path.Combine(RootPath, "logs");
    public string DatabasePath => Path.Combine(IndexPath, "appportable.db");

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(DocumentsPath);
        Directory.CreateDirectory(JsonPath);
        Directory.CreateDirectory(ChunksPath);
        Directory.CreateDirectory(IndexPath);
        Directory.CreateDirectory(TempPath);
        Directory.CreateDirectory(LogsPath);
    }

    public string CopySourcePdf(string sourcePath, string documentId)
    {
        EnsureDirectories();
        var destination = Path.Combine(DocumentsPath, $"{documentId}.pdf");
        File.Copy(sourcePath, destination, overwrite: true);
        return destination;
    }
}
