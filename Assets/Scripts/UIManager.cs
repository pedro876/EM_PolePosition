using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
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
        //ActivateMainMenu();
        ActivateChooseNameHUD();
    }

    private void StartGame()
    {
        if (!inputFieldName.text.Equals(""))
            playerName = inputFieldName.text.ToUpper();
        ActivateMainMenu();
    }

    public void UpdateSpeed(int speed)
    {
        textSpeed.text = "Speed " + speed + " Km/h";
    }

    public void UpdateCountdownText(int numPlayers, int maxPlayers, bool countdownActive, int secondsLeft)
    {
        if (countdownText.enabled)
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

    private void ActivateChooseNameHUD()
    {
        mainMenu.SetActive(false);
        inGameHUD.SetActive(false);
        chooseNameHUD.SetActive(true);
    }
    
    private void ActivateMainMenu()
    {
        mainMenu.SetActive(true);
        inGameHUD.SetActive(false);
        chooseNameHUD.SetActive(false);
    }

    private void ActivateInGameHUD()
    {
        mainMenu.SetActive(false);
        chooseNameHUD.SetActive(false);
        inGameHUD.SetActive(true);
    }

    private void StartHost()
    {
        m_NetworkManager.StartHost();
        ActivateInGameHUD();
    }

    private void StartClient()
    {
        m_NetworkManager.StartClient();
        m_NetworkManager.networkAddress = inputFieldIP.text;
        ActivateInGameHUD();
    }

    private void StartServer()
    {
        m_NetworkManager.StartServer();
        ActivateInGameHUD();
    }
}