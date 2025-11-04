#!/bin/bash

# macOS Code Signing and Notarization Script
# Based on Avalonia official documentation

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

. $SCRIPT_DIR/.env

# ========== Configuration ==========
# Set these variables or pass as environment variables

# Your Apple Developer ID (e.g., "Developer ID Application: Your Name (TEAMID)")
SIGNING_IDENTITY="${CODESIGN_IDENTITY:-}"

# For notarization (optional)
APPLE_ID="${APPLE_ID:-}"
APPLE_ID_PASSWORD="${APPLE_ID_PASSWORD:-}"  # App-specific password
TEAM_ID="${TEAM_ID:-}"

# ===================================

echo "🔐 macOS Code Signing and Notarization"
echo ""

# Check if signing identity is provided
if [ -z "$SIGNING_IDENTITY" ]; then
    echo "⚠️  No signing identity provided."
    echo ""
    echo "To sign apps, set CODESIGN_IDENTITY environment variable:"
    echo "  export CODESIGN_IDENTITY=\"Developer ID Application: Your Name (TEAMID)\""
    echo ""
    echo "Available identities:"
    security find-identity -v -p codesigning
    echo ""
    read -p "Do you want to continue without signing? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
    SKIP_SIGNING=true
else
    echo "✅ Signing identity: $SIGNING_IDENTITY"
    SKIP_SIGNING=false
fi

echo ""

# Function to create entitlements file
create_entitlements() {
    local output_file=$1

    cat > "$output_file" << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <!-- Allow JIT compilation for .NET runtime -->
    <key>com.apple.security.cs.allow-jit</key>
    <true/>

    <!-- Allow unsigned executable memory (required for .NET) -->
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key>
    <true/>

    <!-- Disable library validation (allows loading of .NET libraries) -->
    <key>com.apple.security.cs.disable-library-validation</key>
    <true/>

    <!-- Apple Events (for automation) -->
    <key>com.apple.security.automation.apple-events</key>
    <true/>
</dict>
</plist>
EOF

    echo "  ✅ Entitlements file created: $output_file"
}

# Function to sign app bundle
sign_app() {
    local app_path=$1
    local app_name=$(basename "$app_path")

    echo "🔏 Signing: $app_name"

    if [ "$SKIP_SIGNING" = true ]; then
        echo "  ⏭️  Skipping (no signing identity)"
        return 0
    fi

    # Create entitlements file
    local entitlements_file="$app_path/Contents/entitlements.plist"
    create_entitlements "$entitlements_file"

    # Sign in order: DLLs first, then dylibs, then main executable, then bundle
    # This ensures dependencies are signed before the binaries that reference them

    # 1. Sign .dll files (managed assemblies) FIRST
    echo "  Signing managed assemblies..."
    find "$app_path/Contents/MacOS" -name "*.dll" | while read -r file; do
        echo "    - $(basename "$file")"
        codesign --force \
            --timestamp \
            --options runtime \
            --sign "$SIGNING_IDENTITY" \
            "$file"
    done

    # 2. Sign .dylib files
    echo "  Signing dynamic libraries..."
    find "$app_path/Contents/MacOS" -name "*.dylib" | while read -r file; do
        echo "    - $(basename "$file")"
        codesign --force \
            --timestamp \
            --options runtime \
            --sign "$SIGNING_IDENTITY" \
            "$file"
    done

    # 3. Sign the main executable LAST (after all dependencies)
    echo "  Signing main executable..."
    # Get executable name from Info.plist
    local bundle_exec=$(defaults read "$app_path/Contents/Info.plist" CFBundleExecutable 2>/dev/null)
    if [ -n "$bundle_exec" ]; then
        local main_exec="$app_path/Contents/MacOS/$bundle_exec"
        if [ -f "$main_exec" ]; then
            echo "    - $(basename "$main_exec")"
            codesign --force \
                --timestamp \
                --options runtime \
                --entitlements "$entitlements_file" \
                --sign "$SIGNING_IDENTITY" \
                "$main_exec"
        fi
    fi

    # Sign the app bundle itself
    # Note: We use --deep here to sign any remaining unsigned components
    echo "  Signing app bundle..."
    codesign --force \
        --deep \
        --timestamp \
        --options runtime \
        --entitlements "$entitlements_file" \
        --sign "$SIGNING_IDENTITY" \
        "$app_path" 2>&1 | grep -v "\.json:" || true

    # Verify signature (without --deep to avoid JSON file issues)
    echo "  Verifying signature..."
    codesign --verify --verbose=2 "$app_path"

    echo "  ✅ $app_name signed successfully"
    echo ""
}

# Function to notarize app
notarize_app() {
    local app_path=$1
    local app_name=$(basename "$app_path" .app)

    echo "📮 Notarizing: $app_name"

    if [ "$SKIP_SIGNING" = true ]; then
        echo "  ⏭️  Skipping (app not signed)"
        return 0
    fi

    if [ -z "$APPLE_ID" ] || [ -z "$APPLE_ID_PASSWORD" ] || [ -z "$TEAM_ID" ]; then
        echo "  ⏭️  Skipping (credentials not provided)"
        echo ""
        echo "  To enable notarization, set:"
        echo "    export APPLE_ID=\"your@email.com\""
        echo "    export APPLE_ID_PASSWORD=\"app-specific-password\""
        echo "    export TEAM_ID=\"YOUR_TEAM_ID\""
        echo ""
        return 0
    fi

    # Create ZIP for notarization
    local zip_file="publish/apps/${app_name}.zip"
    echo "  Creating archive..."
    ditto -c -k --sequesterRsrc --keepParent "$app_path" "$zip_file"

    # Submit for notarization
    echo "  Submitting to Apple notary service..."
    xcrun notarytool submit "$zip_file" \
        --apple-id "$APPLE_ID" \
        --password "$APPLE_ID_PASSWORD" \
        --team-id "$TEAM_ID" \
        --wait

    # Staple the notarization ticket
    echo "  Stapling ticket..."
    xcrun stapler staple "$app_path"

    # Clean up
    rm "$zip_file"

    echo "  ✅ $app_name notarized successfully"
    echo ""
}

# Main execution
echo "📁 Looking for apps in publish/apps/..."
echo ""

if [ ! -d "publish/apps" ]; then
    echo "❌ Error: publish/apps directory not found"
    echo "Run build-macos-apps.sh first"
    exit 1
fi

# Find all .app bundles
app_count=0
for app in publish/apps/*.app; do
    if [ -d "$app" ]; then
        app_count=$((app_count + 1))
        sign_app "$app"
        # notarize_app "$app"
    fi
done

if [ $app_count -eq 0 ]; then
    echo "❌ No .app bundles found in publish/apps/"
    exit 1
fi

echo "🎉 Done! Processed $app_count app(s)"
echo ""

if [ "$SKIP_SIGNING" = true ]; then
    echo "⚠️  Apps were NOT signed (no identity provided)"
    echo "   Apps will show security warnings when opened"
    echo ""
else
    echo "✅ Apps are signed and ready for distribution"
    echo ""

    # Display Gatekeeper info
    echo "📋 Gatekeeper status:"
    for app in publish/apps/*.app; do
        if [ -d "$app" ]; then
            echo "  $(basename "$app"):"
            spctl --assess --verbose=4 --type execute "$app" 2>&1 | grep -E "source=|accepted" || true
        fi
    done
    echo ""
fi

echo "📦 Distribution files:"
ls -lh publish/apps/*.app
echo ""
