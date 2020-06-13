using System;
using Mirror;
using UnityEngine;
using Random = System.Random;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

/*
 * Gestiona la configuración incial del jugador al unirse a una partida
 */

public class SetupPlayer : NetworkBehaviour
{
    #region Vars

    private UIManager m_UIManager;
    private PlayerController m_PlayerController;
    private PlayerInfo m_PlayerInfo;
    private PolePositionManager m_PolePositionManager;
    private Rigidbody rb;
    private BoxCollider bc;
    [SerializeField] WheelCollider[] wheelColls;

    #endregion

    #region AwakeStart
    private void Awake()
    {
        GetRefs();
    }

    void GetRefs()
    {
        if(!m_PlayerInfo) m_PlayerInfo = GetComponent<PlayerInfo>();
        if(!m_PlayerController) m_PlayerController = GetComponent<PlayerController>();
        if(!m_PolePositionManager) m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        if(!m_UIManager) m_UIManager = FindObjectOfType<UIManager>();
        if(!rb) rb = GetComponent<Rigidbody>();
        if(!bc) bc = GetComponent<BoxCollider>();
    }

    /*
     * En caso de no ser el servidor, no se simulan físicas y se desactivan los colliders
     */
    [Server]
    void Start()
    {
        rb.isKinematic = false;
        bc.enabled = true;
        foreach (WheelCollider wc in wheelColls) wc.enabled = true;
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
    }

    /// <summary>
    /// Called on every NetworkBehaviour when it is activated on a client.
    /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();
        GetRefs();
        m_PolePositionManager.AddPlayer(m_PlayerInfo);
        if (isLocalPlayer)
        {
            //Al iniciarse un cliente, si es localPlayer, debe mandarse el nombre introducido al servidor
            m_PlayerInfo.CmdChangeName(m_UIManager.chooseNameHUD.playerName);
        }
    }

    /// <summary>
    /// Called when the local player object has been set up.
    /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
    /// </summary>
    /// 
    /*public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        //Al iniciarse un cliente, si es localPlayer, debe mandarse el nombre introducido al servidor
        GetRefs();
        
    }*/

    #endregion

    #region ReleasePlayer
    /*
     * Permite a un jugador moverse, por ejemplo cuando acaba el countdown
     */
    [Server]
    public void ReleasePlayer()
    {
        m_PlayerController.enabled = true;
        rb.constraints = RigidbodyConstraints.None;
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