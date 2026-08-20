#!/usr/bin/env bash
# 情侣小世界 · 一键健康检查
# 用法:
#   bash healthcheck.sh            # 完整检查（容器 + 本地 + 外网 + 登录探测）
#   bash healthcheck.sh --light    # 仅容器 + 本地/外网可达性（不探登录）
#   bash healthcheck.sh --inspect  # 完整检查 + 跑每日巡检（daily_maintenance.py --inspect-only）
#
# 依赖: docker、curl（本机已具备）；需在能访问 localhost:8080 与 7182629.xyz 的机器上运行。
set -u

DOMAIN="7182629.xyz"
LOCAL="http://localhost:8080"
EXT="https://${DOMAIN}"
API_LOCAL="${LOCAL}/api"
API_EXT="${EXT}/api"
USER="partner_a"
PASS="123456"
MAINT="${MAINT_SCRIPT:-D:/Item/cap/workbuddy/scripts/daily_maintenance.py}"

C_RESET=$'\033[0m'; C_OK=$'\033[32m'; C_WARN=$'\033[33m'; C_ERR=$'\033[31m'
C_BOLD=$'\033[1m'; C_DIM=$'\033[2m'; C_CYAN=$'\033[36m'
ok()   { printf "  ${C_OK}✓${C_RESET} %s\n" "$1"; }
warn() { printf "  ${C_WARN}⚠${C_RESET} %s\n" "$1"; }
err()  { printf "  ${C_ERR}✗${C_RESET} %s\n" "$1"; overall=1; }
sec()  { printf "\n${C_BOLD}${C_CYAN}▶ %s${C_RESET}\n" "$1"; }

overall=0
LIGHT=0; INSPECT=0
for a in "$@"; do
  case "$a" in
    --light)   LIGHT=1 ;;
    --inspect) INSPECT=1 ;;
  esac
done

echo "${C_BOLD}❤ 情侣小世界 · 健康检查${C_RESET}  ${C_DIM}$(date '+%Y-%m-%d %H:%M:%S')${C_RESET}"

sec "1) 容器状态 (couple_*)"
if ! command -v docker >/dev/null 2>&1; then
  err "docker 未找到 —— 请先启动 Docker Desktop"
else
  mapfile -t lines < <(docker ps -a --filter "name=couple_" --format "{{.Names}}|{{.Status}}" 2>/dev/null)
  if [ ${#lines[@]} -eq 0 ]; then
    err "未发现任何 couple_ 容器（服务可能未启动：docker compose -f D:/Docker/couple-love-system/docker-compose.yml up -d）"
  else
    for l in "${lines[@]}"; do
      name="${l%%|*}"; st="${l#*|}"
      if [[ "$st" == Up* ]]; then ok "$name — $st"; else err "$name — $st"; fi
    done
  fi
fi

sec "2) 本地访问 (${LOCAL})"
code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 8 "${LOCAL}/" 2>/dev/null)
if [ "$code" = "200" ]; then ok "本机 Web 返回 200"; else err "本机 Web HTTP $code"; fi

sec "3) 外网访问 (${EXT})"
code=$(curl -s -o /dev/null -w '%{http_code}' --max-time 12 "${EXT}/" 2>/dev/null)
if [ "$code" = "200" ]; then ok "公网 Web 返回 200"; else err "公网 Web HTTP $code（检查 cloudflared 容器 / Cloudflare 隧道）"; fi

if [ "$LIGHT" -eq 0 ]; then
  sec "4) 登录探测 (账号 ${USER})"
  for base in "$API_LOCAL" "$API_EXT"; do
    resp=$(curl -s --max-time 12 -X POST "$base/auth/login" \
      -H "Content-Type: application/json" \
      -d "{\"userName\":\"$USER\",\"password\":\"$PASS\"}" 2>/dev/null)
    rc=$(printf '%s' "$resp" | grep -o '"code":[0-9]*' | head -1 | grep -o '[0-9]*')
    if [ "$rc" = "200" ]; then ok "登录成功 @ $base"; else err "登录失败 (code=$rc) @ $base"; fi
  done
fi

if [ "$INSPECT" -eq 1 ]; then
  sec "5) 每日巡检 (daily_maintenance.py --inspect-only)"
  if [ -f "$MAINT" ]; then
    python "$MAINT" --inspect-only || overall=1
  else
    warn "未找到维护脚本: $MAINT（可用 MAINT_SCRIPT 环境变量指定）"
  fi
fi

echo
if [ "$overall" -eq 0 ]; then
  printf "${C_OK}${C_BOLD}✅ 一切正常，服务可访问${C_RESET}\n"
else
  printf "${C_ERR}${C_BOLD}❌ 存在异常，见上方红色项${C_RESET}\n"
fi
exit "$overall"
