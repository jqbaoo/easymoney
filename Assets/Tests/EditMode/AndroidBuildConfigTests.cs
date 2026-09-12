using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace EasyMoney.Tests
{
    /// <summary>
    /// 构建配置守卫。
    ///
    /// 存在的理由：Android 端的 SQLite 原生库是随 NuGet 包分发的，而 NuGetForUnity 只会
    /// 解压它在 NativeRuntimeSettings.json 里登记过的 runtime。一旦构建目标架构和实际解压
    /// 出来的原生库对不上，编辑器里测试全绿、真机上才 DllNotFoundException——反馈延迟太长。
    /// 所以在编辑器里先一步把关。
    ///
    /// 断言用强类型枚举而非数字，避免踩位掩码取值写错的坑。
    /// </summary>
    public class AndroidBuildConfigTests
    {
        [Test]
        public void EveryAndroidTargetArchitecture_HasNativeLibrary()
        {
            AndroidArchitecture eArch = PlayerSettings.Android.targetArchitectures;
            List<string> lMissing = new List<string>();

            // Unity 2022 的 Android 只提供 ARMv7 / ARM64 两个选项，所以只覆盖这两个。
            // （x86 / x86_64 的支持早已被 Unity 移除，枚举里未必还有对应成员。）
            _checkArch(eArch, AndroidArchitecture.ARMv7, "android-arm", lMissing);
            _checkArch(eArch, AndroidArchitecture.ARM64, "android-arm64", lMissing);

            Assert.IsEmpty(lMissing,
                "以下构建目标架构没有对应的 SQLite 原生库，装到真机必然 DllNotFoundException：\n  " +
                string.Join("\n  ", lMissing.ToArray()));
        }

        private static void _checkArch(AndroidArchitecture eTarget, AndroidArchitecture eBit,
                                       string sRid, List<string> lMissing)
        {
            if ((eTarget & eBit) == 0)
            {
                return;
            }

            if (_findNativeLib(sRid) == null)
            {
                lMissing.Add(eBit + "：缺少 runtimes/" + sRid + "/native/libe_sqlite3.so" +
                             "（多半是 NativeRuntimeSettings.json 里没登记 " + sRid + "）");
            }
        }

        private static string _findNativeLib(string sRid)
        {
            string sPackages = Path.Combine(Application.dataPath, "Packages");
            if (!Directory.Exists(sPackages))
            {
                return null;
            }

            foreach (string sDir in Directory.GetDirectories(sPackages, "SourceGear.sqlite3.*"))
            {
                string sPath = Path.Combine(sDir, "runtimes", sRid, "native", "libe_sqlite3.so");
                if (File.Exists(sPath))
                {
                    return sPath;
                }
            }

            return null;
        }
    }
}
