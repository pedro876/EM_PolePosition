using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Ranking HUD")]
    [SerializeField] private GameObject rankingHUD;
    [SerializeField] private Text[] playerTexts;
    [SerializeField] private Button returnButton;
    int textIndex = 0;

    #endregion variables

    private void Awake()
    {
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
    private void StartGame()
    {
        if (!inputFieldName.text.Equals(""))
            playerName = inputFieldName.text.ToUpper();
        ActivateMainMenu();
    }

    #endregion chooseNameHUDfuncs

    #region gameHUDfuncs

    public void UpdateSpeed(int speed)
    {
        textSpeed.text = "Speed " + speed + " Km/h";
    }

    public void UpdateLap(PlayerInfo player)
    {
        if (player.isLocalPlayer)
            textLaps.text = "LAP: "+ player.CurrentLap + "/" + PolePositionManager.maxLaps;
    }

    public void UpdatePosition(String text)
    {
        textPosition.text = text;
    }

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

    IEnumerator RemoveCountdown()
    {
        yield return new WaitForSeconds(2);
        countdownText.gameObject.SetActive(false);
    }

    #endregion gameHUDfuncs

    #region rankingHUDfuncs

    void AddPlayerToRanking(string newName)
    {
        if(textIndex < 4)
        {
            playerTexts[textIndex].gameObject.SetActive(true);
            playerTexts[textIndex].text = newName;
            textIndex++;
        }
    }

    #endregion rankingHUDfuncs

    #region changeState

    private void ActivateChooseNameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        chooseNameHUD.SetActive(true);
        rankingHUD.SetActive(false);
    }
    
    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        chooseNameHUD.SetActive(false);
        rankingHUD.SetActive(false);
    }

    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        chooseNameHUD.SetActive(false);
        inGameHUD.SetActive(true);
        rankingHUD.SetActive(false);
    }

    private void ActivateRankingHUD()
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

    private void StartClient()
    {
        if(inputFieldIP.text != "")
            m_NetworkManager.networkAddress = inputFieldIP.text;
        m_NetworkManager.StartClient();
        m_NetworkManager.networkAddress = inputFieldIP.text;
        ActivateInGameHUD();
    }

    private void StartServer()
    {
        m_NetworkManager.StartServer();
        ActivateInGameHUD();
    }

    #endregion hostClientServerFuncs
}