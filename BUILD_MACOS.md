# macOS 应用打包指南

本指南说明如何在 macOS 上构建、签名和分发 TEXTool 和 TEXCreator 应用程序。

## 📋 前置要求

- macOS 11.0 或更高版本
- .NET 9.0 SDK
- Xcode Command Line Tools（用于代码签名）
- （可选）Apple Developer 账号（用于代码签名和公证）

## 🔨 构建应用

### 方法 1：使用构建脚本（推荐）

```bash
chmod +x build-macos-apps.sh
./build-macos-apps.sh
```

这将：
- 发布两个应用程序（TEXTool 和 TEXCreator）
- 创建标准的 `.app` 包结构
- 转换图标为 `.icns` 格式
- 生成 `Info.plist` 文件
- 设置正确的权限

输出位置：`publish/apps/`

### 方法 2：手动构建

#### 1. 发布应用

```bash
# TEXTool
dotnet publish src/TEXTool.Avalonia/TEXTool.Avalonia.csproj \
  -r osx-arm64 \
  --configuration Release \
  -p:UseAppHost=true \
  -o publish/TEXTool-temp

# TEXCreator
dotnet publish src/TEXCreator.Avalonia/TEXCreator.Avalonia.csproj \
  -r osx-arm64 \
  --configuration Release \
  -p:UseAppHost=true \
  -o publish/TEXCreator-temp
```

#### 2. 创建 .app 包结构

```bash
# TEXTool
mkdir -p "publish/apps/TEX Viewer.app/Contents/MacOS"
mkdir -p "publish/apps/TEX Viewer.app/Contents/Resources"

# 复制文件
cp -a publish/TEXTool-temp/. "publish/apps/TEX Viewer.app/Contents/MacOS/"

# 设置权限
chmod +x "publish/apps/TEX Viewer.app/Contents/MacOS/TEXTool.Avalonia"
```

#### 3. 创建 Info.plist

查看 `build-macos-apps.sh` 中的 `create_info_plist` 函数作为模板。

## 🔐 代码签名（可选但推荐）

如果你有 Apple Developer 账号，可以对应用进行签名以避免安全警告。

### 准备工作

1. **获取开发者证书**
   - 登录 [Apple Developer](https://developer.apple.com/)
   - 创建 "Developer ID Application" 证书
   - 下载并安装到钥匙串

2. **查找签名身份**
   ```bash
   security find-identity -v -p codesigning
   ```

   输出示例：
   ```
   1) XXXX "Developer ID Application: Your Name (TEAMID)"
   ```

### 签名应用

```bash
# 设置签名身份
export CODESIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)"

# 运行签名脚本
chmod +x codesign-macos-apps.sh
./codesign-macos-apps.sh
```

### 公证应用（可选）

公证（Notarization）可以让应用在 macOS 10.15+ 上更顺利地运行。

1. **生成 App-Specific Password**
   - 访问 https://appleid.apple.com/
   - 生成应用专用密码

2. **设置环境变量**
   ```bash
   export APPLE_ID="your@email.com"
   export APPLE_ID_PASSWORD="xxxx-xxxx-xxxx-xxxx"  # App-specific password
   export TEAM_ID="YOUR_TEAM_ID"
   export CODESIGN_IDENTITY="Developer ID Application: Your Name (TEAMID)"
   ```

3. **运行签名和公证**
   ```bash
   ./codesign-macos-apps.sh
   ```

## 🚀 运行应用

### 本地测试

```bash
open "publish/apps/TEX Viewer.app"
open "publish/apps/TEX Creator.app"
```

### 首次运行（未签名应用）

如果应用未签名，macOS 会显示安全警告：

1. 右键点击应用 → 选择"打开"
2. 点击"打开"确认
3. 或者在"系统偏好设置" → "安全性与隐私"中允许

## 📦 分发应用

### 创建分发包

```bash
cd publish/apps

# 压缩应用
tar -czf TEXViewer-macOS-arm64.tar.gz "TEX Viewer.app"
tar -czf TEXCreator-macOS-arm64.tar.gz "TEX Creator.app"

# 或创建 DMG（需要额外工具）
# hdiutil create -volname "TEX Viewer" -srcfolder "TEX Viewer.app" -ov TEXViewer.dmg
```

### 文件清单

发布后你将得到：

```
publish/apps/
├── TEX Viewer.app          # TEXTool 应用包
├── TEX Creator.app         # TEXCreator 应用包
├── TEXViewer-macOS-arm64.tar.gz   # 压缩包（可选）
└── TEXCreator-macOS-arm64.tar.gz  # 压缩包（可选）
```

## 🎯 支持的架构

当前脚本构建 **ARM64（Apple Silicon）** 版本。

### 构建 x64 版本（Intel Mac）

修改 `-r osx-arm64` 为 `-r osx-x64`

### 构建通用二进制（Universal Binary）

需要分别构建两个架构，然后使用 `lipo` 合并：

```bash
# 1. 构建两个架构
dotnet publish -r osx-arm64 ...
dotnet publish -r osx-x64 ...

# 2. 使用 lipo 合并
lipo -create \
  "path/to/arm64/TEXTool.Avalonia" \
  "path/to/x64/TEXTool.Avalonia" \
  -output "TEXTool.Avalonia"
```

## 🔧 故障排除

### 应用无法打开

**问题：** "App is damaged and can't be opened"

**解决：**
```bash
xattr -cr "publish/apps/TEX Viewer.app"
```

### 缺少权限

**问题：** 应用无法执行

**解决：**
```bash
chmod +x "publish/apps/TEX Viewer.app/Contents/MacOS/TEXTool.Avalonia"
```

### 签名验证失败

**检查签名：**
```bash
codesign --verify --deep --strict --verbose=2 "publish/apps/TEX Viewer.app"
```

**查看签名信息：**
```bash
codesign -dv --verbose=4 "publish/apps/TEX Viewer.app"
```

### Gatekeeper 检查

```bash
spctl --assess --verbose=4 --type execute "publish/apps/TEX Viewer.app"
```

## 📚 参考资料

- [Avalonia macOS Deployment 官方文档](https://docs.avaloniaui.net/docs/deployment/macOS)
- [Apple Code Signing Guide](https://developer.apple.com/library/archive/documentation/Security/Conceptual/CodeSigningGuide/)
- [Apple Notarization Guide](https://developer.apple.com/documentation/security/notarizing_macos_software_before_distribution)

## 🆘 获取帮助

如果遇到问题：

1. 检查 .NET 版本：`dotnet --version`
2. 检查 Xcode 工具：`xcode-select -p`
3. 查看构建日志输出
4. 提交 Issue 到项目仓库
