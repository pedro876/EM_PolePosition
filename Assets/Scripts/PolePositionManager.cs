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
    public int maxNumPlayers=1;
    public static int maxLaps = 2;
    public NetworkManager networkManager;

    [SyncVar(hook = nameof(RaceOrder))] public string myRaceOrder;

    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private readonly List<PlayerInfo> m_Ranking = new List<PlayerInfo>();
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;
    

    [SerializeField][SyncVar(hook = nameof(UpdateCountdownUI))]private int secondsLeft = 3;

    #endregion variables

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

    [SyncVar(hook = nameof(AddPlayerToRanking))] string newRankingName;

    #region addAndRemovePlayers

    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        m_uiManager.UpdateCountdownText(m_Players.Count, maxNumPlayers, m_Players.Count == maxNumPlayers, secondsLeft);
        if (isServer && m_Players.Count == maxNumPlayers)
        {
            StartCoroutine("DecreaseCountdown");
        }
    }

    public void RemovePlayer(PlayerInfo player)
    {
        m_Players.Remove(player);
        m_uiManager.UpdateCountdownText(m_Players.Count, maxNumPlayers, m_Players.Count == maxNumPlayers, secondsLeft);
    }

    #endregion addAndRemovePlayers

    #region countdown

    void UpdateCountdownUI(int oldVal, int newVal)
    {
        m_uiManager.UpdateCountdownText(m_Players.Count, maxNumPlayers, m_Players.Count == maxNumPlayers, newVal);
        Debug.Log("actualizando");
        if (newVal == 0)
        {
            foreach (var player in m_Players)
            {
                player.GetComponent<SetupPlayer>().ReleasePlayer();
            }
        }
    }

    IEnumerator DecreaseCountdown()
    {
        while (secondsLeft > 0)
        {
            yield return new WaitForSeconds(2);
            secondsLeft--;
        }
    }

    #endregion countdown

    #region raceProgress

    void AddPlayerToRanking(string oldVal, string newVal)
    {
        if(newVal != oldVal) m_uiManager.AddPlayerToRanking(newVal);
    }

    public void UpdateRaceProgress()
    {
        //Debug.Log("actualizando race progress");
        for (int i = 0; i < m_Players.Count; i++)
        {
            m_Players[i].LastArcLength = ComputeCarArcLength(i);
            if (m_Players[i].Finish)
            {
                m_Ranking.Add(m_Players[i]);
                newRankingName = m_Players[i].PlayerName;
                m_Players.Remove(m_Players[i]);
            }
        }

        if (m_Ranking.Count == maxNumPlayers - 1 && maxNumPlayers != 1)
            StartCoroutine("WaitingLastPlayer");

        m_Players.Sort(new PlayerInfoComparer());

        myRaceOrder = "";
        foreach (var _player in m_Players)
        {
            myRaceOrder += _player.PlayerName + "\n ";
            //Debug.Log(myRaceOrder);
        }
        
    }

    public void RaceOrder(string oldVal,string newVal)
    {
        m_uiManager.UpdatePosition(myRaceOrder);
    }

    IEnumerator WaitingLastPlayer()
    {
        yield return new WaitForSeconds(20);
        if (m_Players.Count != 0)
            m_Players[0].AddLap();
    }

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

    #endregion raceProgress
}