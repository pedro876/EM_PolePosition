using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{
    public string PlayerName { get; set; }

    public int ID { get; set; }

    public int CurrentPosition { get; set; }

    [SyncVar(hook =nameof(UpdateLapUI))] public int CurrentLap;

    public float LastArcLength { get; set; }

    public CircuitProgress CircuitProgress { get; set; }

    public bool Finish { get; set; }

    public override string ToString() { return PlayerName; }

    public UIManager uiManager;

    #region updateLap

    public void AddLap()
    {
        CurrentLap++;
        if (CurrentLap > PolePositionManager.maxLaps)
        {
            transform.gameObject.SetActive(false);
            Finish = true;
        }
        Debug.Log(CurrentLap);
    }

    public void UpdateLapUI(int oldVal, int newVal)
    {
        uiManager.UpdateLap(this);
    }

    #endregion updateLap
}

public class PlayerInfoComparer : Comparer<PlayerInfo>
{
    public override int Compare(PlayerInfo x, PlayerInfo y)
    {
        if (x.LastArcLength < y.LastArcLength)
            return 1;
        else return -1;
    }
}