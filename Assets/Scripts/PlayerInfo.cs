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
    [SerializeField] CustomNetworkManager networkManager;
    private UIManager uiManager;
    private PolePositionManager polePosition;
    private CustomChat chat;
    private CameraController mainCamera;
    public CircuitProgress CircuitProgress { get; set; }
    private Rigidbody rb;
    //private SetupPlayer setupPlayer;
    [SerializeField] MeshRenderer carMeshRenderer;

    #endregion

    #region NetVariables

    [SyncVar] public int ID;
    [SyncVar] public string PlayerName;
    [SyncVar(hook = nameof(ChangeCarColor))] [SerializeField] Color PlayerColor;
    [SyncVar(hook = nameof(FinishCircuit))] public bool Finish;
    [HideInInspector] [SyncVar(hook = nameof(UpdateLapUI))] public int CurrentLap = 0;
    [HideInInspector] [SyncVar(hook = nameof(ChangeSpeedUI))] public float Speed = 0;
    [HideInInspector] [SyncVar(hook = nameof(UpdateBestLapUI))] public string bestLap = "";
    [SyncVar(hook = nameof(SetWrongDir))] bool wrongDir = false;
    [SyncVar] public int uiPlayerIndex;
    [SyncVar] public bool ready = false;

    [SyncVar(hook = nameof(OnPermissionGranted))] public bool permissionGranted = false;

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
    public Vector3 currentSegDir = Vector3.forward;
    [SerializeField] float wrongDirAngle = 90f;
    [SerializeField] float wrongDirMinSpeed = 1.0f;
    [HideInInspector] public float arcLength;

    ///[SerializeField] private float checkDirInterval = 0.5f;
    //[SerializeField] private float wrongDirThreshold = 1.0f;

    [Header("Color")]
    [SerializeField] Color[] colors;
    [SerializeField] Material[] materials;
    Dictionary<string, Material> colorOptions;

    [Header("CHAT")]
    

    //BEST LAP
    private DateTime startTime;
    private TimeSpan bestLapSpan;


    private Renderer[] renderers;
    private Collider[] colliders;
    private Light[] lights;

    #endregion

    #region AwakeStartUpdate

    /*
     * Se crean las entradas del diccionario que se usarán para saber qué material se le debe asignar al coche
     * en función del color del botón pulsado
     */
    private void Awake()
    {
        GetRefs();
        CurrentLap = 0;
    }

    void GetRefs()
    {
        if (!networkManager) networkManager = FindObjectOfType<CustomNetworkManager>();
        if (!mainCamera) mainCamera = Camera.main.GetComponent<CameraController>();
        if (!chat) chat = FindObjectOfType<CustomChat>();
        if (!polePosition)polePosition = FindObjectOfType<PolePositionManager>();
        if (!uiManager) uiManager = FindObjectOfType<UIManager>();
        if (!rb) rb = GetComponent<Rigidbody>();
        if (CircuitProgress == null) CircuitProgress = new CircuitProgress();
        if (colorOptions == null)
        {
            colorOptions = new Dictionary<string, Material>();
            for (int i = 0; i < colors.Length; i++) colorOptions.Add(ColorUtility.ToHtmlStringRGB(colors[i]), materials[i]);
        }
        if (renderers == null || renderers.Length == 0) renderers = GetComponentsInChildren<Renderer>();
        if (colliders == null || colliders.Length == 0) colliders = GetComponentsInChildren<Collider>();
        if (lights == null || lights.Length == 0) lights = GetComponentsInChildren<Light>();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        //Por defecto el host está listo para empezar
        if (isServer) ready = true;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        //Por defecto el color del jugador es blanco
        PlayerColor = Color.white;
    }

    [Client]
    private void Update()
    {
        if(isLocalPlayer && polePosition.inGame)
        {
            mustSave = mustSave || Input.GetButtonDown("R");
            if (Input.GetButtonDown("Escape"))
            {
                networkManager.StopClient();
                if (isServer) networkManager.StopServer();
            }
        }
            
        CheckWrongDir();

        CheckSendChat();
    }

    #endregion

    #region CameraReset

    [ClientRpc]
    public void RpcResetCam()
    {
        if (isLocalPlayer)
        {
            //Debug.Log("reset camera");
            if (!mainCamera) mainCamera = Camera.main.GetComponent<CameraController>();
            mainCamera.strictBehindFlag = true;
            /*StopCoroutine("ResetCamCoroutine");
            StartCoroutine("ResetCamCoroutine");*/

        }
    }

    /*IEnumerator ResetCamCoroutine()
    {
        //yield return new WaitForSeconds(networkManager.serverTickRate * 2f + 0.05f);
        mainCamera.strictBehindFlag = true;
    }*/

    #endregion

    #region Chat

    void CheckSendChat()
    {
        if (Input.GetKeyDown(KeyCode.Return) && isLocalPlayer)
        {
            string message = uiManager.roomHUD.chatInput.text;
            if(message != "") CmdSendMessage(message);
            uiManager.roomHUD.chatInput.text = "";
        }
    }

    [Command]
    public void CmdSendMessage(string message)
    {
        //CAMBIAR COLOR
        //PONER NOMBRE
        //message = "\n" + message;
        string color = ColorUtility.ToHtmlStringRGBA(PlayerColor);

        message =$"<color=#{color}> {PlayerName}: </color> {message}"+"\n";

        chat.chatHistory += message;
    }

    #endregion

    #region PermissionGranted

    /*
     * En caso de ser localPlayer, comienzan las corrutinas para actualizar el input y la comprobación de dirección contraria
     */
    [Client]
    void OnPermissionGranted(bool ov, bool nv)
    {
        if (nv && isLocalPlayer)
        {
            Debug.Log("PERMISSION GRANTED");
            GetRefs();

            mainCamera.SetFocus(this.gameObject);

            StopCoroutine("UpdateInputCoroutine");
            StartCoroutine("UpdateInputCoroutine");
            //StartCoroutine("WrongDirCoroutine");

            uiManager.roomHUD.SetColorButtonsFunctions(this);
            uiManager.roomHUD.SetReadyButtonsFunctions(this);
            uiManager.ActivateRoomHUD();
        }
    }

    #endregion

    #region uiUpdate

    /*
     * Actualiza la información del jugador en pantalla en la sala de espera
     */
    public void UpdateRoomUI()
    {
        GetRefs();
        uiManager.roomHUD.uiPlayers[uiPlayerIndex].textSlot.gameObject.SetActive(true);
        uiManager.roomHUD.uiPlayers[uiPlayerIndex].textSlot.text = PlayerName;
        uiManager.roomHUD.uiPlayers[uiPlayerIndex].SetReady(ready, PlayerColor);
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
        uiManager.gameHUD.UpdateSpeed(this);
    }

    #endregion

    #region finishFuncs

    public void Activate(bool active)
    {
        Debug.Log("Set active: " + active);
        foreach (Renderer r in renderers) r.enabled = active;
        foreach (Light l in lights) l.enabled = active;
        foreach (Collider c in colliders)
        {
            c.gameObject.layer = active ? LayerMask.NameToLayer("Default") :
                LayerMask.NameToLayer("Finish");
        }

        if (active && (isLocalPlayer || isServer))
        {
            Speed = 0f;
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
            if (!isServer)
            {
                CmdSetReady(false);
            }
            else
            {
                CurrentLap = 0;
                CircuitProgress.Initialize();
                bestLap = "--:--:--";
            }
        } else if(isLocalPlayer || isServer)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }
        
    }

    /*
     * Cuando un jugador termina la partida, se desactiva su coche para no ser visible ni controlable para ningún cliente
     * En caso de ser localPlayer, se activa el ranking
     */
    private void FinishCircuit(bool oldVal, bool newVal)
    {
        if (newVal && polePosition.inGame) {
            Debug.Log("Circuito terminado");
            Activate(false);
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
        while (true)
        {
            axisVertical = Input.GetAxis("Vertical");
            axisHorizontal = Input.GetAxis("Horizontal");
            axisBrake = Input.GetAxis("Jump");
            CmdUpdateInput(
                axisVertical,
                axisHorizontal,
                axisBrake,
                mustSave);
            mustSave = false;
            yield return new WaitForSeconds(updateInputInterval);
        }
    }

    #endregion inputUpdate

    #region wrongDirection

    [Server]
    private void CheckWrongDir()
    {
        
        bool rbCondition = Vector3.Angle(
            Vector3.ProjectOnPlane(rb.velocity, Vector3.up).normalized,
            currentSegDir) >
            wrongDirAngle &&  rb.velocity.magnitude > wrongDirMinSpeed;

        wrongDir = rbCondition;
    }

    private void SetWrongDir(bool oldV, bool newV)
    {
        if(oldV != newV && isLocalPlayer)
        {
            uiManager.gameHUD.backwardsText.gameObject.SetActive(newV);
        }
    }

    /*
     * Cada cierto tiempo se comprueba si el jugador va en dirección contraria para avisarle por la interfaz
     */
    /*IEnumerator WrongDirCoroutine()
    {
        while (!Finish)
        {
            float localLastArcLength = LastArcLength;
            yield return new WaitForSeconds(checkDirInterval);
            float difference = LastArcLength - localLastArcLength;
            bool wrongDir = difference < -wrongDirThreshold;
            uiManager.gameHUD.backwardsText.gameObject.SetActive(wrongDir && Speed > 1.0f);
        }
    }*/

    #endregion

    #region updateLap

    /*
     * Actualiza el número de vuelta
     * Reinicia los contadores para los tiempos de vuelta
     * En caso de llegar al máximo de vueltas pone Finish a true para avisar de que ha terminado
     */
    [Server]
    public void AddLap()
    {
        CurrentLap++;
        if (CurrentLap == 1)
        {
            Debug.Log("iniciando contador en vuelta 1");
            startTime = DateTime.Now;
            bestLapSpan = new TimeSpan();
        }
        else
        {
            DateTime endTime = DateTime.Now;
            TimeSpan interval = endTime - startTime;
            if(bestLap=="" || bestLap == "--:--:--" || interval < bestLapSpan)
            {
                bestLapSpan = interval;
                ComputeBestTime();
            }
            startTime = endTime;
        }

        if (!polePosition.clasificationLap && CurrentLap > polePosition.maxLaps)
        {
            Finish = true;
            Debug.Log("ha terminado!");
        }
        else if (polePosition.clasificationLap && CurrentLap == 2)
        {
            Speed = 0f;
            polePosition.PlayerFinishedClasificationLap(this);
            CircuitProgress.Initialize();
        }
        
    }

    /*
     * Convierte a una string formateada el valor de la mejor vuelta
     */
    [Server]
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

    [Client]
    private void UpdateBestLapUI(string oldVal, string newVal)
    {
        uiManager.gameHUD.UpdateBestLap(this);
    }

    [Client]
    private void UpdateLapUI(int oldVal, int newVal)
    {
        uiManager.gameHUD.UpdateLap(this);
    }

    #endregion updateLap

    #region clasificationLap
    [ClientRpc]
    public void RpcActivateWaitingUI()
    {
        if (isLocalPlayer)
            uiManager.gameHUD.HideWaitingText(false);
    }

    public void SetTransparency(bool transparent)
    {
        //float transparency = transparent ? 0.3f : 1f;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (var mesh in renderers)
        {
            Material[] mats = mesh.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = transparent ? MaterialReferences.toTransparentMaterials[mats[i].name] : 
                    MaterialReferences.toOpaqueMaterials[mats[i].name];
            }

            mesh.materials = mats;
        }

        if (transparent)
        {
            foreach (Light l in lights) l.enabled = false;
        } else
        {
            foreach (Light l in lights) l.enabled = true;
        }

    }

    #endregion

    public override string ToString() { return PlayerName; }
}

public class PlayerInfoComparer : Comparer<PlayerInfo>
{
    public override int Compare(PlayerInfo x, PlayerInfo y)
    {
        if (x.arcLength < y.arcLength)
            return 1;
        else return -1;

    }
}