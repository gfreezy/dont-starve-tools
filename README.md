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

| Platform | Architecture | Status | Build Script | Package Format |
|----------|--------------|--------|--------------|----------------|
| macOS | ARM64 (Apple Silicon) | ✅ Native | `build-macos-apps.sh` | `.app` bundle → `.tar.gz` |
| Windows | x64 | ✅ Supported | `build-linux-windows.sh` | `.zip` |
| Windows | ARM64 | ✅ Supported | `build-linux-windows.sh` | `.zip` |
| Linux | x64 | ✅ Supported | `build-linux-windows.sh` | `.tar.gz` |
| Linux | ARM64 | ✅ Supported | `build-linux-windows.sh` | `.tar.gz` |

**Single-file executables**: All packages contain self-contained, single-file executables with no external dependencies.

## Quick Start

### Download Pre-built Apps

* [Download the latest release](https://github.com/gfreezy/dont-starve-tools/releases)

### Build from Source

**Prerequisites:**
- .NET 9.0 SDK

#### Build for All Platforms

**Linux & Windows:**
```bash
# Builds for linux-x64, linux-arm64, win-x64, win-arm64
./build-linux-windows.sh
```

Packages will be created in `publish/packages/`:
- `TEXViewer-linux-x64.tar.gz`
- `TEXViewer-linux-arm64.tar.gz`
- `TEXViewer-win-x64.zip`
- `TEXViewer-win-arm64.zip`
- `TEXCreator-linux-x64.tar.gz`
- `TEXCreator-linux-arm64.tar.gz`
- `TEXCreator-win-x64.zip`
- `TEXCreator-win-arm64.zip`

**macOS:**
```bash
# Builds native .app bundles for Apple Silicon
./build-macos-apps.sh

# Optional: Code sign the apps (requires Apple Developer certificate)
./codesign-macos-apps.sh

# Run the apps
open "publish/apps/TEX Viewer.app"
open "publish/apps/TEX Creator.app"
```

See [BUILD_MACOS.md](BUILD_MACOS.md) for detailed macOS build instructions.

#### Development Mode

Run directly without building packages:

```bash
# Run TEXTool
dotnet run --project src/TEXTool.Avalonia/TEXTool.Avalonia.csproj

# Run TEXCreator
dotnet run --project src/TEXCreator.Avalonia/TEXCreator.Avalonia.csproj
```

## Release Process

### Automated Release Script

The `release.sh` script automates the entire release process:

```bash
# Interactive mode - will prompt for version and release notes
./release.sh

# Or provide directly
./release.sh v1.0.0 "Release notes here"
```

**What it does:**
1. Validates version format (v1.0.0)
2. Checks for uncommitted changes
3. Builds packages for all platforms:
   - macOS ARM64 (.app bundles)
   - Linux x64 and ARM64 (tar.gz)
   - Windows x64 and ARM64 (zip)
4. Creates a git tag
5. Pushes the tag to GitHub
6. Creates a GitHub Release
7. Uploads all packages automatically

**Prerequisites for releases:**
- [GitHub CLI](https://cli.github.com/) installed and authenticated
- Code signing certificates configured (for macOS, see BUILD_MACOS.md)

**Example output:**
```
🚀 Klei Studio Release Script
================================

✅ Version: v1.0.0
✅ Release notes: Initial release

🧹 Cleaning previous builds...
🍎 Building macOS packages...
🐧 🪟 Building Linux and Windows packages...
📦 Collecting release artifacts...
🏷️  Creating git tag v1.0.0...
📤 Pushing tag to remote...
🎉 Creating GitHub release...
✅ Release created successfully!
```

### Manual Release Process

If you prefer to release manually:

```bash
# Build all platforms
./build-macos-apps.sh
./build-linux-windows.sh

# Create tag
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# Create release and upload packages using GitHub CLI
gh release create v1.0.0 \
    --title "Release v1.0.0" \
    --notes "Release notes" \
    publish/apps/*.tar.gz \
    publish/packages/*.{tar.gz,zip}
```

## Project Structure

```
dont-starve-tools/
├── src/
│   ├── TEXTool.Avalonia/       # Texture viewer (cross-platform)
│   ├── TEXCreator.Avalonia/    # Texture creator (cross-platform)
│   ├── KleiLib/                # Core library for .tex file handling
│   └── SquishNET/              # DXT compression library
├── build-macos-apps.sh         # macOS .app bundle builder
├── build-linux-windows.sh      # Linux/Windows package builder
├── codesign-macos-apps.sh      # macOS code signing script
└── release.sh                  # Automated release script
```

## Technical Details

### Major Changes from Previous Versions
- **UI Framework**: Windows Forms → Avalonia UI
- **Texture Compression**: libsquish (native DLL) → BCnEncoder.NET (pure C#)
- **Image Processing**: System.Drawing → SixLabors.ImageSharp
- **Runtime**: .NET Framework → .NET 9.0
- **Architecture**: MVVM pattern with CommunityToolkit.Mvvm
- **Build System**: Automated cross-platform build and release scripts

### No More Native Dependencies
All functionality is now pure C#, eliminating platform-specific native DLLs and enabling true cross-platform support.

### Build Configuration
- **PublishSingleFile**: Creates single executable files
- **SelfContained**: Includes .NET runtime (no installation required)
- **Trimming**: Optimized for smaller file sizes

## Fork History

This is a fork of [oblivioncth's fork](https://github.com/oblivioncth/dont-starve-tools), which is a fork of [zxcvbnm3057's fork](https://github.com/zxcvbnm3057/dont-starve-tools) of the original Klei Studio.

**Previous improvements retained:**
- Locale-agnostic atlas element selection fix
- Fixed off-by-one error in texture type reading/writing
- Fixed mipmap pitch value calculation
- Fixed atlas element dimension display

## Legacy Windows Forms Version

The original Windows Forms version is preserved in `src/TEXTool/` and `src/TEXCreator/` but is no longer maintained. Use the Avalonia versions for new development.

## Troubleshooting

### Linux

**Issue**: "Permission denied" when running the executable
```bash
# Make the file executable
chmod +x TEXTool.Avalonia
```

**Issue**: Missing dependencies on minimal Linux distributions
```bash
# Install required dependencies (Ubuntu/Debian)
sudo apt-get install libice6 libsm6 libfontconfig1

# For Wayland support
sudo apt-get install libwayland-client0
```

### macOS

**Issue**: "Application cannot be opened because the developer cannot be verified"
```bash
# Remove quarantine attribute
xattr -d com.apple.quarantine "TEX Viewer.app"
xattr -d com.apple.quarantine "TEX Creator.app"
```

**Issue**: App signing verification errors
- See [BUILD_MACOS.md](BUILD_MACOS.md) for code signing instructions
- Or use the unsigned apps with the command above

### Windows

**Issue**: "Windows protected your PC" SmartScreen warning
- Click "More info" → "Run anyway"
- This happens with unsigned executables

### Build Issues

**Issue**: .NET SDK not found
```bash
# Check .NET version
dotnet --version

# Should be 9.0 or higher
# Download from: https://dotnet.microsoft.com/download
```

**Issue**: GitHub CLI authentication for releases
```bash
# Authenticate with GitHub
gh auth login

# Check authentication status
gh auth status
```

## Contributing

If you would like to contribute bug fixes and the likes, just make a pull request.

## Copyright and license

Copyright 2013-2020 Matt Stevens under [the MIT license](LICENSE).
