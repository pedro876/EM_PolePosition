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
    public Text debugText;

    #region variables
    public bool showGUI = true;

    [Header("References")]
    [SerializeField]private CustomNetworkManager m_NetworkManager;
    [SerializeField]private PolePositionManager polePosition;

    [Header("Choose Name")]
    [SerializeField] public ChooseNameHUD chooseNameHUD;

    [Header("Main Menu")]
    [SerializeField] public MainMenuHUD mainMenuHUD;

    [Header("Waiting Room")]
    [SerializeField] public RoomHUD roomHUD;


    [Header("In-Game HUD")]
    [SerializeField] public GameHUD gameHUD;

    [Header("Ranking HUD")]
    [SerializeField] public RankingHUD rankingHUD;
    /*[SerializeField] private GameObject rankingHUD;
    [SerializeField] private Text[] playerTexts;
    [SerializeField] private Text[] timeTexts;
    [SerializeField] private Button returnButton;
    private int textIndex = 0;
    */

    #endregion

    #region AwakeStart

    private void Awake()
    {
        GetRefs();
        ActivateChooseNameHUD();
    }

    void GetRefs()
    {
        if(!polePosition) FindObjectOfType<PolePositionManager>();
        if (!m_NetworkManager) m_NetworkManager = FindObjectOfType<CustomNetworkManager>();
    }

    #endregion

    #region changeState

    public void ActivateChooseNameHUD()
    {
        chooseNameHUD.gameObject.SetActive(true);
        mainMenuHUD.gameObject.SetActive(false);
        roomHUD.gameObject.SetActive(false);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(false);
        if(polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
    }

    /*
     * Al iniciar el juego, guarda el nombre introducido en la interfaz si no está vacío
     */
    public void ActivateMainMenu()
    {
        mainMenuHUD.HideNetworkButtons(false);

        chooseNameHUD.gameObject.SetActive(false);
        mainMenuHUD.gameObject.SetActive(true);
        roomHUD.gameObject.SetActive(false);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(false);
        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
    }

    public void ActivateRoomHUD()
    {
        chooseNameHUD.gameObject.SetActive(false);
        mainMenuHUD.gameObject.SetActive(false);
        roomHUD.gameObject.SetActive(true);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(false);

        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StartCoroutine("UpdateRoomUICoroutine");
    }

    public void ActivateInGameHUD()
    {
        chooseNameHUD.gameObject.SetActive(false);
        mainMenuHUD.gameObject.SetActive(false);
        roomHUD.gameObject.SetActive(false);
        gameHUD.gameObject.SetActive(true);
        rankingHUD.gameObject.SetActive(false);
        if (polePosition && polePosition.isActiveAndEnabled)
        {
            polePosition.StopCoroutine("UpdateRoomUICoroutine");
        }
    }

    public void ActivateRankingHUD()
    {
        chooseNameHUD.gameObject.SetActive(false);
        mainMenuHUD.gameObject.SetActive(false);
        roomHUD.gameObject.SetActive(false);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(true);
        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
    }

    #endregion changeState

    #region hostClientServerFuncs

    /*
     * Tratará de conectar a localhost por defecto, si el inputField de la IP no está vacío lo utilizará en lugar de localhost
     */
    

    

    #endregion hostClientServerFuncs
}