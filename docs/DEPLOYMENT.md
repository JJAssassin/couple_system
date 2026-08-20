# 📦 部署手册

## 1. 部署架构（单机 Docker Compose）

```
                 ┌────────────────────────────────────────────┐
 浏览器/App ──▶  Cloudflare 边缘（TLS 终结）                    │
                 └──────────┬─────────────────────────────────┘
                            │ Cloudflare Tunnel（QUIC 反隧道）
                 ┌──────────▼─────────────────────────────────┐
                 │  frontend (nginx:80)  ←── :8080（本机直连）  │
                 │   /api /uploads /hub 反代 → backend         │
                 └──────────┬─────────────────────────────────┘
          ┌─────────────────┼──────────────────┐
    backend (ASP.NET 8:5199)  redis (7)          mysql (8.0.39)
          │  JWT RSA 密钥（secrets/）             命名卷 mysql-data
          │  DataProtection 密钥环（命名卷）
    uploads-data 命名卷（相册图片）
```

五容器：`mysql` / `redis` / `backend` / `frontend` / `cloudflared`。

## 2. 部署配置目录

- **配置文件位置**：`D:\Docker\couple-love-system`
  - `docker-compose.yml`（Caddyfile / certs / backend-pub 等旧方案文件已清理删除）
  - `secrets/`：`jwt-private.pem`（JWT RSA 私钥）、`couple-release.keystore` + `keystore-password.txt`（APK 正式签名）——**均不入库**
  - `.env`：MySQL 密码等（不入库，参照 `.env.example`）
  - `app/`：APK 自动更新分发目录（`couple-love-vX.Y.apk` + `version.json`，**不入库/不入镜像**）
- **源码位置**：`D:\Code\My_vscode\couple-love-system`（compose 的 build.context 指向此处绝对路径，单一来源）

## 3. 一键启动

```bash
cd D:\Docker\couple-love-system
docker compose up -d --build        # 首次含构建，5-10 分钟
```

- 本机访问：`http://localhost:8080`
- 公网访问：`https://7182629.xyz`（Cloudflare Tunnel，见下）

## 4. 公网 HTTPS（Cloudflare Tunnel，免公网 IP）

家用宽带无公网 IP / 封 80/443 的标准解法；隧道免费、证书自动续期。

### 已落地（免绑卡命令行方案）
1. **凭据**：`D:\System_Environment\cloudflared\credentials\`
   - `cert.pem`（授权凭据）、`<tunnel-id>.json`（隧道凭据）、`config.yml`（ingress 配置）
   - 隧道 id：`aed59d37-faab-4d78-8ff0-2618316b04cd`（couple-love）
2. **compose 的 cloudflared 服务**：挂载凭据目录 + `tunnel --config config.yml run`（本地凭据模式，无需 token）
3. **DNS**：Cloudflare 面板 → 站点 `7182629.xyz` → DNS → `CNAME @ → <tunnel-id>.cfargotunnel.com`（Proxied 橙云）

### 常见操作
```bash
# 查看隧道状态
docker logs couple_cloudflared --tail 20

# 升级 cloudflared
docker pull cloudflare/cloudflared:latest && docker compose up -d cloudflared

# 凭据泄露处理：面板删隧道重建 → 更新 DNS CNAME + credentials 目录
```

## 5. 安卓 APK 构建与自动更新

### 构建环境（本机已装，`D:\System_Environment\`）
- `jdk-21`（Capacitor 7 要求 source 21；**JAVA_HOME 必须指 jdk-21**，17 会报「无效的源发行版:21」）
- `Android\`：commandline-tools + platform-tools + platforms;android-36 + build-tools;34.0.0

### 构建
```bash
cd D:\Code\My_vscode\couple-love-system\mobile\android
$env:JAVA_HOME="D:\System_Environment\jdk-21"; $env:ANDROID_HOME="D:\System_Environment\Android"
.\gradlew.bat assembleRelease --no-daemon
# 产物：app\build\outputs\apk\release\app-release.apk（正式签名 CN=Couple Love）
```

### 发布新版本（用户 App 自动更新）
```bash
# 1. 构建（见上）
# 2. 拷贝 APK + 更新清单
cp app-release.apk D:/Docker/couple-love-system/app/couple-love-vX.Y.apk
# 3. 编辑 D:/Docker/couple-love-system/app/version.json：versionCode +1、versionName、url
# 4. 无需重启（挂载目录 nginx 热读）
```
> 用户 App 下次打开自动检测 version.json，弹「立即更新」→ 系统下载 → 安装。
> **注意**：versionCode 递增即可；签名变更需先卸载旧版。

## 6. 发版流程速查（Web 内容）

```bash
# 后端 / 前端代码改动
cd D:\Docker\couple-love-system
docker compose up -d --build backend frontend
# App 在线加载 → 立即生效；PWA 用户硬刷新（Ctrl+Shift+R）一次
```

## 7. 数据与备份

- 数据卷：`mysql-data` / `redis-data` / `uploads-data` / `data-protection-keys`
- 每日 02:30 自动备份 + 健康巡检：见 [运维手册 OPS.md](OPS.md)
