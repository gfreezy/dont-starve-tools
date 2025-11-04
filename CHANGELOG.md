Future changelogs can be found here: https://github.com/handsomematt/dont-starve-tools/releases

## Version 2.0 (2025-11-04) - Cross-Platform Avalonia UI Rewrite

### Major Changes
* **Complete UI Migration**: Migrated from Windows Forms to Avalonia UI for true cross-platform support
* **macOS ARM64 Support**: Native Apple Silicon support with proper app bundles
* **Removed Native Dependencies**: Replaced libsquish DLL with pure C# BCnEncoder.NET library
* **.NET 9.0**: Updated to latest .NET runtime

### TEXTool.Avalonia (New Cross-Platform Version)
* **Cursor-Centered Zoom**:
  - Mouse wheel zoom (10% - 1000%)
  - Zoom centers at cursor position for precise control
  - Smooth multiplicative zoom for natural feel
  - Zoom controls: In, Out, Reset (1:1), Fit to window
  - Real-time zoom percentage display
* **Intuitive Pan & Navigation**:
  - Left-click drag to pan the image
  - Drag threshold prevents accidental panning when clicking
  - Smooth transform-based pan animations
* **Smart Click Selection**:
  - Click (without dragging) to select atlas elements
  - Drag detection distinguishes between pan and select
* **Atlas Element Highlighting**: Selected atlas elements are highlighted in yellow on the preview
* **All Original Features**: Full feature parity with Windows Forms version

### TEXCreator.Avalonia (New Cross-Platform Version)
* Complete rewrite with Avalonia UI
* Modern MVVM architecture with CommunityToolkit.Mvvm
* All original TEXCreator features preserved

### macOS Packaging
* Automated build scripts for creating .app bundles
* Code signing support with entitlements for .NET runtime
* Proper icon conversion (.ico → .icns)
* Documentation in BUILD_MACOS.md

### Technical Improvements
* Pure C# texture compression (BCnEncoder.NET) - no more native DLLs
* Cross-platform image processing with SixLabors.ImageSharp
* MVVM pattern for better code organization and testability
* Async/await patterns for better performance

### Breaking Changes
* Windows Forms versions are now legacy (still available in src/)
* Requires .NET 9.0 runtime

KleiStudio 1.3 ( 01/06/2013 ):
* Applied MIT license to all parts of the project, you can do whatever you want with it now!

* TEXCreator:
	* Added ability to premultiply alpha. Thanks @Ipsquiggle!


TEXTool 1.2 ( Underground Update ):
* Split the texture conversion into it's own unique tool.
* No longer relies on TextureConverter, saving is independent.
* Updated for the Underground update. ( Textures will have to be reconverted! )

TEXTool 1.1:
* Can now convert PNG -> TEX.
* Redone interface.
* Major bug fixes with reading files.

Pre Klei Studio ( TEXTool ):

1.0.2.0:
* You can now open a file with TEXTool by dragging a file onto the .exe
* Fixed a bug where double clicking an item in the file dialog.. Would do nothing.
* General stability bugs.

1.0.1.0:
* Added new save formats:
	* BMP
	* GIF
	* JPEG
	* PNG
* Hotkeys:
	* Ctrl + O: Open
	* Ctrl + S: Save
	* Ctrl + +: Zoom In
	* Ctrl + -: Zoom Out
* Small minor adjustments to the UI to make it easier to use.

1.0.0.0: Initial Release