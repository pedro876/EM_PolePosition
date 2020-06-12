using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using System;

/*
 * Se encarga de mantener actualizados los datos y el estado del jugador en el cliente y en el servidor
 */
public class PlayerInfo : NetworkBehaviour
{

    #region references

    private UIManager uiManager;
    public CircuitProgress CircuitProgress { get; set; }
    private Rigidbody rb;
    [SerializeField] MeshRenderer carMeshRenderer;

    #endregion

    #region NetVariables

    [SyncVar] public string PlayerName;
    [SyncVar(hook = nameof(ChangeCarColor))] [SerializeField] Color PlayerColor;
    [SyncVar] public int ID;
    [SyncVar(hook = nameof(FinishCircuit))] public bool Finish;
    [HideInInspector] [SyncVar(hook = nameof(UpdateLapUI))] public int CurrentLap;
    [HideInInspector] [SyncVar(hook = nameof(ChangeSpeedUI))] public float Speed;
    [HideInInspector] [SyncVar(hook = nameof(UpdateBestLapUI))] public string bestLap;
    [HideInInspector] [SyncVar] public float LastArcLength;
    [SyncVar] public int uiPlayerIndex;
    [SyncVar] public bool ready = false;

    #endregion

    #region localVariables

    //INPUT
    [Header("Input Variables")]
    [HideInInspector] public float axisVertical = 0f;
    [HideInInspector] public float axisHorizontal = 0f;
    [HideInInspector] public float axisBrake = 0f;
    [HideInInspector] public bool mustSave = false;
    private float updateInputInterval = 0.1f;

    [Header("Wrong direction Variables")]
    [SerializeField] private float checkDirInterval = 0.5f;
    [SerializeField] private float wrongDirThreshold = 1.0f;

    [Header("Color")]
    [SerializeField] Color[] colors;
    [SerializeField] Material[] materials;
    Dictionary<string, Material> colorOptions;

    //BEST LAP
    private DateTime startTime;
    private TimeSpan bestLapSpan;

    #endregion

    #region AwakeStartUpdate

    /*
     * Se crean las entradas del diccionario que se usarán para saber qué material se le debe asignar al coche
     * en función del color del botón pulsado
     */
    private void Awake()
    {
        colorOptions = new Dictionary<string, Material>();
        for (int i = 0; i < colors.Length; i++) colorOptions.Add(ColorUtility.ToHtmlStringRGB(colors[i]), materials[i]);
    }

    /*
     * En caso de ser localPlayer, comienzan las corrutinas para actualizar el input y la comprobación de dirección contraria
     */
    private void Start()
    {
        if(uiManager == null) uiManager = FindObjectOfType<UIManager>();
        rb = GetComponent<Rigidbody>();
        CurrentLap = 0;
        CircuitProgress = new CircuitProgress();
        if (this.isLocalPlayer)
        {
            StartCoroutine("UpdateInputCoroutine");
            StartCoroutine("WrongDirCoroutine");

            uiManager.SetColorButtonsFunctions(this);
            uiManager.SetReadyButtonsFunctions(this);
        }
        if (isServer && isLocalPlayer)
            ready = true; //Por defecto el host está listo para empezar
        if (isServer)
            PlayerColor = Color.white; //Por defecto el color del jugador es blanco
    }

    private void Update()
    {
        if(isLocalPlayer)
            mustSave = mustSave || Input.GetKeyDown(KeyCode.R);
    }

    #endregion

    #region uiUpdate

    /*
     * Actualiza la información del jugador en pantalla en la sala de espera
     */
    public void UpdateRoomUI()
    {
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        uiManager.uiPlayers[uiPlayerIndex].textSlot.gameObject.SetActive(true);
        uiManager.uiPlayers[uiPlayerIndex].textSlot.text = PlayerName;
        uiManager.uiPlayers[uiPlayerIndex].SetReady(ready, PlayerColor);
    }

    #endregion

    #region properties

    /*
     * Envía el nombre al servidor, que una vez recibido, lo limita a un máximo de 9 caracteres
     */
    [Command]
    public void CmdChangeName(string newName)
    {
        const int maxChars = 9;
        if (newName.Length > maxChars)
        {
            string aux = "";
            for (int i = 0; i < maxChars; i++) aux += newName[i];
            newName = aux;
        }

        this.PlayerName = newName;
    }

    [Command]
    public void CmdSetReady(bool isReady)
    {
        ready = isReady;
    }

    [Command]
    public void CmdChangeColor(Color newColor)
    {
        PlayerColor = newColor;
    }

    /*
     * Cambia el color del coche cuando la propiedad PlayerColor cambia
     */
    void ChangeCarColor(Color oldColor, Color newColor)
    {
        string newCol = ColorUtility.ToHtmlStringRGB(newColor);
        Material[] mats = carMeshRenderer.materials;
        mats[1] = colorOptions[newCol];
        carMeshRenderer.materials = mats;
    }

    #endregion

    #region speedChange

    public void SetSpeed(float newVal)
    {
        if (Math.Abs(newVal - Speed) < float.Epsilon) return;
        Speed = newVal;
    }

    void ChangeSpeedUI(float oldVal, float newVal)
    {
        uiManager.UpdateSpeed(this);
    }

    #endregion

    #region finishFuncs

    /*
     * Cuando un jugador termina la partida, se desactiva su coche para no ser visible ni controlable para ningún cliente
     * En caso de ser localPlayer, se activa el ranking
     */
    private void FinishCircuit(bool oldVal, bool newVal)
    {
        if (newVal) {
            transform.gameObject.SetActive(false);
            if (isLocalPlayer)
                uiManager.ActivateRankingHUD();
        }
        //Cambiar a cámara de otro jugador en cinematico
    }

    #endregion finishFuncs

    #region inputUpdate

    /*
     * El input se actualizará cada cierto tiempo por una corrutina, una función Cmd se encargará de mantener el input actualizado en el servidor
     */
    [Command]
    private void CmdUpdateInput(float aV, float aH, float aB, bool mSave)
    {
        axisVertical = aV;
        axisHorizontal = aH;
        axisBrake = aB;
        if (!mustSave) mustSave = mSave;
    }

    IEnumerator UpdateInputCoroutine()
    {
        while (!Finish)
        {
            CmdUpdateInput(
                Input.GetAxis("Vertical"),
                Input.GetAxis("Horizontal"),
                Input.GetAxis("Jump"),
                mustSave);
            mustSave = false;
            yield return new WaitForSeconds(updateInputInterval);
        }
    }

    #endregion inputUpdate

    #region wrongDirection

    /*
     * Cada cierto tiempo se comprueba si el jugador va en dirección contraria para avisarle por la interfaz
     */
    IEnumerator WrongDirCoroutine()
    {
        while (!Finish)
        {
            float localLastArcLength = LastArcLength;
            yield return new WaitForSeconds(checkDirInterval);
            float difference = LastArcLength - localLastArcLength;
            bool wrongDir = difference < -wrongDirThreshold;
            uiManager.backwardsText.gameObject.SetActive(wrongDir && Speed > 1.0f);
        }
    }

    #endregion

    #region updateLap

    /*
     * Actualiza el número de vuelta
     * Reinicia los contadores para los tiempos de vuelta
     * En caso de llegar al máximo de vueltas pone Finish a true para avisar de que ha terminado
     */
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
                ComputeBestTime();
            }
            startTime = endTime;
        }
        if (CurrentLap > PolePositionManager.maxLaps)
            Finish = true;
    }

    /*
     * Convierte a una string formateada el valor de la mejor vuelta
     */
    void ComputeBestTime()
    {
        string minutes = bestLapSpan.Minutes.ToString();
        if (minutes.Length == 1)
            minutes = "0" + minutes;
        string seconds = bestLapSpan.Seconds.ToString();
        if (seconds.Length == 1)
            seconds = "0" + seconds;
        string milliseconds = ((int)(bestLapSpan.Milliseconds / 100.0f)).ToString();
        if (milliseconds.Length == 1)
            milliseconds = "0" + milliseconds;
        bestLap = minutes + ":" + seconds + ":" + milliseconds;
    }

    private void UpdateBestLapUI(string oldVal, string newVal)
    {
        uiManager.UpdateBestLap(this);
    }

    private void UpdateLapUI(int oldVal, int newVal)
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