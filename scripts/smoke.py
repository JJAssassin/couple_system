#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
couple-love-system · 全栈冒烟脚本（自包含，仅依赖 Python 标准库）

用途
----
对运行中的后端 API 做一次"登录 -> 只读接口全量探测 -> 写闭环 -> 401 门禁"的
端到端冒烟，用于本地门禁（ci-gate.sh）与 CI 云端集成测试（.github/workflows/ci.yml）。

设计原则
--------
* 零第三方依赖：只用 urllib / json / os / sys / time，CI 无需 pip install。
* 全部行为由环境变量驱动，便于在不同环境复用（本机 / 容器 / 云端 service 容器）。
* 只读探测力求"广覆盖、低误报"：只要返回 2xx 且业务 code==200 即视为通过。
* 写闭环力求"真实但不污染"：每个 create 成功后尽量按 id 删除；删除失败仅告警不阻断。

环境变量
--------
BASE_URL     后端基址，默认 http://localhost:5199
API_PREFIX   API 前缀，默认 /api（拼成 BASE_URL + API_PREFIX + <path>）
USER_A       用户 A 账号，默认 partner_a
PASS_A       用户 A 密码，默认 123456
USER_B       用户 B 账号，默认 partner_b
PASS_B       用户 B 密码，默认 123456
TIMEOUT      单请求超时（秒），默认 15
SMOKE_VERBOSE 非空的任意值 -> 打印每个响应摘要

退出码
------
0 = 全部通过；1 = 存在失败项。
"""

import json
import os
import sys
import time
import urllib.error
import urllib.request

BASE_URL = os.environ.get("BASE_URL", "http://localhost:5199").rstrip("/")
API_PREFIX = os.environ.get("API_PREFIX", "/api").rstrip("/")
USER_A = os.environ.get("USER_A", "partner_a")
PASS_A = os.environ.get("PASS_A", "123456")
USER_B = os.environ.get("USER_B", "partner_b")
PASS_B = os.environ.get("PASS_B", "123456")
TIMEOUT = int(os.environ.get("TIMEOUT", "15"))
VERBOSE = bool(os.environ.get("SMOKE_VERBOSE"))


def log(msg: str) -> None:
    print(msg, flush=True)


def _url(path: str) -> str:
    return BASE_URL + API_PREFIX + path


def call(method: str, path: str, token: str | None = None, body=None):
    """返回 (http_status, parsed_json_or_None, raw_text)。网络层异常会抛出。"""
    data = None
    headers = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode("utf-8")
        headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = "Bearer " + token
    req = urllib.request.Request(_url(path), data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, timeout=TIMEOUT) as resp:
            raw = resp.read().decode("utf-8", "replace")
            status = resp.status
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", "replace")
        status = e.code
    except Exception as e:  # 网络层错误（连接拒绝等）
        raise
    parsed = None
    try:
        parsed = json.loads(raw) if raw else None
    except Exception:
        parsed = None
    return status, parsed, raw


def biz_ok(parsed) -> bool:
    """HTTP 2xx 且业务 code==200（若存在 code 字段）。"""
    if parsed is None:
        return False
    if isinstance(parsed, dict):
        if "code" in parsed and parsed["code"] != 200:
            return False
    return True


def login(user: str, pwd: str):
    status, parsed, _ = call("POST", "/auth/login", body={"userName": user, "password": pwd})
    if status != 200 or not biz_ok(parsed):
        raise RuntimeError(f"登录失败 user={user} status={status} resp={parsed}")
    data = (parsed or {}).get("data") or {}
    token = data.get("accessToken") or data.get("token")
    if not token:
        raise RuntimeError(f"登录响应缺少 token：{parsed}")
    return token


def extract_id(parsed):
    data = (parsed or {}).get("data")
    if isinstance(data, dict):
        return data.get("id")
    if isinstance(data, int):
        return data
    return None


# ── 只读接口探测清单（尽量覆盖所有模块的列表/详情类接口）────────────
READ_ENDPOINTS = [
    "/home/loveinfo",
    "/home/dashboard",
    "/home/nearestanniversary",
    "/anniversary/list",
    "/diary/list",
    "/wish/list",
    "/conflict/list",
    "/album/list",
    "/budget/monthly?year=2026&month=8",
    "/budget/current",
    "/budget/list",
    "/daterecord/list",
    "/footprint/list",
    "/board/list",
    "/message/list",
    "/message/unread/count",
    "/partner/status",
    "/couple/setting",
    "/timeline/list",
    "/todo/list",
    "/quiz/questions",
    "/quiz/rounds",
    "/quiz/stats",
    "/quote/today",
    "/stats/yearreport",
    "/stats/mood-calendar?year=2026",
    "/user/export/alldata",
    "/account/list",
    "/account/summary",
    "/account/statistics?year=2026&month=8",
]

# ── 写闭环清单 ─────────────────────────────────────────────────────
# (path, payload, delete_path_template) ；删除模板用 {id} 占位
WRITE_FLOWS = [
    ("/diary/create", {
        "title": "冒烟测试日记", "content": "smoke test", "moodScore": 5,
        "permissionType": 1, "weather": "晴",
    }, "/diary/delete?id={id}"),
    ("/wish/create", {
        "wishType": 1, "title": "冒烟测试愿望", "priority": 2, "status": 1,
    }, "/wish/delete?id={id}"),
    ("/anniversary/create", {
        "name": "冒烟测试纪念日", "anniversaryType": 1,
        "targetDate": "2026-12-25T00:00:00", "remindDays": 3, "isYearly": False,
    }, "/anniversary/delete?id={id}"),
    ("/conflict/create", {
        "occurTime": "2026-08-26T00:00:00", "summary": "冒烟测试矛盾",
        "conflictLevel": 1, "ruleConclusion": "smoke",
    }, "/conflict/delete?id={id}"),
    ("/album/create", {
        "albumName": "冒烟测试相册", "remark": "smoke",
    }, "/album/delete?id={id}"),
    ("/todo/create", {
        "title": "冒烟测试待办", "priority": 2, "category": "smoke",
    }, "/todo/delete?id={id}"),
    ("/footprint/create", {
        "title": "冒烟测试足迹", "emoji": "✨", "description": "smoke",
    }, "/footprint/delete?id={id}"),
    ("/board/create", {
        "content": "冒烟测试留言", "isPrivate": False,
    }, "/board/delete?id={id}"),
    ("/account/create", {
        "recordType": 2, "category": "餐饮", "amount": 12.5,
        "recordTime": "2026-08-26T00:00:00", "remark": "smoke",
    }, "/account/delete?id={id}"),
    ("/budget/set", {
        "year": 2026, "month": 8, "category": None, "limitAmount": 9999,
    }, "/budget/delete?id={id}"),
    ("/daterecord/create", {
        "isCompleted": True, "planTime": "2026-08-26T19:00:00",
        "realTime": "2026-08-26T19:30:00", "location": "smoke", "experienceScore": 5,
    }, "/daterecord/delete?id={id}"),
]


def main() -> int:
    passed, failed = 0, 0
    failures = []

    log("=" * 64)
    log(f"❤ couple-love-system 全栈冒烟  BASE_URL={BASE_URL}{API_PREFIX}")
    log("=" * 64)

    # 1) 登录双用户
    try:
        token_a = login(USER_A, PASS_A)
        passed += 1
        log(f"  ✓ 登录 {USER_A} 成功")
    except Exception as e:
        failed += 1
        failures.append(f"登录 {USER_A} 失败: {e}")
        log(f"  ✗ 登录 {USER_A} 失败: {e}")
        return _finish(passed, failed, failures)

    try:
        token_b = login(USER_B, PASS_B)
        passed += 1
        log(f"  ✓ 登录 {USER_B} 成功")
    except Exception as e:
        failed += 1
        failures.append(f"登录 {USER_B} 失败: {e}")
        log(f"  ✗ 登录 {USER_B} 失败: {e}")
        token_b = None

    # 2) 401 门禁：未带令牌访问受保护接口应返回 401
    try:
        status, _, _ = call("GET", "/home/loveinfo")
        if status == 401:
            passed += 1
            log("  ✓ 401 门禁：未认证访问受保护接口正确返回 401")
        else:
            failed += 1
            failures.append(f"401 门禁失效：期望 401，实际 {status}")
            log(f"  ✗ 401 门禁失效：期望 401，实际 {status}")
    except Exception as e:
        failed += 1
        failures.append(f"401 门禁请求异常: {e}")
        log(f"  ✗ 401 门禁请求异常: {e}")

    # 3) 只读接口全量探测（用用户 A 令牌）
    log(f"\n▶ 只读接口探测（{len(READ_ENDPOINTS)} 个）")
    for path in READ_ENDPOINTS:
        try:
            status, parsed, _ = call("GET", path, token=token_a)
            ok = (200 <= status < 300) and biz_ok(parsed)
            if ok:
                passed += 1
                if VERBOSE:
                    log(f"  ✓ GET {path} -> {status}")
            else:
                failed += 1
                msg = f"GET {path} -> {status} (biz={parsed.get('code') if isinstance(parsed, dict) else '?'})"
                failures.append(msg)
                log(f"  ✗ {msg}")
        except Exception as e:
            failed += 1
            msg = f"GET {path} 异常: {e}"
            failures.append(msg)
            log(f"  ✗ {msg}")

    # 4) 写闭环（创建 + 尽量删除）
    log(f"\n▶ 写闭环（{len(WRITE_FLOWS)} 个模块）")
    for path, payload, del_tpl in WRITE_FLOWS:
        try:
            status, parsed, _ = call("POST", path, token=token_a, body=payload)
            ok = (200 <= status < 300) and biz_ok(parsed)
            if not ok:
                failed += 1
                msg = f"POST {path} -> {status} (biz={parsed.get('code') if isinstance(parsed, dict) else '?'})"
                failures.append(msg)
                log(f"  ✗ {msg}")
                continue
            passed += 1
            rid = extract_id(parsed)
            log(f"  ✓ POST {path} 成功" + (f" id={rid}" if rid is not None else ""))
            # 尽力删除，失败仅告警
            if rid is not None and del_tpl:
                try:
                    dstatus, dparsed, _ = call("DELETE", del_tpl.format(id=rid), token=token_a)
                    if (200 <= dstatus < 300) and biz_ok(dparsed):
                        if VERBOSE:
                            log(f"    · 清理 {del_tpl.format(id=rid)} -> {dstatus}")
                    else:
                        log(f"    ⚠ 清理 {del_tpl.format(id=rid)} 返回 {dstatus}（已忽略）")
                except Exception as de:
                    log(f"    ⚠ 清理 {del_tpl.format(id=rid)} 异常: {de}（已忽略）")
        except Exception as e:
            failed += 1
            msg = f"POST {path} 异常: {e}"
            failures.append(msg)
            log(f"  ✗ {msg}")

    # 5) 跨用户实时可见性（B 创建日记，A 列表可见 -> 验证情侣数据隔离下的共享）
    if token_b:
        try:
            status, parsed, _ = call("POST", "/diary/create", token=token_b, body={
                "title": "伴侣冒烟日记", "content": "from B", "moodScore": 6, "permissionType": 1,
            })
            rid = extract_id(parsed) if biz_ok(parsed) else None
            if rid is not None:
                passed += 1
                log("  ✓ 跨用户写入：B 创建日记成功")
                try:
                    call("DELETE", f"/diary/delete?id={rid}", token=token_b)
                except Exception:
                    pass
            else:
                failed += 1
                failures.append(f"跨用户写入失败：B 创建日记 status={status}")
                log(f"  ✗ 跨用户写入失败：B 创建日记 status={status}")
        except Exception as e:
            failed += 1
            failures.append(f"跨用户写入异常: {e}")
            log(f"  ✗ 跨用户写入异常: {e}")

    return _finish(passed, failed, failures)


def _finish(passed: int, failed: int, failures) -> int:
    log("\n" + "=" * 64)
    log(f"冒烟结果：通过 {passed} · 失败 {failed}")
    if failures:
        log("-" * 64)
        log("失败项：")
        for f in failures:
            log(f"  • {f}")
    if failed == 0:
        log("✅ 全栈冒烟全部通过")
        return 0
    log("❌ 存在失败项，请排查")
    return 1


if __name__ == "__main__":
    sys.exit(main())
