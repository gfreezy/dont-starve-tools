# Don't Starve Tools - Cross-Platform Edition

A modern, cross-platform suite of tools for working with Don't Starve texture files (.tex).

## 🎉 Version 2.0 - Now Cross-Platform!

This version has been completely rewritten using [Avalonia UI](https://avaloniaui.net/) to support **Windows, macOS, and Linux**.

### ✨ New Features

**TEXTool (Texture Viewer)**:
- 🔍 **Cursor-Centered Zoom**: Mouse wheel zoom (10% - 1000%) centered at cursor position
- 🖱️ **Left-Click Pan**: Drag with left mouse button to pan the image
- 🎯 **Click-to-Select**: Click (without dragging) to select atlas elements
- 📍 **Element Highlighting**: Selected atlas elements are highlighted in yellow
- 🖥️ **Native macOS Support**: Proper .app bundles for Apple Silicon

**TEXCreator (Texture Creator)**:
- 🎨 Full cross-platform texture creation
- 📦 Modern MVVM architecture
- 🚀 All original features preserved

### 🍎 macOS ARM64 Support

Native Apple Silicon apps with:
- Proper .app bundle structure
- Custom icons
- Code signing support
- See [BUILD_MACOS.md](BUILD_MACOS.md) for details

## Platform Support

| Platform | Architecture | Status |
|----------|--------------|--------|
| macOS | ARM64 (Apple Silicon) | ✅ Native |
| macOS | x64 (Intel) | ✅ Build script support |
| Windows | x64 | ✅ Supported |
| Linux | x64/ARM64 | ✅ Supported |

## Quick Start

### Download Pre-built Apps

* [Download the latest release](https://github.com/oblivioncth/dont-starve-tools/releases)

### Build from Source

**Prerequisites:**
- .NET 9.0 SDK

**macOS:**
```bash
./build-macos-apps.sh
open "publish/apps/TEX Viewer.app"
open "publish/apps/TEX Creator.app"
```

**Windows/Linux:**
```bash
dotnet run --project src/TEXTool.Avalonia/TEXTool.Avalonia.csproj
dotnet run --project src/TEXCreator.Avalonia/TEXCreator.Avalonia.csproj
```

## Technical Details

### Major Changes from Previous Versions
- **UI Framework**: Windows Forms → Avalonia UI
- **Texture Compression**: libsquish (native DLL) → BCnEncoder.NET (pure C#)
- **Image Processing**: System.Drawing → SixLabors.ImageSharp
- **Runtime**: .NET Framework → .NET 9.0
- **Architecture**: MVVM pattern with CommunityToolkit.Mvvm

### No More Native Dependencies
All functionality is now pure C#, eliminating platform-specific native DLLs and enabling true cross-platform support.

## Fork History

This is a fork of [oblivioncth's fork](https://github.com/oblivioncth/dont-starve-tools), which is a fork of [zxcvbnm3057's fork](https://github.com/zxcvbnm3057/dont-starve-tools) of the original Klei Studio.

**Previous improvements retained:**
- Locale-agnostic atlas element selection fix
- Fixed off-by-one error in texture type reading/writing
- Fixed mipmap pitch value calculation
- Fixed atlas element dimension display

## Legacy Windows Forms Version

The original Windows Forms version is preserved in `src/TEXTool/` and `src/TEXCreator/` but is no longer maintained. Use the Avalonia versions for new development.

## Contributing

If you would like to contribute bug fixes and the likes, just make a pull request.

## Copyright and license

Copyright 2013-2020 Matt Stevens under [the MIT license](LICENSE).
