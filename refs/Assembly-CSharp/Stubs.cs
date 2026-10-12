// Shape-only stub of ROUNDS' Assembly-CSharp. Contains no game code: only the declarations this
// mod compiles against, every body `throw null`. Method signatures are checked against the
// MMHOOK.Rounds hook delegates by tools/refcheck; fields against the references other mods
// compiled against the real game. See refs/README.md before adding anything.
using System.Collections;
using System.Collections.Generic;
using InControl;
using Photon.Pun;
using UnityEngine;

public enum PickerType { Team, Player }

public class CardCategory : ScriptableObject { }
public class CardThemeColor { public enum CardThemeColorType { DestructiveRed, FirepowerYellow, DefensiveBlue, TechWhite, EvilPurple, PoisonGreen, NatureBrown, ColdBlue, MagicPink } }

public class CardChoice : MonoBehaviour
{
    public static CardChoice instance;
    public CardInfo[] cards;
    public int pickrID;
    public bool IsPicking;
    public void Pick(GameObject pickedCard = null, bool clear = false) => throw null;
    public GameObject AddCardVisual(CardInfo cardToSpawn, Vector3 pos) => throw null;
    public IEnumerator DoPick(int picksToSet, int picketIDToSet, PickerType pType = PickerType.Team) => throw null;
}

public class CardChoiceVisuals : MonoBehaviour
{
    public static CardChoiceVisuals instance;
    public void Show(int pickerID, bool animateIn = false) => throw null;
    public void Hide() => throw null;
}

public class CardInfo : MonoBehaviour
{
    public enum Rarity { Common, Uncommon, Rare }
    public string cardName;
    public string cardDestription;
    public Rarity rarity;
    public CardInfo sourceCard;
    public GameObject cardBase;
    public GameObject cardArt;
    public bool allowMultiple;
    public CardCategory[] categories;
    public CardCategory[] blacklistedCategories;
    public CardThemeColor.CardThemeColorType colorTheme;
}

public class ApplyCardStats : MonoBehaviour { }

public class CharacterStatModifiers : MonoBehaviour
{
    public List<GameObject> objectsAddedToPlayer;
}

public class PlayerActions { }

public class CharacterData : MonoBehaviour
{
    public PhotonView view;
    public Player player;
    public PlayerActions playerActions;
    public CharacterStatModifiers stats;
    public List<CardInfo> currentCards;
    public bool dead;
    public bool isPlaying;
}

public class Player : MonoBehaviour
{
    public int playerID;
    public int teamID;
    public CharacterData data;
}

public class PlayerSkin : MonoBehaviour
{
    public Color color;
    public Color backgroundColor;
    public Color winText;
    public Color particleEffect;
}

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager instance;
    public List<Player> players;
    public PlayerSkin GetColorFromPlayer(int playerID) => throw null;
    public PlayerSkin GetColorFromTeam(int teamID) => throw null;
    public Player GetPlayerWithID(int playerID) => throw null;
}

public class UIHandler : MonoBehaviour
{
    public static UIHandler instance;
    public void DisplayScreenText(Color color, string text, float speed) => throw null;
    public void DisplayScreenTextLoop(string text) => throw null;
    public void DisplayScreenTextLoop(Color color, string text) => throw null;
    public void StopScreenTextLoop() => throw null;
    public void ShowPicker(int pickerID, PickerType pickerType) => throw null;
    public void StopShowPicker() => throw null;
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public bool battleOngoing;
    public bool isPlaying;
}
