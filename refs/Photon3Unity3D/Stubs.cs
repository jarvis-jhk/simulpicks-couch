// Shape-only stub of the Photon3Unity3D assembly ROUNDS ships. Only what this mod compiles
// against; signatures checked against the member references of UnboundLib/ModdingUtils.
namespace ExitGames.Client.Photon
{
    public class Hashtable : System.Collections.Generic.Dictionary<object, object>
    {
        public new object this[object key] { get => throw null; set => throw null; }
    }
    public class EventData
    {
        public byte Code;
        public object CustomData => throw null;
    }
    public struct SendOptions { public bool Reliability { get => throw null; set => throw null; } }
}
