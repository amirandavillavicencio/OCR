using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IJsonPersistenceService
{
    Task SaveProcessedDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProcessedDocument>> LoadAllProcessedDocumentsAsync(CancellationToken cancellationToken = default);
}
