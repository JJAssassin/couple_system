# 发布流程

> 给"我们的小世界"发新版本时**必做**的几件事。漏了哪件都可能让用户收不到更新。

## 一、改动落库（GitHub 仓库 `JJAssassin/couple_system`）

按改动类型分：

| 改动 | 文件 |
|---|---|
| 启动屏/图标 | `mobile/ios/App/App/Assets.xcassets/Splash.imageset/*.png`<br>（用 `python scripts/generate_ios_splash.py` 一键重出） |
| 前端（含更新提示/通知/UI） | `frontend/src/**` → `docker compose up -d --build frontend` |
| 后端 | `backend/**` → `docker compose up -d --build backend` |

## 二、bump 版本号

| 平台 | 文件 | 字段 |
|---|---|---|
| Android | `mobile/android/app/build.gradle` | `versionCode` + `versionName` |
| iOS | iOS Build workflow 的 env（`MARKETING_VERSION` / `CURRENT_PROJECT_VERSION`） | 自动由 git tag 或手动注入 |

## 三、更新版本清单（部署配置，不入 Git）

文件：**`D:\Docker\couple-love-system\app\version.json`**

```jsonc
{
  "versionName": "1.2",          // 和上面 bump 对齐
  "versionCode": 3,              // 严格 > 所有已安装用户的 build
  "changelog": "• ...\n• ...",   // 多行用 \n
  "apkUrl": "https://7182629.xyz/app/couple-love-v1.2.apk",  // Android 直链
  "releaseUrl": "https://github.com/JJAssassin/couple_system/releases/latest",  // iOS 下载页
  "minSupportedCode": 1          // < 此版本的弹"必须升级"
}
```

> **为什么 `apkUrl` 是直链、`releaseUrl` 是页面？** Android 走 `UpdatePlugin.downloadAndInstall()` 必须是 APK 文件直链；iOS 因为没有 App Store，靠 GitHub Releases 页面引导用户用全能签重装。

## 四、丢新 APK（如果有 Android 更新）

把 `app-release.apk` 复制到 **`D:\Docker\couple-love-system\app\`** 并重命名为 `couple-love-v{versionName}.apk`。前端无需重启，nginx 直接 serve 新文件（`/app/` 是卷挂载）。

## 五、发 iOS ipa

1. 推代码到 GitHub（master 分支）
2. Actions → **iOS Build** → Run workflow → 等 ~12 min
3. workflow 产物是 **`App-unsigned.ipa`**，自动作为 GitHub Release 资产发布
4. iPhone 上用**全能签**下载该 ipa → 签名 → 安装（覆盖旧版）
5. 用户打开 App 即可看到新版启动屏 + 自动收不到"新版本"提示（因为 versionCode 已对齐）

## 六、验证清单

- [ ] `curl https://7182629.xyz/app/version.json` 返回新版本
- [ ] `curl https://7182629.xyz/app/couple-love-vX.Y.apk -I` 返回 200 + content-length
- [ ] `https://github.com/JJAssassin/couple_system/releases/latest` 有 `App-unsigned.ipa`
- [ ] Android 端打开 App 看到新版本提示 → 立即更新成功
- [ ] iOS 端打开 App 看到新版本提示（iOS 引导）→ 下载 → 全能签重装
- [ ] iOS 重装后启动屏是新版
