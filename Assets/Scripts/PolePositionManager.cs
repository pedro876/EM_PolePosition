﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;
using System.Threading;

/*
 * Gestiona todos los datos relacionados con la carrera y los mantiene actualizamos para los jugadores.
 * */

public class PolePositionManager : NetworkBehaviour
{
    #region variables

    [SerializeField] UIManager m_uiManager;
    public NetworkManager networkManager;
    private CircuitController m_CircuitController;
    //private GameObject[] m_DebuggingSpheres;

    [Header("RaceStartPos")]
    [SerializeField] Transform[] startPositions;

    [Header("RaceConditions")]
    [SyncVar] public int maxNumPlayers=1;
    public static int maxLaps = 3;
    private int secondsLeft = 3;
    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private readonly List<PlayerInfo> m_Ranking = new List<PlayerInfo>();

    [Header("RaceProgress")]
    [SerializeField] private float progressInterval = 0.1f;
    [SerializeField] private float lastPlayerGracePeriod = 20f;
    private string myRaceOrder;
    bool orderCoroutineCalled = false;
    bool inGame = false;

    [Header("RoomProperties")]
    [SerializeField] float updatePlayersListInterval = 0.3f;
    [SyncVar] public bool admitsPlayers = true;
    public void SetAdmitsPlayers(bool v) { admitsPlayers = v; }


    #endregion variables

    #region AwakeStartUpdate

    private void Awake()
    {
        if (networkManager == null) networkManager = FindObjectOfType<NetworkManager>();
        if (m_CircuitController == null) m_CircuitController = FindObjectOfType<CircuitController>();
    }

    private void Update()
    {
        if (m_Players.Count == 0) return;
        if(isServer) UpdateRaceProgress();
        for(int i = maxNumPlayers; i < m_Players.Count; i++)
        {
            if (m_Players[i].isLocalPlayer) networkManager.StopClient();
        }
    }

    #endregion

    #region roomUIUpdate

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
            m_uiManager.RemoveLostPlayersFromUI(m_Players, maxNumPlayers);
            yield return new WaitForSeconds(updatePlayersListInterval);
        }
    }

    private void OnEnable()
    {
        StartCoroutine("UpdateRoomUICoroutine");
    }

    #endregion

    #region StartGameAndChangeToGameHUD

    public void StartGame()
    {
        bool allReady = true;
        foreach(PlayerInfo player in m_Players) allReady = allReady && player.ready;

        if (allReady)
        {
            inGame = true;
            maxNumPlayers = m_Players.Count;
            //networkManager.maxConnections = maxNumPlayers;
            //networkManager.maxConnections = 0;
            admitsPlayers = false;
            StartCoroutine("DecreaseCountdownCoroutine");
            RpcUpdateCountdownUI(secondsLeft);
            RpcChangeUIFromRoomToGame();
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
    **/
    public void AddPlayer(PlayerInfo player)
    {
        m_Players.Add(player);
        
        if (isServer)
        {
            player.transform.position = startPositions[m_Players.Count - 1].position;
            m_uiManager.AddPlayerToRoomUI(player, m_Players);

            if (!orderCoroutineCalled)
            {
                orderCoroutineCalled = true;
                StartCoroutine("SortRaceOrderCoroutine");
            }
        }
    }

    /*Elimina un jugador y actualiza en la interfaz el número de jugadores restantes en caso de no haber empezado la partida.*/
    public void RemovePlayer(PlayerInfo player)
    {
        int playerIndex = m_Players.IndexOf(player);
        m_Players.RemoveAt(playerIndex);
        m_uiManager.ReAssignUIPlayers(m_Players, maxNumPlayers);
        for (int i = playerIndex; i < m_Players.Count; i++)
        {
            if(m_Players[i] != null && !inGame)
                m_Players[i].transform.position = startPositions[i].position;
        }
        //networkManager.maxConnections--;
    }

    #endregion addAndRemovePlayers

    #region countdown
    /*Cuando la cuenta atras llega a 0 libera los playerControllers para que los coches se puedan empezar a mover.*/
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
        m_uiManager.UpdateCountdownText(seconds);
    }

    #endregion countdown

    #region ranking

    /*Añade el nombre y mejor tiempo del jugador que haya terminado la carrera en la interfaz de todos los clientes.*/
    [ClientRpc]
    void RpcAddPlayerToRanking(string pName, string bestTime)
    {
        m_uiManager.AddPlayerToRanking(pName, bestTime);
    }

    #endregion

    #region raceProgress

    /*Actualiza el orden de jugadores para mostrarlo en la interfaz inGame. En caso de que un jugador se haya terminado la carrera,
    *dejara de mostrarse inGame para añadir su nombre y mejor tiempo en el ranking.
    *Si todos los jugadores menos uno han terminado, se le dará un tiempo de gracia para intentar terminar la carrera. Esto no se 
    *aplicará cuando solo haya un corredor.
    **/
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
        while (m_Ranking.Count < maxNumPlayers)
        {
            SortRaceOrder();
            yield return new WaitForSeconds(progressInterval);
        }
    }

    void SortRaceOrder()
    {
        m_Players.Sort(new PlayerInfoComparer());

        myRaceOrder = "";
        for(int i = 0; i < m_Players.Count; i++)
        {
            myRaceOrder += m_Players[i].PlayerName;
            if (i < m_Players.Count - 1)
                myRaceOrder += "\n";
        }

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

        if (m_Players[ID].CircuitProgress.UpdateProgress(minArcL / m_CircuitController.CircuitLength))
            m_Players[ID].AddLap();
            
        //this.m_DebuggingSpheres[ID].transform.position = carProj;
        
        minArcL += m_CircuitController.CircuitLength * (m_Players[ID].CurrentLap);
        
        return minArcL;
    }
}