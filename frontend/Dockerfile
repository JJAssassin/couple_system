# ---- 构建阶段 ----
FROM node:20-alpine AS build
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .
# npm run build = vue-tsc --noEmit && vite build（含类型检查，保证产物可编译）
RUN npm run build

# ---- 运行阶段 ----
# 关键：Docker 官方 nginx 镜像的 core 是源码编译，与 apk 仓库编译的 brotli 模块 ABI 不兼容，
# 会报 "module ... is not binary compatible"，brotli_static 无法加载。
# 改用纯 alpine + 同一次 apk 构建安装 nginx 与 nginx-mod-http-brotli，二者 ABI 一致，
# 模块由 /etc/nginx/modules/10_http_brotli.conf 自动 load_module（被主配置 include）。
# 代价：nginx 版本跟随 Alpine stable（3.20 → nginx 1.26.3），对个人情侣应用无影响。
FROM alpine:3.20 AS runtime
RUN apk add --no-cache nginx nginx-mod-http-brotli
COPY nginx.conf /etc/nginx/nginx.conf
COPY security-headers.conf /etc/nginx/security-headers.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
