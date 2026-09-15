#!/usr/bin/env bash
# 命令行跑 EditMode 测试。用法: bash Tools/run-editmode-tests.sh
# 退出码: 0 = 全部通过; 1 = 有测试失败; 2 = 环境/编译错误
#
# 环境变量:
#   UNITY_EXE          Unity 编辑器路径
#   TIMEOUT_SECONDS    等结果文件的上限，默认 1200（实测一轮约 4-5 分钟）
set -uo pipefail

PROJECT_PATH="E:/Projects/easymoney"
UNITY_EXE="${UNITY_EXE:-D:/unity/unity2022/2022.3.53f1c1/Editor/Unity.exe}"
# 产物必须放在 Temp/ 之外：Unity 退出时会清理 Temp 目录，
# 实测结果文件写完（2.7KB）后会在进程结束时被删掉，导致脚本误判为「编译错误」。
# 日志文件因被 Unity 自身持有而幸免，但不能指望这个巧合。
RESULTS="$PROJECT_PATH/Tools/editmode-results.xml"
LOGFILE="$PROJECT_PATH/Tools/editmode.log"
TIMEOUT_SECONDS="${TIMEOUT_SECONDS:-1200}"

mkdir -p "$PROJECT_PATH/Tools"

if [ ! -f "$UNITY_EXE" ]; then
  echo "找不到 Unity 编辑器: $UNITY_EXE"
  echo "请设置环境变量 UNITY_EXE 指向 Unity.exe"
  exit 2
fi

# 只收掉「我们自己起的、正在跑测试的」那个 Unity。
# 按命令行里的 -runTests 精确匹配——用户自己开着的编辑器不带这个参数，
# 不会被误伤（实测两者进程名完全一样，按名字杀会丢未保存的改动）。
_stop_test_unity()
{
  powershell -NoProfile -Command "
    Get-CimInstance Win32_Process -Filter \"Name='Unity.exe'\" |
    Where-Object { \$_.CommandLine -like '*-runTests*' } |
    ForEach-Object { Stop-Process -Id \$_.ProcessId -Force -ErrorAction SilentlyContinue }
  " >/dev/null 2>&1
}

rm -f "$RESULTS"

# ── 为什么后台起、然后轮询结果文件 ──────────────────────────
# Unity 跑完测试后**经常不自己退出**（实测多次，进程驻留 1.6 GB），
# 前台等待就会一直挂在这里，白等几分钟。
# 但结果文件是 NUnit 在测试**全部结束时**才写的，所以「XML 写完了」就等于
# 「测试跑完了」——据此主动收掉进程，不必等它自己走。
# ⚠️ 正因如此，**不能用 Unity 的退出码判定成败**：我们自己 kill 出来的
# 退出码必然非 0，拿它当「编译错误」会把每一轮都误判成失败。成败只看 XML。
"$UNITY_EXE" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -runTests \
  -testPlatform EditMode \
  -testResults "$RESULTS" \
  -logFile "$LOGFILE" &
UNITY_PID=$!

# 「写完了」的判据是**根标签已闭合**，不是文件大小稳定：NUnit 的 XML 以
# </test-run> 收尾，见到它才是最终态。早先按「大小连续两次一致」判，中间态
# 也可能稳定住，会读到半截结果却当成跑完了。
_has_final_result()
{
  [ -f "$RESULTS" ] && tail -c 200 "$RESULTS" | grep -q '</test-run>'
}

ELAPSED=0
while ! _has_final_result; do
  # 进程自己先走了却没留下完整结果，多半是编译错误，不必空等到超时
  if ! kill -0 "$UNITY_PID" 2>/dev/null; then
    sleep 3
    break
  fi

  if [ "$ELAPSED" -ge "$TIMEOUT_SECONDS" ]; then
    echo "等了 ${TIMEOUT_SECONDS}s 仍未产生完整结果，收掉进程。日志尾部："
    _stop_test_unity
    tail -60 "$LOGFILE"
    exit 2
  fi

  sleep 3
  ELAPSED=$((ELAPSED + 3))
done

_stop_test_unity
wait "$UNITY_PID" 2>/dev/null

if [ ! -f "$RESULTS" ]; then
  echo "=== 测试未产生结果文件，多半是编译错误。日志尾部： ==="
  tail -60 "$LOGFILE"
  exit 2
fi

# 一道防「静默失败」的闸：加了 -quit 会让 Unity 在跑测试前就退出，
# 退出码 0 但一个用例都没跑。结果文件在、用例数为 0 是同一种假通过。
# ⚠️ 先取出整个 <test-run ...> 标签，再从中抽 total="N"。
# 直接对标签抽数字是错的：标签开头是 id="2"，head -1 会取到那个 2 而不是用例数。
TOTAL=$(grep -oE '<test-run[^>]*>' "$RESULTS" | grep -oE ' total="[0-9]+"' | head -1 | grep -oE '[0-9]+')
if [ -z "$TOTAL" ] || [ "$TOTAL" -eq 0 ]; then
  echo "=== 结果文件里一个用例都没有，测试没真正跑起来。日志尾部： ==="
  tail -40 "$LOGFILE"
  exit 2
fi

if grep -q 'result="Failed"' "$RESULTS"; then
  echo "=== 有测试失败（共 $TOTAL 个用例），失败详情： ==="
  grep -B2 -A8 '<failure>' "$RESULTS" | head -100
  exit 1
fi

echo "EditMode 测试全部通过（$TOTAL 个用例）"
exit 0
