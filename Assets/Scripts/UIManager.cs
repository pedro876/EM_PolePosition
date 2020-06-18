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

    [Header("Controls")]
    [SerializeField] public GameObject controlsHUD;

    [Header("Main Menu")]
    [SerializeField] public MainMenuHUD mainMenuHUD;

    [Header("Waiting Room")]
    [SerializeField] public RoomHUD roomHUD;


    [Header("In-Game HUD")]
    [SerializeField] public GameHUD gameHUD;

    [Header("Ranking HUD")]
    [SerializeField] public RankingHUD rankingHUD;

    [Header("Light options")]
    [SerializeField] public bool hasDayLight = true;
    [SerializeField] GameObject dayLightContainer;
    [SerializeField] GameObject nightLightContainer;
    private Light[] dayLights;
    private Light[] nightLights;
    [SerializeField] Material daySkybox;
    [SerializeField] Material nightSkybox;

    #endregion

    #region AwakeStart

    private void Awake()
    {
        GetRefs();
        ActivateChooseNameHUD();
    }

    private void Start()
    {
        dayLightContainer.SetActive(true);
        dayLights = dayLightContainer.GetComponentsInChildren<Light>();
        nightLights = nightLightContainer.GetComponentsInChildren<Light>();
        ChangeLightMode();
    }

    void GetRefs()
    {
        if(!polePosition) FindObjectOfType<PolePositionManager>();
        if (!m_NetworkManager) m_NetworkManager = FindObjectOfType<CustomNetworkManager>();
    }

    #endregion

    #region lighting

    public void ChangeLightMode()
    {
        foreach (Light l in nightLights) l.enabled = !hasDayLight;
        foreach (Light l in dayLights) l.enabled = hasDayLight;
        RenderSettings.skybox = hasDayLight ? daySkybox : nightSkybox;
    }

    #endregion

    #region changeState

    public void ActivateChooseNameHUD()
    {
        controlsHUD.SetActive(false);
        chooseNameHUD.gameObject.SetActive(true);
        mainMenuHUD.gameObject.SetActive(false);
        roomHUD.gameObject.SetActive(false);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(false);
        if(polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
    }

    public void ActivateControlsHUD()
    {
        controlsHUD.SetActive(true);
        chooseNameHUD.gameObject.SetActive(false);
        mainMenuHUD.gameObject.SetActive(false);
        roomHUD.gameObject.SetActive(false);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(false);
    }

    public void ActivateMainMenu()
    {
        controlsHUD.SetActive(false);
        mainMenuHUD.HideNetworkButtons(false);
        rankingHUD.Reset();

        chooseNameHUD.gameObject.SetActive(false);
        mainMenuHUD.gameObject.SetActive(true);
        roomHUD.gameObject.SetActive(false);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(false);
        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
    }

    public void ActivateRoomHUD()
    {
        controlsHUD.SetActive(false);
        chooseNameHUD.gameObject.SetActive(false);
        mainMenuHUD.gameObject.SetActive(false);
        roomHUD.gameObject.SetActive(true);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(false);

        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StartCoroutine("UpdateRoomUICoroutine");
    }

    public void ActivateInGameHUD()
    {
        controlsHUD.SetActive(false);
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
        controlsHUD.SetActive(false);
        chooseNameHUD.gameObject.SetActive(false);
        mainMenuHUD.gameObject.SetActive(false);
        roomHUD.gameObject.SetActive(false);
        gameHUD.gameObject.SetActive(false);
        rankingHUD.gameObject.SetActive(true);
        if (polePosition && polePosition.isActiveAndEnabled) polePosition.StopCoroutine("UpdateRoomUICoroutine");
    }

    #endregion changeState
}