using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace EasyMoney.Tests
{
    /// <summary>
    /// Player Settings 守卫。
    ///
    /// 存在的理由：这一批配置全都只在真机上才暴露后果——剥离级别设错要等装到手机上
    /// 记账时才闪退、MinSdk 设错要等安装失败、包名设错要等签名对不上。而 Unity 面板
    /// 里的值没有任何东西盯着，改一次别的设置就可能被顺手带跑。
    ///
    /// 所以把「Step 2 那张表」翻译成断言，配置对不对不靠肉眼核对面板。
    /// 原生库与目标架构那部分守卫在 <see cref="AndroidBuildConfigTests"/> 里。
    /// </summary>
    public class AndroidPlayerSettingsTests
    {
        private const string EXPECTED_PACKAGE_NAME = "com.easymoney.app";

        /// <summary>构建脚本要用的场景。改名或删除会让命令行构建直接失败。</summary>
        private const string SCENE_PATH = "Assets/Scenes/SampleScene.unity";

        [Test]
        public void PackageName_IsExpected()
        {
            string sActual = PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android);
            Assert.AreEqual(EXPECTED_PACKAGE_NAME, sActual,
                "包名变了，已安装的旧版 APK 会变成另一个应用（数据不共享、无法覆盖安装）");
        }

        [Test]
        public void MinSdkVersion_IsAndroid7()
        {
            // 计划定的是 Android 7.0 (API 24)。定低了会让人以为支持老机器，
            // 实际却在那些机器上跑不起来；定高了白白丢掉还能用的设备。
            Assert.AreEqual(AndroidSdkVersions.AndroidApiLevel24, PlayerSettings.Android.minSdkVersion,
                "Minimum API Level 应为 Android 7.0 (API 24)");
        }

        [Test]
        public void TargetSdkVersion_IsAutomatic()
        {
            // 0 == Automatic (highest installed)。跟着装好的 SDK 走，
            // 免得手写一个数字之后被新系统的行为变更卡住。
            Assert.AreEqual(AndroidSdkVersions.AndroidApiLevelAuto, PlayerSettings.Android.targetSdkVersion,
                "Target API Level 应保持 Automatic");
        }

        [Test]
        public void TargetArchitectures_CoverBothArmv7AndArm64()
        {
            // AndroidBuildConfigTests 只管「勾了的架构有没有原生库」这一个方向，
            // 管不了「该勾的有没有勾」——而漏勾恰恰是实际发生过的（项目一度只勾了 ARMv7）。
            // 两个方向合起来才完整：那边防打包出 DllNotFoundException，这边防静默丢设备。
            AndroidArchitecture eArch = PlayerSettings.Android.targetArchitectures;

            Assert.IsTrue((eArch & AndroidArchitecture.ARMv7) != 0,
                "缺少 ARMv7：仍在使用 32 位 Android 设备的用户装不上");
            Assert.IsTrue((eArch & AndroidArchitecture.ARM64) != 0,
                "缺少 ARM64：不满足 Google Play 的 64 位要求");
        }

        [Test]
        public void ScriptingBackend_IsIl2Cpp()
        {
            // Mono 后端在 Android 上早就不能用了（Unity 2022 已移除），
            // 但这里仍然守着：后端一旦被切回 Mono，sqlite-net 的原生库加载方式完全不同。
            Assert.AreEqual(ScriptingImplementation.IL2CPP,
                PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android),
                "Android 的 Scripting Backend 必须是 IL2CPP");
        }

        [Test]
        public void ManagedStrippingLevel_IsMinimal()
        {
            // 这是整个打包环节最阴的一个坑：剥离级别调高后，IL2CPP 会删掉 sqlite-net
            // 靠反射调用的代码。编辑器里、EditMode 测试里全都正常，只有真机上一记账就闪退。
            Assert.AreEqual(ManagedStrippingLevel.Minimal,
                PlayerSettings.GetManagedStrippingLevel(BuildTargetGroup.Android),
                "剥离级别必须是 Minimal——调高会剥离 sqlite-net 的反射代码，表现为真机闪退");
        }

        [Test]
        public void InternetAccess_IsNotRequired()
        {
            // 单机 App，不需要网络权限。要着权限反而会让用户在安装时多一层顾虑。
            Assert.IsFalse(PlayerSettings.Android.forceInternetPermission,
                "不应申请网络权限（Internet Access 应为 Not Required）");
        }

        [Test]
        public void LinkXml_Exists()
        {
            // link.xml 是剥离的第一道防线（与 Minimal 双保险）。
            // 少了它，即使剥离级别是 Minimal，某些 Unity 版本仍会剥掉反射用到的类型。
            string sPath = Path.Combine(Application.dataPath, "Plugins/SQLite/link.xml");
            Assert.IsTrue(File.Exists(sPath),
                "缺少 " + sPath + "，IL2CPP 剥离会删掉 sqlite-net 的反射代码");
        }

        [Test]
        public void BuildScene_Exists()
        {
            // BuildScript 里硬编码了场景路径。场景一旦被改名或挪走，
            // 命令行构建会以「找不到场景」失败，而这时人往往已经等了十几分钟。
            string sPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                                        SCENE_PATH);
            Assert.IsTrue(File.Exists(sPath),
                "构建脚本引用的场景不存在：" + SCENE_PATH);
        }
    }
}
