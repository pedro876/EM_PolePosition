using System;
using Mirror;
using UnityEngine;
using Random = System.Random;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
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
    [SerializeField] WheelCollider[] wheelColls;


    #endregion variables

    private void Awake()
    {
        m_PlayerInfo = GetComponent<PlayerInfo>();
        m_PlayerController = GetComponent<PlayerController>();
        m_NetworkManager = FindObjectOfType<NetworkManager>();
        m_PolePositionManager = FindObjectOfType<PolePositionManager>();
        m_UIManager = FindObjectOfType<UIManager>();
        rb = GetComponent<Rigidbody>();
        bc = GetComponent<BoxCollider>();
    }

    void Start()
    {
        if (!isServer)
        {
            rb.isKinematic = true;
            bc.enabled = false;
            foreach (WheelCollider wc in wheelColls) wc.enabled = false;
        }
    }

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
            m_PlayerInfo.CmdChangeName(m_UIManager.playerName);
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
        ConfigureCamera();
    }

    #endregion

    public void ReleasePlayer()
    {
        if (isServer)
            m_PlayerController.enabled = true;

        rb.constraints = RigidbodyConstraints.None;
    }

    void ConfigureCamera()
    {
        if (Camera.main != null) Camera.main.gameObject.GetComponent<CameraController>().SetFocus(this.gameObject);
    }

    private void OnDestroy()
    {
        m_PolePositionManager.RemovePlayer(m_PlayerInfo);
    }
}