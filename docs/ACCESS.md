# 情侣小世界 · 访问指南

> 只属于两个人的小天地。把本文件（或 `access-card.html` 截图）发给对方即可。

## 三种打开方式

| 场景 | 地址 |
| --- | --- |
| 📱 手机 / 异地（最常用） | https://7182629.xyz |
| 💻 跑服务的本机 | http://localhost:8080 |
| 🏠 同一 WiFi（可选） | http://&lt;电脑局域网IP&gt;:8080（电脑命令行 `ipconfig` 查 IPv4） |

- 公网域名经 Cloudflare 隧道，传输全程 HTTPS 加密，无需公网 IP / 端口转发。
- iOS / 安卓原生 App 连接地址同样是 `https://7182629.xyz`。

## 登录账号

- 账号 A：`partner_a`　密码：`123456`
- 账号 B：`partner_b`　密码：`123456`

两人各用各自账号登录，数据自动同步。首次登录后建议在「设置」里改昵称、传头像。

## 原生 App

- **iOS**：用「全能签」安装已签名的 IPA；更新时重装一次即可。
- **安卓**：安装 APK，App 内自动检测新版本并提示更新。

## 管理员运维命令

```bash
# 一键健康检查（容器 / 本地 / 外网 / 登录探测）
bash scripts/healthcheck.sh

# 日常备份 + 巡检（每天 02:30 自动跑，也可手动）
python D:/Item/cap/workbuddy/scripts/daily_maintenance.py

# 重启 / 停止全部服务
docker compose -f D:/Docker/couple-love-system/docker-compose.yml up -d
docker compose -f D:/Docker/couple-love-system/docker-compose.yml down
```

## 安全说明

- 登录有防爆破限速（同 IP / 同账号 15 分钟内多次失败即限流）。
- JWT 采用 RSA 非对称签名，密钥不写在代码里，存于部署目录 `secrets/`。
- 每日自动备份数据库 + 上传文件 + 配置，保留最近 14 天。
