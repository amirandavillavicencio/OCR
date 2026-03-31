# Arquitectura

## Capas

- **Core**: contratos del dominio y `DocumentPipelineService`.
- **Infrastructure**: implementación de extracción PDF (PdfPig), OCR (Tesseract), chunking y persistencia JSON/local.
- **Search**: índice y consulta full-text sobre SQLite FTS5 con ranking BM25.
- **Desktop**: presentación WPF (MVVM) con tres paneles.

## Flujo

1. Se carga PDF.
2. `PdfExtractionService` extrae texto nativo por página.
3. Si página es inválida o corta, se intenta OCR por página.
4. Se genera `ProcessedDocument`.
5. `ChunkingService` produce chunks con overlap.
6. Persistencia en JSON local.
7. `FtsIndexService` indexa chunks.
8. `FtsSearchService` consulta y devuelve snippets.

## Decisiones técnicas

- **PdfPig**: simple, open source, extracción por página sin dependencias pesadas.
- **SQLite FTS5**: búsqueda local performante y portable.
- **WPF**: UI desktop madura para Windows empresarial.
