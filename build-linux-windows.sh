#!/bin/bash

# Linux and Windows Build Script for TEXTool and TEXCreator
# Builds packages for Linux (x64, arm64) and Windows (x64, arm64)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "🚀 Building Linux and Windows Application Packages..."
echo ""

# Function to build and package application
build_app() {
    local project_path=$1
    local app_name=$2
    local display_name=$3
    local runtime=$4
    local output_dir=$5

    local runtime_name="${runtime//-/_}"
    local output_name="${display_name}-${runtime}"

    echo "📦 Building $display_name for $runtime..."

    # Publish the application
    echo "  Publishing..."
    dotnet publish "$project_path" \
        -r "$runtime" \
        --configuration Release \
        -p:UseAppHost=true \
        -p:PublishSingleFile=true \
        -p:SelfContained=true \
        -o "publish/temp/$app_name-$runtime"

    if [ $? -ne 0 ]; then
        echo "  ❌ Build failed for $display_name ($runtime)"
        return 1
    fi

    # Create package directory
    local package_dir="publish/packages/$output_name"
    mkdir -p "$package_dir"

    # Copy published files
    echo "  Copying files..."
    cp -a "publish/temp/$app_name-$runtime/." "$package_dir/"

    # Set executable permissions for Linux builds
    if [[ "$runtime" == linux-* ]]; then
        echo "  Setting executable permissions..."
        chmod +x "$package_dir/$app_name"
    fi

    # Create archive
    echo "  Creating archive..."
    cd "publish/packages"

    if [[ "$runtime" == linux-* ]]; then
        # Create tar.gz for Linux
        tar -czf "${output_name}.tar.gz" "$output_name"
        echo "  ✅ Created: ${output_name}.tar.gz"
    else
        # Create zip for Windows
        if command -v zip &> /dev/null; then
            zip -r "${output_name}.zip" "$output_name"
            echo "  ✅ Created: ${output_name}.zip"
        else
            echo "  ⚠️  Warning: zip command not found, skipping archive creation"
            echo "  📁 Files available in: publish/packages/$output_name"
        fi
    fi

    cd "$SCRIPT_DIR"

    # Clean up temp directory
    rm -rf "publish/temp/$app_name-$runtime"

    echo "  ✅ $display_name for $runtime completed!"
    echo ""
}

# Create output directories
mkdir -p publish/temp
mkdir -p publish/packages

# Define runtimes to build
RUNTIMES=(
    "linux-x64"
    "linux-arm64"
    "win-x64"
    "win-arm64"
)

echo "📋 Building for the following runtimes:"
for runtime in "${RUNTIMES[@]}"; do
    echo "   - $runtime"
done
echo ""

# Build TEXTool for all runtimes
echo "🔧 Building TEXTool..."
for runtime in "${RUNTIMES[@]}"; do
    build_app \
        "src/TEXTool.Avalonia/TEXTool.Avalonia.csproj" \
        "TEXTool.Avalonia" \
        "TEXViewer" \
        "$runtime" \
        "publish/packages"
done

# Build TEXCreator for all runtimes
echo "🔧 Building TEXCreator..."
for runtime in "${RUNTIMES[@]}"; do
    build_app \
        "src/TEXCreator.Avalonia/TEXCreator.Avalonia.csproj" \
        "TEXCreator.Avalonia" \
        "TEXCreator" \
        "$runtime" \
        "publish/packages"
done

# Clean up temp directory
rm -rf "publish/temp"

echo "🎉 All applications built successfully!"
echo ""
echo "📁 Packages location: publish/packages/"
echo ""
echo "📦 Generated packages:"
ls -lh publish/packages/*.{tar.gz,zip} 2>/dev/null || echo "   No archives found (zip command may not be available)"
echo ""
echo "📋 Available platforms:"
echo "   Linux x64:   TEXViewer-linux-x64.tar.gz, TEXCreator-linux-x64.tar.gz"
echo "   Linux ARM64: TEXViewer-linux-arm64.tar.gz, TEXCreator-linux-arm64.tar.gz"
echo "   Windows x64: TEXViewer-win-x64.zip, TEXCreator-win-x64.zip"
echo "   Windows ARM64: TEXViewer-win-arm64.zip, TEXCreator-win-arm64.zip"
echo ""
echo "🚀 To extract and run (Linux):"
echo "   tar -xzf TEXViewer-linux-x64.tar.gz"
echo "   cd TEXViewer-linux-x64"
echo "   ./TEXTool.Avalonia"
echo ""
echo "🚀 To extract and run (Windows):"
echo "   Extract TEXViewer-win-x64.zip"
echo "   Run TEXTool.Avalonia.exe"
echo ""
