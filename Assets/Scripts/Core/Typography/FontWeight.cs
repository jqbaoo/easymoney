namespace EasyMoney.Core
{
    /// <summary>
    /// 界面用到的字重档位。
    ///
    /// 为什么不用 Unity 的 FontStyle：它只有 Normal / Bold 两档，没有 Medium。
    /// 而 Medium 恰好是金额和标题最需要的那一档——中文的 Bold 压下来太重，
    /// Normal 又撑不起层次，中间这档缺了就只能拿合成粗体凑合。
    ///
    /// 命名提醒：TMPro 和 UnityEngine.TextCore.LowLevel 里各有一个同名的 FontWeight，
    /// 档位还都叫 Regular / Medium / Bold。项目目前没有任何文件 using TMPro，
    /// 所以不冲突；将来引入 TMP 并 using 了它，同一文件里无限定地写 FontWeight
    /// 会触发 CS0104，届时用完全限定名或给本枚举改名即可。
    /// </summary>
    public enum FontWeight
    {
        Regular,
        Medium,
        Bold
    }
}
