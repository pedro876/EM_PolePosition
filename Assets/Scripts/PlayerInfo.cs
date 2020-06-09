using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System;


public class PlayerInfo : NetworkBehaviour
{

    #region variables

    [SyncVar] public string PlayerName;
    [SyncVar] public int ID;
    [SyncVar(hook = nameof(FinishCircuit))] public bool Finish;
    [SyncVar(hook = nameof(UpdateLapUI))] public int CurrentLap;
    [SyncVar(hook = nameof(IsWrongDirection))] public float LastArcLength;
    [SerializeField] private float wrondDirDelayTime = 2.0f;
    private bool wrongDir = false;
    private bool coroutineCalled = false;
    public UIManager uiManager;
    public CircuitProgress CircuitProgress { get; set; }
    private DateTime startTime;
    private TimeSpan bestLapSpan;
    [SyncVar (hook = nameof(UpdateBestLapUI))]public string bestLap;
    

    #endregion

    

    private void Update()
    {

        if (this.isLocalPlayer)
        {
            CmdUpdateInput(
                Input.GetAxis("Vertical"),
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Jump"),
                Input.GetKeyDown(KeyCode.R));
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

    [SyncVar] public float axisVertical = 0f;
    [SyncVar] public float axisHorizontal = 0f;
    [SyncVar] public float axisBrake = 0f;
    [SyncVar] public bool mustSave = false;

    [Command]
    private void CmdUpdateInput(float aV, float aH, float aB, bool mSave)
    {
        axisVertical = aV;
        axisHorizontal = aH;
        axisBrake = aB;
        if (!mustSave) mustSave = mSave;
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
        if (CurrentLap == 1)
            startTime = DateTime.Now;
        else
        {
            DateTime endTime = DateTime.Now;
            TimeSpan interval = endTime - startTime;
            if(bestLap=="" || interval < bestLapSpan)
            {
                bestLapSpan = interval;
                string minutes = bestLapSpan.Minutes.ToString();
                if (minutes.Length == 1)
                    minutes = "0" + minutes;
                string seconds = bestLapSpan.Seconds.ToString();
                if (seconds.Length == 1)
                    seconds = "0" + seconds;
                string milliseconds = (bestLapSpan.Milliseconds / 100).ToString();
                if (milliseconds.Length == 1)
                    milliseconds = "0" + milliseconds;
                bestLap = "Best lap: "+ minutes + ":" + seconds + ":" + milliseconds;
            }
            startTime = endTime;
        }
        if (CurrentLap > PolePositionManager.maxLaps)
        {
            //transform.gameObject.SetActive(false);
            Finish = true;
        }

        Debug.Log(CurrentLap);
    }

    private void UpdateBestLapUI(string oldVal, string newVal)
    {
        uiManager.UpdateBestLap(this);

    }

    public void UpdateLapUI(int oldVal, int newVal)
    {
        uiManager.UpdateLap(this);
    }

    #endregion updateLap

    public override string ToString() { return PlayerName; }
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