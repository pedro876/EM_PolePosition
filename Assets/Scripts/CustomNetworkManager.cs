using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    #region Vars
    [Header("References")]
    [SerializeField]private UIManager uiManager;
    [SerializeField]private PolePositionManager polePosition;
    [SerializeField]private CameraController mainCamera;
    #endregion

    #region UnityCallbacks
    public override void Awake()
    {
        base.Awake();
        GetRefs();
    }

    void GetRefs()
    {
        if(!uiManager) uiManager = FindObjectOfType<UIManager>();
        if(!polePosition) polePosition = FindObjectOfType<PolePositionManager>();
        if(!mainCamera) mainCamera = Camera.main.gameObject.GetComponent<CameraController>();
    }

    #endregion

    #region onClientFuncs
    public override void OnClientConnect(NetworkConnection conn)
    {
        base.OnClientConnect(conn);
        Debug.Log("OnClientConnect");
    }
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        base.OnClientDisconnect(conn);
        Debug.Log("OnClientDisconnect");
    }
    public override void OnClientError(NetworkConnection conn, int errorCode)
    {
        base.OnClientError(conn, errorCode);
        Debug.Log("OnClientError");
    }
    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("OnStartClient");
    }
    /*
     * Cuando un cliente se desconecta se le lleva al menú principal y se le indica que la conexión con el host ha terminado
     */
    public override void OnStopClient()
    {
        base.OnStopClient();
        Debug.Log("OnStopClient");

        uiManager.ActivateMainMenu();
        mainCamera.Reset();
        uiManager.mainMenuHUD.SetTemporalMessage("Connection finished");
    }
    #endregion

    #region onServerFuncs
    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        base.OnServerAddPlayer(conn);
        Debug.Log("OnServerAddPlayer");
    }
    public override void OnServerRemovePlayer(NetworkConnection conn, NetworkIdentity player)
    {
        base.OnServerRemovePlayer(conn, player);
        Debug.Log("OnServerRemovePlayer");
    }
    public override void OnServerConnect(NetworkConnection conn)
    {
        base.OnServerConnect(conn);
        Debug.Log("OnServerConnect");
    }
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        base.OnServerDisconnect(conn);
        Debug.Log("OnServerDisconnect");
    }
    public override void OnServerError(NetworkConnection conn, int errorCode)
    {
        base.OnServerError(conn, errorCode);
        Debug.Log("OnServerError");
    }
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("OnStartServer");
    }
    public override void OnStopServer()
    {
        base.OnStopServer();
        Debug.Log("OnStopServer");
    }
    #endregion

    #region onHostFuncs
    /*
     * Cuando se inicia el Host se le activan en las interfaces aquellas opciones que solo debera tener el host
     */
    public override void OnStartHost()
    {
        base.OnStartHost();
        Debug.Log("OnStartHost");

        uiManager.roomHUD.ActivateRoomHostOptions(true);
        uiManager.rankingHUD.ActivateRoomHostOptions(true);

    }
    /*
     * Cuando se desconecta el Host se le desactivan en las interfaces aquellas opciones que solo debera tener el host
     */
    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("OnStopHost");

        uiManager.roomHUD.ActivateRoomHostOptions(false);
        uiManager.rankingHUD.ActivateRoomHostOptions(false);
    }
    #endregion
}
