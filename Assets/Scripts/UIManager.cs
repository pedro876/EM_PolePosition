using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class UIPlayer
{
    public Text textSlot;
    public Text readySlot;
    public bool ready;

    public void SetReady(bool _ready, Color textColor)
    {
        ready = _ready;
        if (readySlot)
        {
            readySlot.gameObject.SetActive(ready);
            readySlot.color = textColor;
        }
    }
}


/*
 * Se encarga de dar interactividad a la interfaz
 */

public class UIManager :  MonoBehaviour
{
    public Text debugText;

    #region variables
    public bool showGUI = true;

    private NetworkManager m_NetworkManager;
    private PolePositionManager polePosition;

    [Header("Choose Name")] [SerializeField] private GameObject chooseNameHUD;
    [SerializeField] private InputField inputFieldName;
    [SerializeField] private Button buttonStart;
    public string playerName = "PLAYER";

    [Header("Main Menu")] [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    //[SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;
    [SerializeField] GameObject errorField;
    [SerializeField] Text errorFieldText;
    [SerializeField] float temporalMessageTime = 4.0f;

    [Header("Waiting Room")]
    [SerializeField] private GameObject roomHUD;
    [SerializeField] private GameObject[] hostOptions;
    [SerializeField] public UIPlayer[] uiPlayers;
    [SerializeField] private Button[] colorButtons;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button notReadyButton;
    [SerializeField] private Button startButton;
    

    [Header("In-Game HUD")]
    [SerializeField] private GameObject inGameHUD;
    [SerializeField] private Text textSpeed;
    [SerializeField] private Text textLaps;
    [SerializeField] private Text textPosition;
    [SerializeField] private Text countdownText;
    [SerializeField] public Text backwardsText;
    [SerializeField] public Text bestLapText;

    [Header("Ranking HUD")]
    [SerializeField] private GameObject rankingHUD;
    [SerializeField] private Text[] playerTexts;
    [SerializeField] private Text[] timeTexts;
    [SerializeField] private Button returnButton;
    private int textIndex = 0;

    #endregion variables

    #region AwakeStart

    private void Awake()
    {
        polePosition = FindObjectOfType<PolePositionManager>();
        foreach (Text t in playerTexts) t.gameObject.SetActive(false);
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        foreach (GameObject option in hostOptions) option.SetActive(false);
        foreach(UIPlayer uiP in uiPlayers)
        {
            uiP.SetReady(false, Color.white);
            uiP.textSlot.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        buttonStart.onClick.AddListener(() => StartMainMenu());

        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        //buttonServer.onClick.AddListener(() => StartServer());
        ActivateChooseNameHUD();
    }

    #endregion

    #region getStateFuncs

    public bool inGame()
    {
        return inGameHUD.gameObject.activeSelf;
    }

    #endregion

    #region chooseNameHUDfuncs

    /*
     * Al iniciar el juego, guarda el nombre introducido en la interfaz si no está vacío
     */
    private void StartMainMenu()
    {
        if (!inputFieldName.text.Equals(""))
            playerName = inputFieldName.text.ToUpper();
        ActivateMainMenu();
    }

    #endregion chooseNameHUDfuncs

    #region mainMenuFuncs_TemporalMessage

    bool displayingMessage = false;
    void SetTemporalMessage(string message = "")
    {
        StopCoroutine("TemporalMessageCoroutine");
        Debug.Log(message);
        if (message != "" && ! displayingMessage)
        {
            displayingMessage = true;
            errorField.SetActive(true);
            errorFieldText.text = message;
            StartCoroutine("TemporalMessageCoroutine");
        }
    }

    IEnumerator TemporalMessageCoroutine()
    {
        yield return new WaitForSeconds(temporalMessageTime);
        errorField.SetActive(false);
        errorFieldText.text = "";
        displayingMessage = false;
        buttonHost.gameObject.SetActive(true);
        buttonClient.gameObject.SetActive(true);
        inputFieldIP.gameObject.SetActive(true);
    }

    #endregion

    #region gameHUDfuncs

    #region positionAndSpeed

    public void UpdateSpeed(PlayerInfo player)
    {
        if(player.isLocalPlayer)
            textSpeed.text = "Speed " + (int)(player.Speed*5f) + " Km/h";
    }

    public void UpdatePosition(String text) { textPosition.text = text; }

    #endregion

    #region laps

    public void UpdateLap(PlayerInfo player)
    {
        if (player.isLocalPlayer)
            textLaps.text = "LAP: "+ player.CurrentLap + "/" + polePosition.maxLaps;
    }

    public void UpdateBestLap(PlayerInfo player)
    {
        if (player.isLocalPlayer)
            bestLapText.text = "Best lap: " + player.bestLap;
    }

    #endregion

    #region countdown

    /*
     * Se encarga de actualizar la información del countdown en pantalla, en caso de no estar activo el countdown indicará el número de jugadores actual respecto al total
     */
    public void UpdateCountdownText(int secondsLeft)
    {
        if (countdownText != null && countdownText.gameObject.activeSelf)
        {
            if (secondsLeft == 0)
            {
                countdownText.text = "GO!";
                StartCoroutine("RemoveCountdown");
            }
            else
                countdownText.text = secondsLeft.ToString();
        }
    }

    /*
     * Esta corrutina se encarga de quitar el mensaje GO! de la interfaz a los dos segundos de aparecer
     */
    IEnumerator RemoveCountdown()
    {
        yield return new WaitForSeconds(2);
        countdownText.gameObject.SetActive(false);
    }
    #endregion

    #endregion gameHUDfuncs

    #region colorButtons

    /*
     * Añade funcionalidad a los botones de la interfaz
     */
    public void SetColorButtonsFunctions(PlayerInfo localPlayer)
    {
        foreach (Button colorButton in colorButtons)
        {
            colorButton.onClick.RemoveAllListeners();
            colorButton.onClick.AddListener(() => localPlayer.CmdChangeColor(colorButton.colors.normalColor));
        }
    }

    #endregion

    #region readyNotReadyStartButtons

    /*
     * Añade funcionalidad a los botones de ready, not ready y start
     */
    public void SetReadyButtonsFunctions(PlayerInfo localPlayer)
    {
        readyButton.onClick.RemoveAllListeners();
        notReadyButton.onClick.RemoveAllListeners();
        startButton.onClick.RemoveAllListeners();

        if (!polePosition) polePosition = FindObjectOfType<PolePositionManager>();
        if (localPlayer.isServer)
        {
            readyButton.gameObject.SetActive(false);
            notReadyButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(() => polePosition.StartGame());
            
        } else
        {
            readyButton.gameObject.SetActive(!localPlayer.ready);
            notReadyButton.gameObject.SetActive(localPlayer.ready);
            startButton.gameObject.SetActive(false);
            readyButton.onClick.AddListener(() => localPlayer.CmdSetReady(true));
            notReadyButton.onClick.AddListener(() => localPlayer.CmdSetReady(false));
        }
    }

    #endregion

    #region playerList

    /*
     * Añade un slot en la lista de jugadores de la sala de espera para mostrar su información
     */
    public void AddPlayerToRoomUI(PlayerInfo player, List<PlayerInfo> players)
    {
        player.uiPlayerIndex = players.Count-1;
    }

    /*
     * Reasigna el índice de slot para mostrar información para cada jugador
     */
    public void ReAssignUIPlayers(List<PlayerInfo> players, int maxPlayers)
    {
        for (int i = 0; i < players.Count; i++)
            players[i].uiPlayerIndex = i;
        RemoveLostPlayersFromUI(players, maxPlayers);
    }

    /*
     * Desactiva los slots de información que sobran, ya que es posible que un jugador salga de la partida
     */
    public void RemoveLostPlayersFromUI(List<PlayerInfo> players, int maxPlayers)
    {
        for (int j = players.Count; j < maxPlayers; j++)
        {
            uiPlayers[j].SetReady(false, Color.black);
            if(uiPlayers[j].textSlot) uiPlayers[j].textSlot.gameObject.SetActive(false);
        }
    }
    #endregion

    #region rankingHUDfuncs

    public void AddPlayerToRanking(string newName, string bestTime)
    {
        if(textIndex < 4)
        {
            playerTexts[textIndex].gameObject.SetActive(true);
            playerTexts[textIndex].text = newName;
            timeTexts[textIndex].gameObject.SetActive(true);
            timeTexts[textIndex].text = bestTime == "00:00:00" || bestTime == "" ? "--:--:--" : bestTime;
            textIndex++;
        }
    }

    #endregion rankingHUDfuncs

    #region changeState

    public void ActivateChooseNameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        chooseNameHUD.SetActive(true);
        rankingHUD.SetActive(false);
        roomHUD.SetActive(false);
        if(polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
    }
    
    public void ActivateMainMenu(string temporalMessage = "")
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        chooseNameHUD.SetActive(false);
        rankingHUD.SetActive(false);
        roomHUD.SetActive(false);
        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
        SetTemporalMessage(temporalMessage);
    }

    public void ActivateRoomHUD(bool isServer)
    {
        errorField.SetActive(false);

        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        chooseNameHUD.SetActive(false);
        rankingHUD.SetActive(false);
        roomHUD.SetActive(true);
        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StartCoroutine("UpdateRoomUICoroutine");
        
        if (isServer)
        {
            foreach (GameObject option in hostOptions) option.SetActive(true);
        }
    }

    public void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        chooseNameHUD.SetActive(false);
        inGameHUD.SetActive(true);
        rankingHUD.SetActive(false);
        roomHUD.SetActive(false);
        if (polePosition && polePosition.isActiveAndEnabled)
        {
            polePosition.StopCoroutine("UpdateRoomUICoroutine");
        }
    }

    public void ActivateRankingHUD()
    {
        mainMenu.SetActive(false);
        chooseNameHUD.SetActive(false);
        inGameHUD.SetActive(false);
        rankingHUD.SetActive(true);
        roomHUD.SetActive(false);
        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
    }

    #endregion changeState

    #region hostClientServerFuncs

    private void StartHost()
    {
        m_NetworkManager.StartHost();
    }

    /*
     * Tratará de conectar a localhost por defecto, si el inputField de la IP no está vacío lo utilizará en lugar de localhost
     */
    private void StartClient()
    {
        if(inputFieldIP.text != "")
            m_NetworkManager.networkAddress = inputFieldIP.text;
        m_NetworkManager.StartClient();

        errorField.SetActive(true);
        errorFieldText.text = "Trying to connect...";
    }

    /*private void StartServer()
    {
        m_NetworkManager.StartServer();
        
        //ActivateInGameHUD();
    }*/

    #endregion hostClientServerFuncs
}