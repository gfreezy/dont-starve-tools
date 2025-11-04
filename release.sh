#!/bin/bash

# Complete Release Script for TEXTool and TEXCreator
# Builds all platforms (macOS, Linux, Windows) and creates GitHub Release

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored messages
print_message() {
    local color=$1
    local message=$2
    echo -e "${color}${message}${NC}"
}

print_message "$BLUE" "🚀 Klei Studio Release Script"
print_message "$BLUE" "================================"
echo ""

# Check if gh CLI is installed
if ! command -v gh &> /dev/null; then
    print_message "$RED" "❌ Error: GitHub CLI (gh) is not installed."
    echo "Please install it from: https://cli.github.com/"
    exit 1
fi

# Check if user is authenticated with gh
if ! gh auth status &> /dev/null; then
    print_message "$RED" "❌ Error: Not authenticated with GitHub CLI."
    echo "Please run: gh auth login"
    exit 1
fi

# Get version from user
if [ -z "$1" ]; then
    print_message "$YELLOW" "📋 Usage: $0 <version> [release-notes]"
    echo "Example: $0 v1.0.0 \"Initial release\""
    echo ""
    read -p "Enter version (e.g., v1.0.0): " VERSION
else
    VERSION=$1
fi

# Validate version format
if [[ ! "$VERSION" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    print_message "$RED" "❌ Error: Invalid version format. Use format: v1.0.0"
    exit 1
fi

# Get release notes
if [ -z "$2" ]; then
    read -p "Enter release notes (optional): " RELEASE_NOTES
else
    RELEASE_NOTES=$2
fi

if [ -z "$RELEASE_NOTES" ]; then
    RELEASE_NOTES="Release $VERSION"
fi

print_message "$GREEN" "✅ Version: $VERSION"
print_message "$GREEN" "✅ Release notes: $RELEASE_NOTES"
echo ""

# Check if tag already exists
if git rev-parse "$VERSION" >/dev/null 2>&1; then
    print_message "$RED" "❌ Error: Tag $VERSION already exists."
    read -p "Do you want to delete it and continue? (y/N): " confirm
    if [[ $confirm == [yY] ]]; then
        git tag -d "$VERSION"
        git push origin ":refs/tags/$VERSION" 2>/dev/null || true
        print_message "$YELLOW" "⚠️  Deleted existing tag $VERSION"
    else
        exit 1
    fi
fi

# Check for uncommitted changes
if [[ -n $(git status -s) ]]; then
    print_message "$YELLOW" "⚠️  Warning: You have uncommitted changes."
    git status -s
    echo ""
    read -p "Do you want to continue anyway? (y/N): " confirm
    if [[ ! $confirm == [yY] ]]; then
        exit 1
    fi
fi

# Clean previous builds
print_message "$BLUE" "🧹 Cleaning previous builds..."
rm -rf publish/apps
rm -rf publish/packages
rm -rf publish/temp
echo ""

# Build macOS packages
print_message "$BLUE" "🍎 Building macOS packages..."
if [ -f "./build-macos-apps.sh" ]; then
    ./build-macos-apps.sh
    ./codesign-macos-apps.sh

    # Create archives for macOS apps
    cd publish/apps
    if [ -d "TEX Viewer.app" ]; then
        tar -czf "TEXViewer-macOS-arm64.tar.gz" "TEX Viewer.app"
        print_message "$GREEN" "  ✅ Created: TEXViewer-macOS-arm64.tar.gz"
    fi
    if [ -d "TEX Creator.app" ]; then
        tar -czf "TEXCreator-macOS-arm64.tar.gz" "TEX Creator.app"
        print_message "$GREEN" "  ✅ Created: TEXCreator-macOS-arm64.tar.gz"
    fi
    cd "$SCRIPT_DIR"
else
    print_message "$YELLOW" "  ⚠️  macOS build script not found, skipping..."
fi
echo ""

# Build Linux and Windows packages
print_message "$BLUE" "🐧 🪟 Building Linux and Windows packages..."
if [ -f "./build-linux-windows.sh" ]; then
    ./build-linux-windows.sh
else
    print_message "$YELLOW" "  ⚠️  Linux/Windows build script not found, skipping..."
fi
echo ""

# Collect all release artifacts
print_message "$BLUE" "📦 Collecting release artifacts..."
ARTIFACTS=()

# macOS artifacts
if [ -f "publish/apps/TEXViewer-macOS-arm64.tar.gz" ]; then
    ARTIFACTS+=("publish/apps/TEXViewer-macOS-arm64.tar.gz")
fi
if [ -f "publish/apps/TEXCreator-macOS-arm64.tar.gz" ]; then
    ARTIFACTS+=("publish/apps/TEXCreator-macOS-arm64.tar.gz")
fi

# Linux and Windows artifacts
for file in publish/packages/*.{tar.gz,zip}; do
    if [ -f "$file" ]; then
        ARTIFACTS+=("$file")
    fi
done

if [ ${#ARTIFACTS[@]} -eq 0 ]; then
    print_message "$RED" "❌ Error: No artifacts found to release."
    exit 1
fi

print_message "$GREEN" "Found ${#ARTIFACTS[@]} artifacts:"
for artifact in "${ARTIFACTS[@]}"; do
    echo "  - $(basename "$artifact")"
done
echo ""

# Create git tag
print_message "$BLUE" "🏷️  Creating git tag $VERSION..."
git tag -a "$VERSION" -m "Release $VERSION"
print_message "$GREEN" "  ✅ Tag created successfully"
echo ""

# Push tag to remote
print_message "$BLUE" "📤 Pushing tag to remote..."
git push origin "$VERSION"
print_message "$GREEN" "  ✅ Tag pushed successfully"
echo ""

# Create GitHub release
print_message "$BLUE" "🎉 Creating GitHub release..."
gh release create "$VERSION" \
    --title "Release $VERSION" \
    --notes "$RELEASE_NOTES" \
    "${ARTIFACTS[@]}"

if [ $? -eq 0 ]; then
    print_message "$GREEN" "✅ Release created successfully!"
    echo ""
    print_message "$BLUE" "📋 Release Summary:"
    echo "  Version: $VERSION"
    echo "  Artifacts: ${#ARTIFACTS[@]}"
    echo ""
    print_message "$GREEN" "🎊 Release $VERSION is now live!"
    echo ""
    echo "View release at:"
    gh release view "$VERSION" --web
else
    print_message "$RED" "❌ Failed to create release"
    print_message "$YELLOW" "⚠️  Rolling back tag..."
    git tag -d "$VERSION"
    git push origin ":refs/tags/$VERSION" 2>/dev/null || true
    exit 1
fi
