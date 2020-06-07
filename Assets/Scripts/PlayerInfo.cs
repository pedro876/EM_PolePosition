using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
    public string PlayerName { get; set; }

    public int ID { get; set; }

    public int CurrentPosition { get; set; }

    public int CurrentLap { get; set; }

    public float LastArcLength { get; set; }

    public CircuitProgress CircuitProgress { get; set; }

    public override string ToString()
    {
        return PlayerName;
    }
}

public class PlayerInfoComparer : Comparer<PlayerInfo>
{

    /*public PlayerInfoComparer()
    {

    }*/

    public override int Compare(PlayerInfo x, PlayerInfo y)
    {
        if (x.LastArcLength < y.LastArcLength)
            return 1;
        else return -1;
    }
}