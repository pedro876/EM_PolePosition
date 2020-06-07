using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Mirror;
using UnityEngine;
using System.Threading;

public class PolePositionManager : NetworkBehaviour
{
    public static int maxNumPlayers=2;
    public NetworkManager networkManager;

    private readonly List<PlayerInfo> m_Players = new List<PlayerInfo>();
    private CircuitController m_CircuitController;
    private GameObject[] m_DebuggingSpheres;

    private int secondsLeft = 3;
    
    private List<CircuitProgress> m_PlayersProgresses = new List<CircuitProgress>();

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
        if (m_Players.Count==maxNumPlayers)
            StartCoroutine("DecreaseCountdown");
    }

    IEnumerator DecreaseCountdown()
    {
        while (secondsLeft > 0)
        {
            Debug.Log(secondsLeft);
            yield return new WaitForSeconds(2);
            secondsLeft--;
            //mostrar en interfaz segundos 3,2,1, GO!
        }

        foreach (var player in m_Players)
        {
            player.GetComponent<SetupPlayer>().ReleasePlayer();

        }
        //ocultar interfaz
    }

    public void RemovePlayer(PlayerInfo player)
    {
        m_Players.Remove(player);
    }

    private class PlayerInfoComparer : Comparer<PlayerInfo>
    {
        float[] m_ArcLengths;

        public PlayerInfoComparer(float[] arcLengths)
        {
            m_ArcLengths = arcLengths;
        }

        public override int Compare(PlayerInfo x, PlayerInfo y)
        {
            if (this.m_ArcLengths[x.ID] < m_ArcLengths[y.ID])
                return 1;
            else return -1;
        }
    }

    public void UpdateRaceProgress()
    {
        // Update car arc-lengths
        float[] arcLengths = new float[m_Players.Count];

        for (int i = 0; i < m_Players.Count; ++i)
        {
            arcLengths[i] = ComputeCarArcLength(i);
        }

        m_Players.Sort(new PlayerInfoComparer(arcLengths));

        string myRaceOrder = "";
        foreach (var _player in m_Players)
        {
            myRaceOrder += _player.PlayerName + " ";
        }

        //Debug.Log("El orden de carrera es: " + myRaceOrder);
    }

    /*Calculo para aumenta vuelta*/
    void IncreseLap(int ID)
    {
        m_Players[ID].CurrentLap++;
        m_PlayersProgresses[ID].Reset();
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

        float minArcL =
            this.m_CircuitController.ComputeClosestPointArcLength(carPos, out segIdx, out carProj, out carDist);



        this.m_DebuggingSpheres[ID].transform.position = carProj;

        if (this.m_Players[ID].CurrentLap == 0)
        {
            minArcL -= m_CircuitController.CircuitLength;
        }
        else
        {
            minArcL += m_CircuitController.CircuitLength *
                       (m_Players[ID].CurrentLap - 1);
        }

        /*Comprueba si ha llegado a algun checkpoint. Hay dos casos:
         * 1. Comprueba si acabamos de completar una vuelta, por lo que last es 0 (se resetea) y no tenemos que comparar con el checkpoint anterior.
         * O en caso de ser un checkpoint del medio del mapa, que el anterior esté visitado. Y si nuestra posicion actual es igual a la del primer 
         * checkpoint, se activa el bool de este checkpoint y last se aumenta en uno para empezar a comprobar el siguiente.
         * 2. Si nos encontramos en el ultimo checkpoint. Por lo que last es el numero de puntos menos uno (porque empezamos en 0), el checkpoint 
         * anterior está visitado y nos encontramos en el punto del circuito del checkpoint, aumentamos una vuelta con el metodo IncreseLap,
         * que resetea los checkpoints visitados y aumenta una vuelta al jugador.
         * 
         * */

        
        
        /*if(m_PlayersProgresses[ID].actual== m_PlayersProgresses[ID].spots-1 && 
            m_PlayersProgresses[ID].visitedSpots[m_PlayersProgresses[ID].actual-1].visited==true && 
            m_PlayersProgresses[ID].visitedSpots[m_PlayersProgresses[ID].actual].progress == (minArcL - (m_CircuitController.CircuitLength * (m_Players[ID].CurrentLap - 1))))
        {
            IncreseLap(ID);
        }
        else if ((m_PlayersProgresses[ID].actual == 0 || m_PlayersProgresses[ID].visitedSpots[m_PlayersProgresses[ID].actual - 1].visited == true) &&
            m_PlayersProgresses[ID].visitedSpots[m_PlayersProgresses[ID].actual].progress == (minArcL - (m_CircuitController.CircuitLength * (m_Players[ID].CurrentLap - 1))))
        {
            m_PlayersProgresses[ID].visitedSpots[m_PlayersProgresses[ID].actual].visited = true;
            m_PlayersProgresses[ID].actual++;
        }*/

        return minArcL;
        
    
    }
}