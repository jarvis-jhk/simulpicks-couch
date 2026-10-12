// Shape-only stub of Unity.TextMeshPro. Only what this mod compiles against.
namespace TMPro
{
    // Values unused here: UnboundLib's default arguments carry their own constants.
    public enum TextAlignmentOptions { TopLeft = 257, Top = 258, Left = 513, Center = 514, Midline = 4610 }
    public class TMP_FontAsset : UnityEngine.ScriptableObject { }
    public abstract class TMP_Text : UnityEngine.UI.MaskableGraphic { public virtual string text { get => throw null; set => throw null; } }
    public class TextMeshProUGUI : TMP_Text { }
    public class TextMeshPro : TMP_Text { }
}
