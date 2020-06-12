using System;
using Mirror;
using UnityEngine;
using Random = System.Random;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

/*
 * 
 */

public class SetupPlayer : NetworkBehaviour
{
    #region variables

    private UIManager m_UIManager;
    private NetworkManager m_NetworkManager;
    private PlayerController m_PlayerController;
    private PlayerInfo m_PlayerInfo;
    private PolePositionManager m_PolePositionManager;
    private Rigidbody rb;
    private BoxCollider bc;
    private CameraController mainCam;
    [SerializeField] WheelCollider[] wheelColls;


    #endregion variables

    #region AwakeStart
    private void Awake()
    {
        m_PlayerInfo = GetComponent<PlayerInfo>();
        m_PlayerController = GetComponent<PlayerController>();
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_UIManager = FindObjectOfType<UIManager>();
        rb = GetComponent<Rigidbody>();
        bc = GetComponent<BoxCollider>();
        if (Camera.main != null) mainCam = Camera.main.gameObject.GetComponent<CameraController>();
    }

    /*
     * En caso de no ser el servidor, no se simulan físicas y se desactivan los colliders
     */
    void Start()
    {
        if (!isServer)
        {
            rb.isKinematic = true;
            bc.enabled = false;
            foreach (WheelCollider wc in wheelColls) wc.enabled = false;
        }
    }

    #endregion

    #region Start & Stop Callbacks

    /// <summary>
    /// This is invoked for NetworkBehaviour objects when they become active on the server.
    /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
    /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
    /// </summary>
    public override void OnStartServer()
    {
        base.OnStartServer();
        m_PlayerInfo.ID = connectionToClient.connectionId;
        Debug.Log("Server iniciado");
    }

    /// <summary>
    /// Called on every NetworkBehaviour when it is activated on a client.
    /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            //Al iniciarse un cliente, si es localPlayer, debe mandarse el nombre introducido al servidor
            m_PlayerInfo.CmdChangeName(m_UIManager.playerName);
            if (!m_PolePositionManager.admitsPlayers)
            {
                m_NetworkManager.StopClient();
                return;
            }
            else
            {
                ConfigureCamera();
                m_UIManager.ActivateRoomHUD(isServer);
            }
        }
        m_PolePositionManager.AddPlayer(m_PlayerInfo);
    }

    /// <summary>
    /// Called when the local player object has been set up.
    /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
    /// </summary>
    /// 

    public override void OnStartLocalPlayer()
    {
        //ConfigureCamera();
    }

    #endregion

    #region ReleasePlayer
    /*
     * Permite a un jugador moverse, por ejemplo cuando acaba el countdown
     */
    public void ReleasePlayer()
    {
        if (isServer)
            m_PlayerController.enabled = true;

        rb.constraints = RigidbodyConstraints.None;
    }

    #endregion

    #region ConfigureCamera
    /*
     * Indica a la cámara que utilice a este objeto como target
     */
    void ConfigureCamera()
    {
        mainCam.SetFocus(this.gameObject);
    }

    #endregion

    #region OnDestroyRemovePlayer
    /*
     * Al destruirse esta instancia, debe eliminarse de la lista de jugadores
     */
    private void OnDestroy()
    {
        m_PolePositionManager.RemovePlayer(m_PlayerInfo);
    }

    #endregion
}