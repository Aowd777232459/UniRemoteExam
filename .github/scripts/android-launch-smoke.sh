#!/usr/bin/env bash
set -euo pipefail

package_name="ye.edu.sanaau.uniremoteexam"
apk_path="artifacts/android/UniRemoteExam-Android.apk"

adb install -r "$apk_path"
adb logcat -c
adb shell monkey -p "$package_name" -c android.intent.category.LAUNCHER 1
sleep 12

app_pid="$(adb shell pidof "$package_name" | tr -d '\r')"
if test -z "$app_pid"; then
  adb logcat -d '*:E' | tail -n 300
  echo "Application process is not running after launch."
  exit 1
fi

if ! adb shell dumpsys activity activities | grep -Eq "(mResumedActivity|topResumedActivity).*${package_name}"; then
  adb shell dumpsys activity activities | grep -E "mResumedActivity|topResumedActivity|${package_name}" || true
  echo "Application is running but is not the foreground activity."
  exit 1
fi

adb logcat -d --pid="$app_pid" > artifacts/android/app-logcat.txt
if grep -q "FATAL EXCEPTION" artifacts/android/app-logcat.txt; then
  tail -n 300 artifacts/android/app-logcat.txt
  exit 1
fi

adb exec-out screencap -p > artifacts/android/launch-smoke.png
echo "Application stayed open successfully with PID $app_pid."
