// Shape-only stub of PhotonRealtime (PUN 2). See Photon3Unity3D/Stubs.cs.
using System.Collections.Generic;
using ExitGames.Client.Photon;

namespace Photon.Realtime
{
    public class Player
    {
        public int ActorNumber => throw null;
        public string NickName { get => throw null; set => throw null; }
        public readonly bool IsLocal;
        public bool IsMasterClient => throw null;
        public Hashtable CustomProperties => throw null;
    }
    public class Room
    {
        public string Name => throw null;
        public byte PlayerCount => throw null;
        public Dictionary<int, Player> Players => throw null;
        public Player GetPlayer(int id) => throw null;
    }
    public class FriendInfo { }
    public class LoadBalancingClient { }

    public interface IInRoomCallbacks
    {
        void OnPlayerEnteredRoom(Player newPlayer);
        void OnPlayerLeftRoom(Player otherPlayer);
        void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged);
        void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps);
        void OnMasterClientSwitched(Player newMasterClient);
    }
    public interface IMatchmakingCallbacks
    {
        void OnFriendListUpdate(List<FriendInfo> friendList);
        void OnCreatedRoom();
        void OnCreateRoomFailed(short returnCode, string message);
        void OnJoinedRoom();
        void OnJoinRoomFailed(short returnCode, string message);
        void OnJoinRandomFailed(short returnCode, string message);
        void OnLeftRoom();
    }
}
