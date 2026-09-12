#!/usr/bin/env bash
# 命令行跑 EditMode 测试。用法: bash Tools/run-editmode-tests.sh
# 退出码: 0 = 全部通过; 1 = 有测试失败; 2 = 环境/编译错误
set -uo pipefail

PROJECT_PATH="E:/Projects/easymoney"
UNITY_EXE="${UNITY_EXE:-D:/unity/unity2022/2022.3.53f1c1/Editor/Unity.exe}"
# 产物必须放在 Temp/ 之外：Unity 退出时会清理 Temp 目录，
# 实测结果文件写完（2.7KB）后会在进程结束时被删掉，导致脚本误判为「编译错误」。
# 日志文件因被 Unity 自身持有而幸免，但不能指望这个巧合。
RESULTS="$PROJECT_PATH/Tools/editmode-results.xml"
LOGFILE="$PROJECT_PATH/Tools/editmode.log"

mkdir -p "$PROJECT_PATH/Tools"

if [ ! -f "$UNITY_EXE" ]; then
  echo "找不到 Unity 编辑器: $UNITY_EXE"
  echo "请设置环境变量 UNITY_EXE 指向 Unity.exe"
  exit 2
fi

rm -f "$RESULTS"

"$UNITY_EXE" \
  -batchmode \
  -nographics \
  -projectPath "$PROJECT_PATH" \
  -runTests \
  -testPlatform EditMode \
  -testResults "$RESULTS" \
  -logFile "$LOGFILE"
UNITY_EXIT=$?

if [ ! -f "$RESULTS" ]; then
  echo "=== 测试未产生结果文件，多半是编译错误。日志尾部： ==="
  tail -60 "$LOGFILE"
  exit 2
fi

grep -oE '<test-run[^>]*result="[^"]*"[^>]*passed="[0-9]+"[^>]*failed="[0-9]+"' "$RESULTS" | head -1
grep -oE 'failed="[0-9]+"' "$RESULTS" | head -1

if grep -q 'result="Failed"' "$RESULTS"; then
  echo "=== 有测试失败，失败详情： ==="
  grep -B2 -A8 '<failure>' "$RESULTS" | head -100
  exit 1
fi

if [ "$UNITY_EXIT" -ne 0 ]; then
  echo "Unity 退出码 $UNITY_EXIT，日志尾部："
  tail -40 "$LOGFILE"
  exit 2
fi

echo "EditMode 测试全部通过"
exit 0
