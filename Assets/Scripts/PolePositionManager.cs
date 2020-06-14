using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;
using System.Threading;
using Mirror.Examples.Basic;
using UnityEngine.UI;

/*
 * Gestiona todos los datos relacionados con la carrera y los mantiene actualizamos para los jugadores.
 * */

public class PolePositionManager : NetworkBehaviour
{
    #region Vars

    [SerializeField] private UIManager m_uiManager;
    [SerializeField] private CustomNetworkManager networkManager;
    [SerializeField] private CircuitController m_CircuitController;
    [SerializeField] private CameraController mainCamera;

    [Header("RaceStartPos")]
    [SerializeField] private Transform[] startPositions;

    [Header("RaceConditions")]
    [SyncVar] public int maxNumPlayers=1;
    [SyncVar] public int maxLaps = 3;
    private int secondsLeft = 3;
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private readonly List<PlayerInfo> m_Ranking = new List<PlayerInfo>();

    [Header("ClasificationLap")]
    public bool clasificationLap = false;
    [Server] public void SetClasificationLap(bool v) { clasificationLap = v; }
    private int finishedPlayersCount = 0;

    [Header("RaceProgress")]
    [SerializeField] private float progressInterval = 0.1f;
    [SerializeField] private float lastPlayerGracePeriod = 20f;
    private string myRaceOrder;
    private bool orderCoroutineCalled = false;
    [SyncVar] [HideInInspector] public bool inGame = false;

    [Header("RoomProperties")]
    [SerializeField] private float updatePlayersListInterval = 0.3f;
    [SerializeField] private bool admitsPlayers = true;
    [Server] public void SetAdmitsPlayers(bool v) { admitsPlayers = v; }

    object addPlayerLock = new object();

    
    #endregion

    #region AwakeStartUpdate

    private void Awake()
    {
        GetRefs();
    }

    void GetRefs()
    {
        if (!mainCamera) mainCamera = Camera.main.GetComponent<CameraController>();
        if (networkManager == null) networkManager = FindObjectOfType<CustomNetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
    }

    [Server]
    private void Update()
    {
        if (m_Players.Count == 0) return;
        UpdateRaceProgress();
    }

    #endregion

    #region IncrementDecrementLaps

    [Server]
    public void IncrementLaps(Text refText)
    {
        if(maxLaps < 9) maxLaps++;
        if (refText != null) refText.text = maxLaps.ToString();
    }

    [Server]
    public void DecrementLaps(Text refText)
    {
        if(maxLaps > 1) maxLaps--;
        if (refText != null) refText.text = maxLaps.ToString();
    }

    #endregion

    #region roomUIUpdate

    [Client]
    IEnumerator UpdateRoomUICoroutine()
    {
        while (true)
        {
            if(m_Players.Count > 0)
            {
                foreach (PlayerInfo player in m_Players)
                {
                    player.UpdateRoomUI();
                }
            }
            m_uiManager.roomHUD.RemoveLostPlayersFromUI(m_Players, maxNumPlayers);
            yield return new WaitForSeconds(updatePlayersListInterval);
        }
    }

    [Client]
    private void OnEnable()
    {
        StartCoroutine("UpdateRoomUICoroutine");
        Reset();
    }

    private void Reset()
    {
        clasificationLap = false;
        finishedPlayersCount = 0;
        maxNumPlayers = 4;
        m_Ranking.Clear();
        inGame = false;
        orderCoroutineCalled = false;
        admitsPlayers = true;
    }

    #endregion

    #region StartGameAndChangeToGameHUD

    [Server]
    public void StartGame()
    {
        bool allReady = true;
        foreach(PlayerInfo player in m_Players) allReady = allReady && player.ready;

        if (allReady)
        {
            
            maxNumPlayers = m_Players.Count;
            inGame = true;
            admitsPlayers = false;
            RpcChangeUIFromRoomToGame();
            secondsLeft = 3;
            if (clasificationLap) StartClasificationLap();
            StopCoroutine("DecreaseCountdownCoroutine");
            StartCoroutine("DecreaseCountdownCoroutine");
            RpcUpdateCountdownUI(secondsLeft);
        }
    }

    [ClientRpc]
    void RpcChangeUIFromRoomToGame()
    {
        m_uiManager.ActivateInGameHUD();
    }

    #endregion

    #region addAndRemovePlayers

    /*Se encarga de añadir un jugador a la partida. Actualiza la interfaz de los juagdores restantes, y en caso de haber llegado al maximo de jugadores, 
    *llama a la corrutina que hace comenzar la cuenta atras previa a la carrera. Cuando se añade el primer jugador a la partida, hará comenzar una 
    *corrutina que se encargara de mostrar el orden de los jugadores hasta que termine la misma.
    */
    public void AddPlayer(PlayerInfo player)
    {
        lock (addPlayerLock)
        {
            if (!admitsPlayers && isServer)
            {
                player.connectionToClient.Disconnect();
                return;
            }
            m_Players.Add(player);
            if (isServer)
            {
                player.permissionGranted = true;
                player.transform.position = startPositions[m_Players.Count - 1].position;
                player.transform.rotation = startPositions[m_Players.Count - 1].rotation;
                player.RpcResetCam();
                m_uiManager.roomHUD.AddPlayerToRoomUI(player, m_Players);
                if (!orderCoroutineCalled)
                {
                    orderCoroutineCalled = true;
                    StartCoroutine("SortRaceOrderCoroutine");
                }
            }
        }
    }

    /*Elimina un jugador y actualiza en la interfaz el número de jugadores restantes en caso de no haber empezado la partida.*/
    public void RemovePlayer(PlayerInfo player)
    {
        lock (addPlayerLock)
        {
            int playerIndex = m_Players.IndexOf(player);
            if (playerIndex > -1)
            {
                m_Players.RemoveAt(playerIndex);
                m_uiManager.roomHUD.ReAssignUIPlayers(m_Players, maxNumPlayers);
                for (int i = playerIndex; i < m_Players.Count; i++)
                {
                    if (m_Players[i] != null && !inGame)
                        m_Players[i].transform.position = startPositions[i].position;
                }
                maxNumPlayers = m_Players.Count;
                CheckFinishClasificationLap();
            }
        }
    }

    #endregion addAndRemovePlayers

    #region countdown
    /*Cuando la cuenta atras llega a 0 libera los playerControllers para que los coches se puedan empezar a mover.*/
    [Server]
    void UpdateCountdownUI()
    {
        RpcUpdateCountdownUI(secondsLeft);
        if (secondsLeft == 0)
        {
            foreach (var player in m_Players)
            {
                player.GetComponent<SetupPlayer>().ReleasePlayer();
            }
        }
    }

    [Client]
    IEnumerator DecreaseCountdownCoroutine()
    {
        while (secondsLeft > 0)
        {
            yield return new WaitForSeconds(2);
            secondsLeft--;
            UpdateCountdownUI();
        }
    }

    /*Actualiza la interfaz de la cuenta atras con los segundos restantes para todos los clientes.*/
    [ClientRpc]
    void RpcUpdateCountdownUI(int seconds)
    {
        m_uiManager.gameHUD.UpdateCountdownText(seconds);
    }

    #endregion countdown

    #region ranking

    /*Añade el nombre y mejor tiempo del jugador que haya terminado la carrera en la interfaz de todos los clientes.*/
    [ClientRpc]
    void RpcAddPlayerToRanking(string pName, string bestTime)
    {
        m_uiManager.rankingHUD.AddPlayerToRanking(pName, bestTime);
    }

    #endregion

    #region raceProgress

    /*Actualiza el orden de jugadores para mostrarlo en la interfaz inGame. En caso de que un jugador se haya terminado la carrera,
    *dejara de mostrarse inGame para añadir su nombre y mejor tiempo en el ranking.
    *Si todos los jugadores menos uno han terminado, se le dará un tiempo de gracia para intentar terminar la carrera. Esto no se 
    *aplicará cuando solo haya un corredor.
    **/
    [Server]
    public void UpdateRaceProgress()
    {
        for (int i = 0; i < m_Players.Count; i++)
        {
            m_Players[i].arcLength = ComputeCarArcLength(i);
            if (m_Players[i].Finish)
            {
                m_Ranking.Add(m_Players[i]);
                RpcAddPlayerToRanking(m_Players[i].PlayerName, m_Players[i].bestLap);
                m_Players.Remove(m_Players[i]);
            }
        }

        if (m_Ranking.Count == maxNumPlayers - 1 && maxNumPlayers != 1)
            StartCoroutine("WaitingLastPlayer");
    }

    [Server]
    IEnumerator SortRaceOrderCoroutine()
    {
        while (m_Ranking.Count < maxNumPlayers)
        {
            SortRaceOrder();
            yield return new WaitForSeconds(progressInterval);
        }
    }

    [Server]
    void SortRaceOrder()
    {
        m_Players.Sort(new PlayerInfoComparer());

        myRaceOrder = "";
        if (!clasificationLap)
        {
            for (int i = 0; i < m_Players.Count; i++)
            {
                myRaceOrder += m_Players[i].PlayerName;
                if (i < m_Players.Count - 1)
                    myRaceOrder += "\n";
            }
        }
        RpcUpdateRaceProgressUI(myRaceOrder);
    }

    [ClientRpc]
    void RpcUpdateRaceProgressUI(string raceOrder)
    {
        m_uiManager.gameHUD.UpdatePosition(raceOrder);
    }

    #endregion

    #region lastPlayer

    [Server]
    IEnumerator WaitingLastPlayer()
    {
        yield return new WaitForSeconds(lastPlayerGracePeriod);
        if (m_Players.Count != 0)
            m_Players[0].Finish = true;
    }

    #endregion

    #region CarArcLength

    /*Se encarga de calcular la distancia recorrida desde el inicio de la carrera, teniendo en cuenta el número de vueltas.*/
    float ComputeCarArcLength(int ID)
    {
        // Compute the projection of the car position to the closest circuit 
        // path segment and accumulate the arc-length along of the car along
        // the circuit.
        Vector3 carPos = this.m_Players[ID].transform.position;

        int segIdx;
        float carDist;
        Vector3 carProj;

        float minArcL = this.m_CircuitController.ComputeClosestPointArcLength(carPos, out segIdx, out carProj, out carDist);
        m_Players[ID].currentSegDir = m_CircuitController.GetSegment(segIdx);
        if (m_Players[ID].CircuitProgress.UpdateProgress(minArcL / m_CircuitController.CircuitLength))
            m_Players[ID].AddLap();
            
        //this.m_DebuggingSpheres[ID].transform.position = carProj;
        
        minArcL += m_CircuitController.CircuitLength * (m_Players[ID].CurrentLap);
        
        return minArcL;
    }

    #endregion

    #region ClasificationLap

    [Server]
    public void StartClasificationLap()
    {
        RpcTransparentPlayer(true);
        RpcHideUIClasificationLap(true);
        int clasificationLayer = LayerMask.NameToLayer("Clasification");
        foreach (PlayerInfo player in m_Players)
        {
            Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
            foreach (Collider c in playerColliders) c.gameObject.layer = clasificationLayer;
            player.gameObject.layer = clasificationLayer;
            player.transform.position = startPositions[0].position;
            player.transform.rotation = startPositions[0].rotation;
            player.RpcResetCam();
        }
        secondsLeft = 4;
    }

    [ClientRpc]
    private void RpcHideUIClasificationLap(bool hide)
    {
        
        m_uiManager.gameHUD.HideLapsAndRaceOrder(hide);
    }

    [Server]
    public void ClasificationLapFinished()
    {
        RpcTransparentPlayer(false);
        clasificationLap = false;
        int raceLayer = LayerMask.NameToLayer("Default");
        finishedPlayersCount = 0;
        foreach (PlayerInfo player in m_Players)
        {
            player.CurrentLap = 0;
            player.bestLap = "--:--:--";
            Collider[] playerColliders = player.GetComponentsInChildren<Collider>();
            foreach (Collider c in playerColliders) c.gameObject.layer = raceLayer;
            player.gameObject.layer = raceLayer;
        }
        RpcHideUIClasificationLap(false);
        RpcDisableWaitingUI();
        StartGame();

    }

    [ClientRpc]
    void RpcDisableWaitingUI()
    {
        Debug.Log("desactivando");
        m_uiManager.gameHUD.HideWaitingText(true);
    }

    [Server]
    public void PlayerFinishedClasificationLap(PlayerInfo player)
    {
        player.transform.position = startPositions[finishedPlayersCount].position;
        player.transform.rotation = startPositions[finishedPlayersCount].rotation;
        player.RpcResetCam();
        player.GetComponent<SetupPlayer>().BlockPlayer();
        if(maxNumPlayers-finishedPlayersCount>1) player.RpcActivateWaitingUI();
        finishedPlayersCount++;
        CheckFinishClasificationLap();
        
    }

    [Server]
    void CheckFinishClasificationLap()
    {
        if (finishedPlayersCount == maxNumPlayers && clasificationLap)
        {
            ClasificationLapFinished();
        }
    }

    [ClientRpc]
    void RpcTransparentPlayer(bool transparent)
    {
        foreach (PlayerInfo player in m_Players)
        {
            if (!player.isLocalPlayer)
                player.SetTransparency(transparent);
        }
    }


    #endregion
}