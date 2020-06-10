using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

/*
 * Se encarga de dar interactividad a la interfaz
 */

public class UIManager :  MonoBehaviour
{
    #region variables
    public bool showGUI = true;

    private NetworkManager m_NetworkManager;

    [Header("Choose Name")] [SerializeField] private GameObject chooseNameHUD;
    [SerializeField] private InputField inputFieldName;
    [SerializeField] private Button buttonStart;
    public string playerName = "PLAYER";

    [Header("Main Menu")] [SerializeField] private GameObject mainMenu;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private Button buttonServer;
    [SerializeField] private InputField inputFieldIP;

    [Header("In-Game HUD")] [SerializeField]
    private GameObject inGameHUD;
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

    private void Awake()
    {
        foreach (Text t in playerTexts) t.gameObject.SetActive(false);
        m_NetworkManager = FindObjectOfType<NetworkManager>();
    }

    private void Start()
    {
        buttonStart.onClick.AddListener(() => StartGame());

        buttonHost.onClick.AddListener(() => StartHost());
        buttonClient.onClick.AddListener(() => StartClient());
        buttonServer.onClick.AddListener(() => StartServer());
        ActivateChooseNameHUD();
    }


    #region chooseNameHUDfuncs

    /*
     * Al iniciar el juego, guarda el nombre introducido en la interfaz si no está vacío
     */
    private void StartGame()
    {
        if (!inputFieldName.text.Equals(""))
            playerName = inputFieldName.text.ToUpper();
        ActivateMainMenu();
    }

    #endregion chooseNameHUDfuncs

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
            textLaps.text = "LAP: "+ player.CurrentLap + "/" + PolePositionManager.maxLaps;
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
    public void UpdateCountdownText(int numPlayers, int maxPlayers, bool countdownActive, int secondsLeft)
    {
        if (countdownText != null && countdownText.gameObject.activeSelf)
        {
            if (!countdownActive)
            {
                countdownText.text = numPlayers + "/" + maxPlayers + " PLAYERS";
            }
            else
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
    }
    
    public void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        chooseNameHUD.SetActive(false);
        rankingHUD.SetActive(false);
    }

    public void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        chooseNameHUD.SetActive(false);
        inGameHUD.SetActive(true);
        rankingHUD.SetActive(false);
    }

    public void ActivateRankingHUD()
    {
        mainMenu.SetActive(false);
        chooseNameHUD.SetActive(false);
        inGameHUD.SetActive(false);
        rankingHUD.SetActive(true);
    }

    #endregion changeState

    #region hostClientServerFuncs

    private void StartHost()
    {
        m_NetworkManager.StartHost();
        ActivateInGameHUD();
    }

    /*
     * Tratará de conectar a localhost por defecto, si el inputField de la IP no está vacío lo utilizará en lugar de localhost
     */
    private void StartClient()
    {
        if(inputFieldIP.text != "")
            m_NetworkManager.networkAddress = inputFieldIP.text;
        Debug.Log(m_NetworkManager.networkAddress);
        m_NetworkManager.StartClient();
        ActivateInGameHUD();
    }

    private void StartServer()
    {
        m_NetworkManager.StartServer();
        ActivateInGameHUD();
    }

    #endregion hostClientServerFuncs
}