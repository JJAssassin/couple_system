# 发布流程

> 给"我们的小世界"发新版本时**必做**的几件事。漏了哪件都可能让用户收不到更新。

## 〇、一条命令发版（推荐）

```bash
cd D:\Code\My_vscode\couple-love-system
python scripts/release.py 1.4 --changelog "• 更新说明\n• 第二行"   # 常规发版
python scripts/release.py 1.4 --bump-android                        # Android 本期也要重装时
python scripts/release.py 1.4 --dry-run                              # 先预览改动
```

`release.py` 自动完成：bump `build.gradle`（versionCode/versionName）+ 写部署目录 `version.json`（含 apkUrl 固定直链）+ git commit & push。
push 自动触发 **CI + iOS Build + Android Build** 三个 workflow，两端云构建完成后即发版完成，无需手动操作。

> 只有以下情况需要人工补充：
> - **改启动屏/图标** → `python scripts/generate_ios_splash.py` 重出后再发版
> - **iOS 签名** → 仓库 Secrets 配 `IOS_SIGNING_CERT_BASE64` 等后 workflow 自动正式签名；未配置则产未签名 ipa 供全能签
> - **Android 正式签名** → Secrets 配 `ANDROID_KEYSTORE_BASE64` 等（否则 debug 签名包，覆盖安装需先卸载旧版）

## 一、改动落库（GitHub 仓库 `JJAssassin/couple_system`）

按改动类型分：

| 改动 | 文件 |
|---|---|
| 启动屏/图标 | `mobile/ios/App/App/Assets.xcassets/Splash.imageset/*.png`<br>（用 `python scripts/generate_ios_splash.py` 一键重出） |
| 前端（含更新提示/通知/UI） | `frontend/src/**` → `docker compose up -d --build frontend` |
| 后端 | `backend/**` → `docker compose up -d --build backend` |

## 二、bump 版本号（脚本自动做，手动可跳过）

| 平台 | 文件 | 字段 |
|---|---|---|
| Android | `mobile/android/app/build.gradle` | `versionCode` + `versionName`（release.py 自动） |
| iOS | `mobile/ios/App/App.xcodeproj`（原生工程） | `MARKETING_VERSION` / `CURRENT_PROJECT_VERSION`（与 version.json 对齐即可，脚本不强制改） |
| 分发清单 | `D:\Docker\couple-love-system\app\version.json` | `versionName` / `versionCode` / `androidVersionCode` / `apkUrl`（release.py 自动） |

## 三、版本清单字段说明（release.py 自动写）

文件：**`D:\Docker\couple-love-system\app\version.json`**（部署配置，不入 Git；容器挂载宿主机文件，改完即生效）

```jsonc
{
  "versionName": "1.3",          // 与 build.gradle 对齐
  "versionCode": 4,              // iOS 侧：> 已安装用户 build 才提示重装（原生启动屏等资源改动才需要）
  "androidVersionCode": 3,       // Android 侧：远程模式常驻最新，无原生改动就保持不提示
  "changelog": "• ...\n• ...",   // 多行用 \n
  "apkUrl": "https://github.com/JJAssassin/couple_system/releases/download/android-v1.3/app-release.apk",  // 固定直链（Android workflow 发布）
  "releaseUrl": "https://github.com/JJAssassin/couple_system/releases/latest",  // iOS 下载页
  "minSupportedCode": 1          // < 此版本的弹"必须升级"
}
```

> **为什么 `apkUrl` 用 GitHub 直链而不用本机 `/app/`？** Android 走 `UpdatePlugin.downloadAndInstall()` 必须可公网访问的 APK 直链；GitHub Release 的 `android-v{versionName}` tag 由 Android workflow 幂等发布（同名覆盖），链接固定不变。iOS 没有 App Store，靠 Releases 页面引导全能签重装。

## 四、Android 云构建（自动）

- 触发：push 到 `mobile/android/**` 或 Actions 手动 Run。
- 产物：`app-release.apk`，自动发布到 GitHub Release `android-v{versionName}`（幂等覆盖）。
- 签名：有 `ANDROID_KEYSTORE_BASE64` 等 4 个 Secrets → 正式签名；否则 debug 签名（可安装，覆盖安装需先卸载旧版，数据在云端无损）。
- 下载直链：`https://github.com/JJAssassin/couple_system/releases/download/android-v{versionName}/app-release.apk`

## 五、发 iOS ipa（自动）

1. push 到 `mobile/**` 自动触发 **iOS Build**（或 Actions → iOS Build → Run workflow）
2. workflow 产物 **`App-unsigned.ipa`** 自动作为 GitHub Release 资产发布（Latest）
3. iPhone 上用**全能签**下载 → 签名 → 安装（覆盖旧版）
4. 打开 App 即可看到新版启动屏

## 六、验证清单

- [ ] `curl https://7182629.xyz/app/version.json` 返回新版本
- [ ] `curl -I "https://github.com/JJAssassin/couple_system/releases/download/android-vX.Y/app-release.apk"` 返回 200
- [ ] `https://github.com/JJAssassin/couple_system/releases/latest` 有 `App-unsigned.ipa`
- [ ] Android 端打开 App 看到新版本提示 → 立即更新成功（或远程模式打开即最新，无提示属正常）
- [ ] iOS 端打开 App 看到新版本提示 → 下载 → 全能签重装
- [ ] iOS 重装后启动屏是新版
