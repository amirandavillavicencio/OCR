# Build y publicación

## Requisitos previos

- .NET 8 SDK
- Windows x64
- Tesseract OCR (con carpeta `tessdata`)

## Configurar Tesseract

Opciones soportadas:

1. Variable de entorno `TESSERACT_PATH` apuntando al directorio con `tesseract.exe`.
2. O tener `tesseract.exe` disponible en `PATH`.

La aplicación muestra un mensaje explícito en UI si OCR no está disponible.

## Comandos

```bash
dotnet restore AppPortable.sln
dotnet build AppPortable.sln -c Release
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj \
  -c Release -r win-x64 --self-contained true \
  /p:PublishSingleFile=true \
  /p:IncludeNativeLibrariesForSelfExtract=true
```
