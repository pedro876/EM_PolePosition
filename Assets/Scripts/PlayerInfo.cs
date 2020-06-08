using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{
    public string PlayerName { get; set; }

    public int ID { get; set; }

    [SyncVar(hook =nameof(UpdateLapUI))] public int CurrentLap;

    [SyncVar(hook = nameof(IsWrongDirection))] public float LastArcLength;

    public CircuitProgress CircuitProgress { get; set; }

    public bool Finish { get; set; }

    public override string ToString() { return PlayerName; }

    public UIManager uiManager;

    #region inputUpdate

    [SyncVar]public float axisVertical = 0f;
    [SyncVar]public float axisHorizontal = 0f;
    [SyncVar]public float axisBrake = 0f;

    [Command]
    private void CmdUpdateInput(float aV, float aH, float aB)
    {
        axisVertical = aV;
        axisHorizontal = aH;
        axisBrake = aB;
    }

    #endregion inputUpdate

    private void Update()
    {
        
        if (this.isLocalPlayer)
        {
            CmdUpdateInput(
                Input.GetAxis("Vertical"),
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Jump"));
        }
    }

    public void IsWrongDirection(float oldVal, float newVal)
    {
        if(oldVal < newVal && uiManager.backwardsText.gameObject.activeSelf)
        {
            uiManager.backwardsText.gameObject.SetActive(false);
        }
        if (newVal < oldVal && !uiManager.backwardsText.gameObject.activeSelf)
        {
            uiManager.backwardsText.gameObject.SetActive(true);
        }
    }

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