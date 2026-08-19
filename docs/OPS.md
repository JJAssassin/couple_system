# 🛠️ 运维手册

## 1. 每日自动化（备份 + 健康巡检）

- **脚本**：`D:\Item\cap\workbuddy\scripts\daily_maintenance.py`（python 3.13）
- **定时**：WorkBuddy 自动化，每天 **02:30** 执行（id `automation-1787132292876`）
- **覆盖**：
  - 备份 → `D:\Item\cap\workbuddy\backups\`（保留 14 天）：
    - `mysql-couple_love-YYYYMMDD.sql`（mysqldump，root 凭据读 `.env`）
    - `uploads-YYYYMMDD.tar.gz`（uploads-data 命名卷）
    - `secrets/`（JWT 私钥 / APK keystore / 密码）
  - 巡检：5 容器状态 / 后端日志 24h 错误计数（>10 告警）/ nginx 5xx（>20 告警）/ API 冒烟（本机 `http://localhost:8080`，登录 + 3 核心接口）/ D 盘使用率（>85% 告警）
- 异常时脚本 exit 1，自动化会列出异常 + 排查建议

```bash
# 手动执行一次
python D:/Item/cap/workbuddy/scripts/daily_maintenance.py
```

## 2. 数据恢复

```bash
# MySQL 恢复（例：恢复 20260819 的备份）
docker exec -i couple_mysql mysql -uroot -p<密码> couple_love < D:/Item/cap/workbuddy/backups/mysql-couple_love-20260819.sql

# uploads 卷恢复
docker run --rm -v uploads-data:/data -v D:/Item/cap/workbuddy/backups:/backup alpine sh -c \
  "rm -rf /data/* && tar xzf /backup/uploads-20260819.tar.gz -C /data"

# secrets 恢复：拷贝回 D:/Docker/couple-love-system/secrets/ 后重启 backend
```

## 3. 常见故障排查

| 症状 | 排查 | 处理 |
|---|---|---|
| 页面白屏（旧 JS 缓存） | 浏览器硬刷新 Ctrl+Shift+R | 已配 no-cache，刷一次永久稳定 |
| 后端 502 Bad Gateway（重建后） | `docker logs couple_frontend` | nginx resolver 10s 自愈；等 10s 重试 |
| 后端启动即崩「MySQL 连接」 | `docker logs couple_backend` | 检查 mysql 容器 healthy；`app@'%'` 授权（重装 MySQL 后需重做） |
| 生产「Redis 不可达」拒绝启动 | 检查 redis 容器 | fail-fast 设计如此；先起 redis |
| 登录后点功能被踢回登录页（国产浏览器） | 浏览器设置「无痕/退出时清除数据」 | 关掉；token 已做三层持久化兜底 |
| 域名打不开 | `docker logs couple_cloudflared`、CF 面板 DNS | 隧道连接 / CNAME 记录（`<tunnel-id>.cfargotunnel.com`） |
| App 不弹更新 | 检查 `https://域名/app/version.json` | versionCode 必须 > 当前；APK 与清单同目录 |
| 磁盘告警 | `docker system df` | `docker builder prune -f`（安全回收构建缓存） |
| 账号被登录限速锁定（429） | 锁定期 15 分钟自动恢复 | 应急解锁：`docker exec couple_redis redis-cli DEL "cache:ratelimit:login:u:<用户名>"`（限速计数存 Redis，key 带 `cache:` 前缀；锁定期内登录成功也会被 Check 拦截，属安全设计） |

## 4. 安全加固（方向#4 已落地）

- **登录防爆破**：`LoginRateLimiter`（IP 15 分钟 10 次 / 账号 15 分钟 5 次失败 → 429 `RateLimitedException`）；计数经 `ICacheService` 存 Redis（key `cache:ratelimit:*`，跨重启持久）；登录成功自动清账号计数
- **安全响应头**（nginx `security-headers.conf`，每个带 add_header 的 location include）：`X-Frame-Options: DENY`、`X-Content-Type-Options: nosniff`、`Referrer-Policy: strict-origin-when-cross-origin`、`Permissions-Policy` 禁用相机/麦克风/定位/支付（首页 HTML 可能被 CF 边缘覆写为 `SAMEORIGIN`，防护等效）
- **未启用 CSP**：Vue/naive/ECharts 依赖内联样式与 blob/data，需先收敛资源再启用
- 现有基线：JWT RSA-2048 非对称（私钥外置）、生产禁 InMemory TokenStore（fail-fast）、BCrypt 密码哈希、HtmlSanitizer 富文本净化、EF 参数化、GitHub CI 测试门禁

## 4. 安全注意

- `secrets/`（JWT 私钥、keystore、密码）与 `.env` 均不入库；克隆仓库后按 `.env.example` 重建
- 曾泄露的 GitHub PAT 已建议 revoke
- JWT 为 RSA-2048 非对称，私钥仅后端持有；生产禁 InMemory TokenStore（启动校验）
- 输入均有 HtmlSanitizer 白名单净化 + EF 参数化查询

## 5. 版本与备份核对

- git remote：`git@github.com:JJAssassin/couple_system.git`（SSH 22 端口）
- 分支：`master`；每次提交均推送远端
- 备份恢复演练建议：每季度手动恢复一次到临时库验证备份可用
