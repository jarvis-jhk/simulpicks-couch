// Shape-only stub of InControl. Only what this mod compiles against.
namespace InControl
{
    public class OneAxisInputControl { public bool WasPressed => throw null; }
    public class PlayerAction : OneAxisInputControl { }
    public class TwoAxisInputControl { public float X => throw null; }
    public class PlayerTwoAxisAction : TwoAxisInputControl { }
}
