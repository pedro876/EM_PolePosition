using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;
using System.Threading;

public class PolePositionManager : NetworkBehaviour
{
    [SerializeField] UIManager m_uiManager;
    public int maxNumPlayers=1;
    public int maxLaps = 1;
    public NetworkManager networkManager;

    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private readonly List<PlayerInfo> m_Ranking = new List<PlayerInfo>();
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;
    

    [SerializeField][SyncVar(hook = nameof(UpdateCountdownUI))]private int secondsLeft = 3;

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
        //Debug.Log("Num players: " + m_Players.Count);
        
        if (m_Players.Count == 0)
            return;

        UpdateRaceProgress();
    }

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
            //Debug.Log(secondsLeft);
            yield return new WaitForSeconds(2);
            secondsLeft--;
        }
    }

    IEnumerator WaitingLastPlayer()
    {
        yield return new WaitForSeconds(20);
        if (m_Players.Count!=0)
            m_Players[0].AddLap(maxLaps);
    }

    public void UpdateRaceProgress()
    {
        // Update car arc-lengths
        
        for (int i = 0; i < m_Players.Count; i++)
        {
            m_Players[i].LastArcLength = ComputeCarArcLength(i);
            if (m_Players[i].Finish)
            {
                m_Ranking.Add(m_Players[i]);
                m_Players.Remove(m_Players[i]);
            }
        }

        if (m_Ranking.Count == maxNumPlayers - 1 && maxNumPlayers != 1)
            StartCoroutine("WaitingLastPlayer");

        m_Players.Sort(new PlayerInfoComparer());

        string myRaceOrder = "";
        foreach (var _player in m_Players)
        {
            myRaceOrder += _player.PlayerName + " ";
        }

        //Debug.Log("El orden de carrera es: " + myRaceOrder);
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
        {
            m_Players[ID].AddLap(maxLaps);
            if (m_Players[ID].CurrentLap == 0)
            {
                m_uiManager.UpdateLap(1, maxLaps);
            }
            else
            {
                m_uiManager.UpdateLap(m_Players[ID].CurrentLap, maxLaps);
            }
        }
            
        this.m_DebuggingSpheres[ID].transform.position = carProj;
        
        minArcL += m_CircuitController.CircuitLength * (m_Players[ID].CurrentLap);
        
        return minArcL;
        
    }
}