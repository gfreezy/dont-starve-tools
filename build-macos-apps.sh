#!/bin/bash

# macOS App Bundle Creation Script for TEXTool and TEXCreator
# Based on Avalonia official documentation

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "🚀 Building macOS Application Bundles..."
echo ""

# Function to convert .ico to .icns
convert_icon() {
    local ico_file=$1
    local icns_file=$2

    echo "  Converting icon: $ico_file -> $icns_file"

    # Create iconset directory
    local tmp_iconset="${icns_file%.icns}.iconset"
    mkdir -p "$tmp_iconset"

    # Extract and convert using sips (macOS built-in tool)
    # First extract to PNG
    local base_png="${tmp_iconset}/base.png"
    sips -s format png "$ico_file" --out "$base_png" 2>/dev/null || {
        echo "  ⚠️  Warning: Could not convert icon, using .ico as fallback"
        # Just copy the .ico file as .icns (not ideal but works)
        cp "$ico_file" "$icns_file"
        rm -rf "$tmp_iconset"
        return 0
    }

    # Create all required icon sizes
    sips -z 16 16     "$base_png" --out "${tmp_iconset}/icon_16x16.png" 2>/dev/null
    sips -z 32 32     "$base_png" --out "${tmp_iconset}/icon_16x16@2x.png" 2>/dev/null
    sips -z 32 32     "$base_png" --out "${tmp_iconset}/icon_32x32.png" 2>/dev/null
    sips -z 64 64     "$base_png" --out "${tmp_iconset}/icon_32x32@2x.png" 2>/dev/null
    sips -z 128 128   "$base_png" --out "${tmp_iconset}/icon_128x128.png" 2>/dev/null
    sips -z 256 256   "$base_png" --out "${tmp_iconset}/icon_128x128@2x.png" 2>/dev/null
    sips -z 256 256   "$base_png" --out "${tmp_iconset}/icon_256x256.png" 2>/dev/null
    sips -z 512 512   "$base_png" --out "${tmp_iconset}/icon_256x256@2x.png" 2>/dev/null
    sips -z 512 512   "$base_png" --out "${tmp_iconset}/icon_512x512.png" 2>/dev/null
    sips -z 1024 1024 "$base_png" --out "${tmp_iconset}/icon_512x512@2x.png" 2>/dev/null

    # Convert iconset to icns
    iconutil -c icns "$tmp_iconset" -o "$icns_file" 2>/dev/null || {
        echo "  ⚠️  Warning: iconutil failed, using .ico as fallback"
        cp "$ico_file" "$icns_file"
    }

    # Clean up
    rm -rf "$tmp_iconset"

    echo "  ✅ Icon created: $icns_file"
}

# Function to create Info.plist
create_info_plist() {
    local output_file=$1
    local bundle_name=$2
    local bundle_display_name=$3
    local bundle_id=$4
    local bundle_executable=$5
    local bundle_icon=$6
    local bundle_version=$7

    cat > "$output_file" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>$bundle_name</string>
    <key>CFBundleDisplayName</key>
    <string>$bundle_display_name</string>
    <key>CFBundleIdentifier</key>
    <string>$bundle_id</string>
    <key>CFBundleVersion</key>
    <string>$bundle_version</string>
    <key>CFBundleShortVersionString</key>
    <string>$bundle_version</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>????</string>
    <key>CFBundleExecutable</key>
    <string>$bundle_executable</string>
    <key>CFBundleIconFile</key>
    <string>$bundle_icon</string>
    <key>LSMinimumSystemVersion</key>
    <string>11.0</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>
EOF

    echo "  ✅ Info.plist created"
}

# Function to build app bundle
build_app_bundle() {
    local project_path=$1
    local app_name=$2
    local display_name=$3
    local bundle_id=$4
    local icon_path=$5
    local output_dir=$6

    echo "📦 Building $display_name..."

    # 1. Publish the application
    echo "  Publishing..."
    dotnet publish "$project_path" \
        -r osx-arm64 \
        --configuration Release \
        -p:UseAppHost=true \
        -p:PublishSingleFile=true \
        -p:SelfContained=true \
        -o "publish/$app_name-temp"

    # 2. Create .app bundle structure
    local app_bundle="$output_dir/$display_name.app"
    echo "  Creating app bundle structure..."

    if [ -d "$app_bundle" ]; then
        rm -rf "$app_bundle"
    fi

    mkdir -p "$app_bundle/Contents/MacOS"
    mkdir -p "$app_bundle/Contents/Resources"

    # 3. Copy published files
    echo "  Copying application files..."
    cp -a "publish/$app_name-temp/." "$app_bundle/Contents/MacOS/"

    # 4. Create Info.plist
    echo "  Creating Info.plist..."
    create_info_plist \
        "$app_bundle/Contents/Info.plist" \
        "$app_name" \
        "$display_name" \
        "$bundle_id" \
        "$app_name" \
        "AppIcon.icns" \
        "1.0.0"

    # 5. Convert and copy icon
    echo "  Processing icon..."
    convert_icon "$icon_path" "$app_bundle/Contents/Resources/AppIcon.icns"

    # 6. Set executable permissions
    echo "  Setting permissions..."
    chmod +x "$app_bundle/Contents/MacOS/$app_name"

    # Clean up temp publish directory
    rm -rf "publish/$app_name-temp"

    echo "  ✅ $display_name.app created successfully!"
    echo ""
}

# Create output directory
mkdir -p publish/apps

# Build TEXTool
build_app_bundle \
    "src/TEXTool.Avalonia/TEXTool.Avalonia.csproj" \
    "TEXTool.Avalonia" \
    "TEX Viewer" \
    "com.klei.textool" \
    "src/TEXTool.Avalonia/Assets/TEXTool.ico" \
    "publish/apps"

# Build TEXCreator
build_app_bundle \
    "src/TEXCreator.Avalonia/TEXCreator.Avalonia.csproj" \
    "TEXCreator.Avalonia" \
    "TEX Creator" \
    "com.klei.texcreator" \
    "src/TEXCreator.Avalonia/Assets/TEXCreator.ico" \
    "publish/apps"

echo "🎉 All applications built successfully!"
echo ""
echo "📁 Location: publish/apps/"
echo ""
echo "🚀 To run:"
echo "   open 'publish/apps/TEX Viewer.app'"
echo "   open 'publish/apps/TEX Creator.app'"
echo ""
echo "📦 To distribute:"
echo "   cd publish/apps"
echo "   tar -czf TEXViewer-macOS-arm64.tar.gz 'TEX Viewer.app'"
echo "   tar -czf TEXCreator-macOS-arm64.tar.gz 'TEX Creator.app'"
echo ""
