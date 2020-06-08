using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class PlayerInfo : NetworkBehaviour
{

    #region variables

    public string PlayerName { get; set; }
    public int ID { get; set; }
    [SyncVar(hook = nameof(FinishCircuit))] public bool Finish;
    [SyncVar(hook = nameof(UpdateLapUI))] public int CurrentLap;
    [SyncVar(hook = nameof(IsWrongDirection))] public float LastArcLength;
    [SerializeField] private float wrondDirDelayTime = 2.0f;
    private bool wrongDir = false;
    private bool coroutineCalled = false;
    public UIManager uiManager;
    public CircuitProgress CircuitProgress { get; set; }

    #endregion

    public override string ToString() { return PlayerName; }

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

    #region finishFuncs

    private void FinishCircuit(bool oldVal, bool newVal)
    {
        if (newVal) {
            transform.gameObject.SetActive(false);
            if (isLocalPlayer)
            {
                uiManager.ActivateRankingHUD();
            }
        }
        //Cambiar a cámara de otro jugador en cinematico
    }

    #endregion finishFuncs

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


    #region wrongDirection
    public void IsWrongDirection(float oldVal, float newVal)
    {
        if (isLocalPlayer)
        {
            wrongDir = oldVal > newVal;
            if (!coroutineCalled && wrongDir)
            {
                coroutineCalled = true;
                StartCoroutine("WrongDirDelay");
            }
        }
    }

    IEnumerator WrongDirDelay()
    {
        yield return new WaitForSeconds(wrondDirDelayTime);
        uiManager.backwardsText.gameObject.SetActive(wrongDir);
        coroutineCalled = false;
    }

    #endregion

    #region updateLap

    public void AddLap()
    {
        CurrentLap++;
        if (CurrentLap > PolePositionManager.maxLaps)
        {
            //transform.gameObject.SetActive(false);
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