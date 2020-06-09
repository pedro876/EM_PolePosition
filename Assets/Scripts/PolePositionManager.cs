using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;
using System.Threading;

public class PolePositionManager : NetworkBehaviour
{
    #region variables

    [SerializeField] UIManager m_uiManager;
    public NetworkManager networkManager;
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;

    [Header("RaceConditions")]
    public int maxNumPlayers=1;
    public static int maxLaps = 3;
    private int secondsLeft = 3;
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private readonly List<PlayerInfo> m_Ranking = new List<PlayerInfo>();

    [Header("RaceProgress")]
    [SerializeField] private float progressInterval = 0.1f;
    [SerializeField] private float lastPlayerGracePeriod = 20f;
    private string myRaceOrder;
    bool orderCoroutineCalled = false;

    #endregion variables

    #region netVariables

    //[SyncVar(hook = nameof(AddPlayerToRanking))] string newRankingName;

    #endregion

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();

        m_DebuggingSpheres = new GameObject[networkManager.maxConnections];
        for (int i = 0; i < networkManager.maxConnections; ++i)
        {
            m_DebuggingSpheres[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DebuggingSpheres[i].GetComponent<SphereCollider>().enabled = false;
        }
    }

    private void Update()
    {
        if (m_Players.Count == 0) return;
        if(isServer) UpdateRaceProgress();
    }

    #region addAndRemovePlayers

    public void AddPlayer(PlayerInfo player)
    {
        Debug.Log("addPlayer");
        m_Players.Add(player);
        if (isServer)
        {
            RpcUpdateCountdownUI(m_Players.Count, maxNumPlayers, m_Players.Count == maxNumPlayers, secondsLeft);
            if (m_Players.Count == maxNumPlayers)
            {
                StartCoroutine("DecreaseCountdownCoroutine");
            }
            else if (!orderCoroutineCalled)
            {
                orderCoroutineCalled = true;
                StartCoroutine("SortRaceOrderCoroutine");
            }
        }
    }

    public void RemovePlayer(PlayerInfo player)
    {
        m_Players.Remove(player);
        RpcUpdateCountdownUI(m_Players.Count, maxNumPlayers, m_Players.Count == maxNumPlayers, secondsLeft);
    }

    #endregion addAndRemovePlayers

    #region countdown

    void UpdateCountdownUI()
    {
        RpcUpdateCountdownUI(m_Players.Count, maxNumPlayers, m_Players.Count == maxNumPlayers, secondsLeft);
        if (secondsLeft == 0)
        {
            foreach (var player in m_Players)
            {
                player.GetComponent<SetupPlayer>().ReleasePlayer();
            }
        }
    }

    IEnumerator DecreaseCountdownCoroutine()
    {
        while (secondsLeft > 0)
        {
            yield return new WaitForSeconds(2);
            secondsLeft--;
            UpdateCountdownUI();
        }
    }

    [ClientRpc]
    void RpcUpdateCountdownUI(int numPlayers, int maxPlayers, bool countdownActive, int seconds)
    {
        m_uiManager.UpdateCountdownText(numPlayers, maxPlayers, countdownActive, seconds);
    }

    #endregion countdown

    #region ranking

    [ClientRpc]
    void RpcAddPlayerToRanking(string pName, string bestTime)
    {
        m_uiManager.AddPlayerToRanking(pName, bestTime);
    }

    #endregion

    #region raceProgress

    public void UpdateRaceProgress()
    {
        for (int i = 0; i < m_Players.Count; i++)
        {
            m_Players[i].LastArcLength = ComputeCarArcLength(i);
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

    IEnumerator SortRaceOrderCoroutine()
    {
        while(m_Ranking.Count < maxNumPlayers)
        {
            SortRaceOrder();
            yield return new WaitForSeconds(progressInterval);
        }
    }

    void SortRaceOrder()
    {
        m_Players.Sort(new PlayerInfoComparer());

        myRaceOrder = "";
        foreach (var _player in m_Players)
            myRaceOrder += _player.PlayerName + "\n ";

        RpcUpdateRaceProgressUI(myRaceOrder);
    }

    [ClientRpc]
    void RpcUpdateRaceProgressUI(string raceOrder)
    {
        m_uiManager.UpdatePosition(raceOrder);
    }

    #endregion

    #region lastPlayer

    IEnumerator WaitingLastPlayer()
    {
        yield return new WaitForSeconds(lastPlayerGracePeriod);
        if (m_Players.Count != 0)
            m_Players[0].Finish = true;
            //m_Players[0].AddLap();
    }

    #endregion

    /*Porcentaje de la vuelta*/
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

        if (m_Players[ID].CircuitProgress.UpdateProgress(minArcL / m_CircuitController.CircuitLength))
            m_Players[ID].AddLap();
            
        this.m_DebuggingSpheres[ID].transform.position = carProj;
        
        minArcL += m_CircuitController.CircuitLength * (m_Players[ID].CurrentLap);
        
        return minArcL;
    }
}