# 容器化部署（docker-compose）

把「情侣小世界」整套服务（MySQL + Redis + 后端 .NET 8 + 前端 Vue3）一键跑起来，互不干扰、可重复部署。

## 架构

```
宿主机 :8080 ──► [frontend / nginx]
                    │ 托管 dist 静态资源
                    │ 反向代理（同源，规避 CORS）
                    │   /api     → backend:5199
                    │   /uploads → backend:5199
                    │   /hub     → backend:5199  (SignalR WebSocket)
                    ▼
              [backend :5199]
                    │  连 mysql:3306（ConnectionStrings__MySql 覆盖）
                    │  连 redis:6379（TokenStore__Configuration 覆盖）
                    │  JWT 私钥挂载 /app/keys/jwt-private.pem（生产强制 RSA）
                    │  上传目录 /app/uploads 持久化（volume）
                    ▼            ▼
              [mysql:3306]   [redis:6379]
```

端口映射（与本机已运行的 3306/6379/5199 错开，避免冲突）：

| 宿主机 | 容器 | 用途 |
|---|---|---|
| 8080 | frontend:80 | **用户访问入口**（浏览器打开 http://localhost:8080）|
| 3307 | mysql:3306 | 仅调试 MySQL 用，正常不直连 |
| 6380 | redis:6379 | 仅调试 Redis 用，正常不直连 |

## 前置

- Docker Engine + docker compose v2（本项目 `docker compose`，非旧版 `docker-compose`）
- 约 2~3 GB 镜像空间（首次构建会拉取 sdk/aspnet/node/nginx/mysql/redis 镜像）

## 步骤

```bash
# 1. 准备环境变量（含数据库密码）
cp .env.example .env
#   按需修改 .env 里的 MYSQL_ROOT_PASSWORD / MYSQL_PASSWORD

# 2. 确保 JWT 私钥存在（已随仓库生成；若丢失可重新生成）
#   openssl genrsa -out secrets/jwt-private.pem 2048
#   该文件已被 .gitignore 忽略，不会入库。

# 3. 构建并启动（--build 强制重新构建镜像）
docker compose up -d --build

# 4. 查看日志
docker compose logs -f backend      # 后端启动 / 迁移 / 运行时日志
docker compose logs -f frontend     # nginx 访问日志

# 5. 停止
docker compose down                 # 停容器（数据卷保留）
docker compose down -v              # 连数据卷一起删（清空数据库）
```

## 健康检查

- 前端：浏览器打开 http://localhost:8080
- 后端 API：http://localhost:8080/api/account/me 应返回 401（未登录门禁正常）
- Swagger：http://localhost:8080/swagger/index.html
- 数据库迁移：后端启动时自动 `MigrateAsync()` 建表；mysql 容器用 `MYSQL_DATABASE` 自动建库 `couple_love`

## 关键安全约定

1. **JWT 私钥不入库**：`secrets/jwt-private.pem` 由 `.gitignore` 忽略；生产环境 `JwtKeyResolver` 强制 RSA，缺失则启动失败。
2. **数据库密码不入库**：`.env` 由 `.gitignore` 忽略，仅 `.env.example` 入库作模板。
3. **上传文件持久化**：`uploads-data` 命名卷，容器重建不丢图片。
4. **CORS 不适用**：前端经 nginx 同源反代访问后端，无需后端开 CORS。

## 与「本机裸跑」的差异

| 项 | 本机裸跑 | docker-compose |
|---|---|---|
| 数据库 | 本机 MySQL（D:\System_Environment）| 容器内 MySQL（端口 3307）|
| 缓存/令牌 | 本机 Redis（6379）| 容器内 Redis（端口 6380）|
| 后端 | dotnet 手动启动 5199 | 容器 backend:5199（compose 网络内）|
| 前端 | vite dev 5174 | nginx 托管 dist，8080 |
| 访问 | localhost:5174 | localhost:8080 |

两套环境数据独立（不同 MySQL 实例）。本机裸跑的数据不会出现在容器里，反之亦然。
