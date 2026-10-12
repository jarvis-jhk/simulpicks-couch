// Shape-only stub of UnityEngine.UI (uGUI). Only what this mod compiles against.
namespace UnityEngine.EventSystems { public abstract class UIBehaviour : UnityEngine.MonoBehaviour { } }
namespace UnityEngine.UI
{
    public abstract class Selectable : UnityEngine.EventSystems.UIBehaviour { }
    public class Slider : Selectable
    {
        public enum Direction { LeftToRight, RightToLeft, BottomToTop, TopToBottom }
        public float value { get => throw null; set => throw null; }
    }
    public class Toggle : Selectable { public bool isOn { get => throw null; set => throw null; } }
    public abstract class Graphic : UnityEngine.EventSystems.UIBehaviour
    {
        public virtual UnityEngine.Color color { get => throw null; set => throw null; }
        public UnityEngine.RectTransform rectTransform => throw null;
    }
    public abstract class MaskableGraphic : Graphic { }
    public class Image : MaskableGraphic { }
}
