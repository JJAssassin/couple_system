# ---- 构建阶段 ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# 后端为单项目结构（Api 项目内含 Application/Core/Infrastructure 各层；Tests 不参与发布）
COPY . .
RUN dotnet restore CoupleLoveSystem.Api/CoupleLoveSystem.Api.csproj
RUN dotnet publish CoupleLoveSystem.Api/CoupleLoveSystem.Api.csproj -c Release -o /app/publish --no-restore

# ---- 运行阶段 ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
# 运行时所需目录：上传文件 / JWT 私钥(由宿主机挂载) / 日志
RUN mkdir -p /app/uploads /app/keys /app/logs
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://0.0.0.0:5199
EXPOSE 5199
ENTRYPOINT ["dotnet", "CoupleLoveSystem.Api.dll"]
