using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IIndexService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
    Task IndexAsync(ProcessedDocument document, CancellationToken cancellationToken = default);
    Task ReindexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default);
}
