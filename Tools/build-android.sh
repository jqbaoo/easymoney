#!/usr/bin/env bash
# 构建 Android APK。用法: bash Tools/build-android.sh
#
# 退出码: 0 = 构建成功; 1 = 构建失败; 2 = 环境问题（找不到 Unity / 编辑器正开着）
#
# 首次 IL2CPP 构建要 10-20 分钟，属正常。
set -uo pipefail

PROJECT_PATH="E:/Projects/easymoney"
UNITY_EXE="${UNITY_EXE:-D:/unity/unity2022/2022.3.53f1c1/Editor/Unity.exe}"
OUTPUT_APK="$PROJECT_PATH/Builds/EasyMoney.apk"

# 日志放 Tools/ 而不是 Temp/：Unity 退出时会清理 Temp/，
# 构建失败后想 tail 日志时它已经没了（run-editmode-tests.sh 踩过同一个坑）。
LOGFILE="$PROJECT_PATH/Tools/build-android.log"

if [ ! -f "$UNITY_EXE" ]; then
  echo "找不到 Unity 编辑器: $UNITY_EXE"
  exit 2
fi

# 同一个项目不能被两个 Unity 实例同时打开，否则构建起不来。
#
# 判据以**项目下的 Temp/UnityLockfile** 为准：编辑器开着就一定持有它，正常关闭会删掉。
# 光看「机器上有没有 Unity.exe」会误伤两种情形——别的项目开着的编辑器，以及跑完没退
# 干净的残留进程。后者实测把构建白挡了一轮：那个进程 0 线程 0 句柄、没有 lockfile，
# 而 taskkill 对它只报「拒绝访问」，普通权限清不掉，只能重启。
# 两个条件同时成立才算占用。
if [ -f "$PROJECT_PATH/Temp/UnityLockfile" ] &&
   tasklist //FI "IMAGENAME eq Unity.exe" 2>/dev/null | grep -qi "Unity.exe"; then
  echo "Unity 编辑器正在运行（项目下有 Temp/UnityLockfile），请先关闭再构建"
  exit 2
fi

mkdir -p "$PROJECT_PATH/Builds"

# 先删掉旧产物。留着的话，这次构建失败也会因为「文件在」被误判成成功——
# 那正是最该避免的静默失败。
rm -f "$OUTPUT_APK"

"$UNITY_EXE" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -buildTarget Android \
  -executeMethod EasyMoney.App.EditorTools.BuildScript.BuildAndroid \
  -logFile "$LOGFILE"
EXIT_CODE=$?

if [ -f "$OUTPUT_APK" ]; then
  echo "构建成功: $OUTPUT_APK"
  ls -lh "$OUTPUT_APK"
  exit 0
fi

echo "构建失败（Unity 退出码 $EXIT_CODE），日志尾部："
tail -60 "$LOGFILE"
exit 1
