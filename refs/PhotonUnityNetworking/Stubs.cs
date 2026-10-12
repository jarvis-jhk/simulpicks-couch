// Shape-only stub of PhotonUnityNetworking (PUN 2). See Photon3Unity3D/Stubs.cs.
using Photon.Realtime;
using UnityEngine;

namespace Photon.Pun
{
    public enum RpcTarget { All, Others, MasterClient, AllBuffered, OthersBuffered, AllViaServer, AllBufferedViaServer }

    public class PhotonView : MonoBehaviour
    {
        public int ViewID { get => throw null; set => throw null; }
        public bool IsMine => throw null;
        public bool AmOwner => throw null;
        public Player Owner => throw null;
        public int OwnerActorNr { get => throw null; set => throw null; }
        public int ControllerActorNr { get => throw null; set => throw null; }
        public object[] InstantiationData { get => throw null; set => throw null; }
        public void RPC(string methodName, RpcTarget target, params object[] parameters) => throw null;
        public void RPC(string methodName, Player targetPlayer, params object[] parameters) => throw null;
        public static PhotonView Find(int viewID) => throw null;
    }

    public struct PhotonMessageInfo
    {
        public readonly PhotonView photonView;
        public PhotonMessageInfo(Player player, int timestamp, PhotonView view) => throw null;
    }

    public interface IPunInstantiateMagicCallback { void OnPhotonInstantiate(PhotonMessageInfo info); }

    public interface IPunPrefabPool
    {
        GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation);
        void Destroy(GameObject gameObject);
    }

    public static class PhotonNetwork
    {
        public static LoadBalancingClient NetworkingClient;
        public static bool OfflineMode { get => throw null; set => throw null; }
        public static bool InRoom => throw null;
        public static bool IsConnected => throw null;
        public static bool IsMasterClient => throw null;
        public static string NickName { get => throw null; set => throw null; }
        public static Player LocalPlayer => throw null;
        public static Player[] PlayerList => throw null;
        public static Room CurrentRoom => throw null;
        public static int ServerTimestamp => throw null;
        public static IPunPrefabPool PrefabPool { get => throw null; set => throw null; }
        public static bool AllocateViewID(PhotonView view) => throw null;
        public static bool LocalCleanPhotonView(PhotonView view) => throw null;
        public static void AddCallbackTarget(object target) => throw null;
        public static void RemoveCallbackTarget(object target) => throw null;
        public static GameObject Instantiate(string prefabName, Vector3 position, Quaternion rotation, byte group = 0, object[] data = null) => throw null;
        public static void Destroy(GameObject targetGo) => throw null;
        public static int GetPing() => throw null;
    }
}
