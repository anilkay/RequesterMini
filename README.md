# RequesterMini

Avalonia Base Http request Maker

## Generate Executable (for Smelly nerds)

> Single-file + trimmed build is configured in the project. Just run the publish command.

### Windows Publish
```bash
dotnet publish -r win-x64 --self-contained true -c Release
```

### macOS Publish
```zsh
dotnet publish -r osx-arm64 --self-contained true -c Release
```

### Distribution

The following native files must be distributed **alongside** the exe:

| File | Purpose |
|---|---|
| `av_libglesv2.dll` | Avalonia GPU/OpenGL rendering |
| `libHarfBuzzSharp.dll` | Font shaping |
| `libSkiaSharp.dll` | Skia graphics engine |

## Used Prompts
- [For Styling](https://chat.openai.com/share/56bad6c9-9997-4f55-96d1-5b7714ede4be).