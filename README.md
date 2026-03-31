# AppPortable

Aplicación de escritorio Windows (.NET 8 + WPF) para procesamiento documental local con pipeline completo:

PDF → extracción nativa/OCR → JSON estructurado → chunking semántico → indexación SQLite FTS5 → búsqueda full-text.

## Requisitos

- Windows x64
- .NET 8 SDK
- Tesseract OCR instalado (opcional pero recomendado)

## Uso rápido

1. `dotnet restore AppPortable.sln`
2. `dotnet build AppPortable.sln -c Release`
3. Ejecuta `AppPortable.Desktop`.
4. Carga PDFs desde la UI.
5. Busca por términos en la columna central.

## Estructura

- `AppPortable.Core`: dominio, interfaces, orquestación.
- `AppPortable.Infrastructure`: PDF/OCR/chunking/json/storage.
- `AppPortable.Search`: índice FTS5 + búsqueda BM25.
- `AppPortable.Desktop`: UI WPF en dark mode.
- `AppPortable.Tests`: pruebas xUnit.
- `docs/`: arquitectura y build/publicación.
