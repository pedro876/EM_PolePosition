using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Actualiza la posición de la cámara en función del target m_Focus
 */

public class CameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PolePositionManager polePosition;
    [SerializeField] private bool cinematicModeActive = false;
    [SerializeField] public GameObject m_Focus;
    [SerializeField] public CircuitController m_Circuit;

    private Vector3 originalPosition = new Vector3(0f, 2.82f, -68f);
    private Quaternion originalRotation = Quaternion.identity;

    [Header("Cinematic mode")]
    [SerializeField] public Vector3 m_offset = new Vector3(10, 10, 10);
    [SerializeField] private float m_Distance = 10;
    [SerializeField] private float m_Elevation = 8;
    [Range(0, 1)] [SerializeField] private float m_Following = 0.5f;
    private Vector3 m_Direction = Vector3.zero;

    [Header("Play mode")]
    [SerializeField] int currentOption = 0;
    [SerializeField] float lerpMultiplier = 20f;
    [SerializeField] float lerpStrictBehindMultiplier = 30f;
    [SerializeField] float strictBehindTime = 1.0f;
    [SerializeField] float[] distanceOptions;
    [SerializeField] float[] elevationOptions;
    public bool strictBehindFlag = true;
    private bool settingBehind = false;
    

    private void Awake()
    {
        GetRefs();
        originalPosition = transform.position;
        originalRotation = transform.rotation;
    }

    void GetRefs()
    {
        if (!polePosition) FindObjectOfType<PolePositionManager>();
        if (!mainCamera) GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("V") && polePosition.inGame)
        {
            currentOption = ++currentOption % distanceOptions.Length;
        }
    }

    void FixedUpdate()
    {
        if (m_Focus != null)
        {
            if (cinematicModeActive)
                CinematicMode();
            else PlayMode();
        }
    }

    public void SetFocus(GameObject focus)
    {
        m_Focus = focus;
        if (m_Focus)
        {
            if (!cinematicModeActive)
                transform.position = ComputePosition(true);
        } else Reset();
    }

    public void Reset()
    {
        Debug.Log("cam reset");
        m_Focus = null;
        transform.position = originalPosition;
        transform.rotation = originalRotation;
    }

    #region playMode

    /*
     * Modo de vista de cámara que permite una visión trasera del coche
     */
    void PlayMode()
    {
        if (polePosition)
        {
            float localLerp = lerpMultiplier;
            //if (strictBehindFlag) Debug.Log("strict behing");
            Vector3 newPos = ComputePosition(!polePosition.inGame || settingBehind);
            Quaternion newRot = Quaternion.LookRotation(m_Focus.transform.position - transform.position, Vector3.up);
            if (strictBehindFlag)
            {
                settingBehind = true;
                strictBehindFlag = false;
                StopCoroutine("StrictBehindCoroutine");
                StartCoroutine("StrictBehindCoroutine");
            }
            if(settingBehind) localLerp = lerpStrictBehindMultiplier;
            transform.position = Vector3.Lerp(transform.position, newPos, Time.fixedDeltaTime * localLerp);
            transform.position = new Vector3(
                transform.position.x, 
                m_Focus.transform.position.y + elevationOptions[currentOption], 
                transform.position.z);
            transform.rotation = Quaternion.Lerp(transform.rotation, newRot, Time.fixedDeltaTime * localLerp);
            //strictBehindFlag = false;
        }
    }

    IEnumerator StrictBehindCoroutine()
    {
        yield return new WaitForSeconds(strictBehindTime);
        settingBehind = false;
        Debug.Log("setting behind to false");
    }

    /*
     * Esta función calcula una posición para la cámara que se mantenga dentro de un rango con una elevación constante respecto al jugador
     */
    Vector3 ComputePosition(bool strictBehind = false)
    {
        

        Vector3 from = transform.position;
        Vector3 to = m_Focus.transform.position;
        if (!strictBehind)
        {
            Vector3 direction = to - from;
            if (direction.magnitude > distanceOptions[currentOption])
                from = to - direction.normalized * distanceOptions[currentOption];
        }
        else
            from = to - m_Focus.transform.forward * distanceOptions[currentOption];
        
        //from.y = m_Focus.transform.position.y + elevationOptions[currentOption];
        return from;
    }

    #endregion

    #region cinematicMode

    /*
     * Este modo de cámara proporciona una vista aérea al jugador
     */
    void CinematicMode()
    {
        if (this.m_Circuit != null)
        {
            if (this.m_Direction.magnitude == 0)
                this.m_Direction = new Vector3(0f, -1f, 0f);

            int segIdx;
            float carDist;
            Vector3 carProj;

            m_Circuit.ComputeClosestPointArcLength(m_Focus.transform.position, out segIdx, out carProj, out carDist);

            Vector3 pathDir = -m_Circuit.GetSegment(segIdx);
            pathDir = new Vector3(pathDir.x, 0f, pathDir.z);
            pathDir.Normalize();

            this.m_Direction = Vector3.Lerp(this.m_Direction, pathDir, this.m_Following * Time.deltaTime);
            Vector3 offset = this.m_Direction * this.m_Distance;
            offset = new Vector3(offset.x, m_Elevation, offset.z);

            mainCamera.transform.position = m_Focus.transform.position + offset;
            mainCamera.transform.LookAt(m_Focus.transform.position);
        }
        else
        {
            mainCamera.transform.position = m_Focus.transform.position + m_offset;
            mainCamera.transform.LookAt(m_Focus.transform.position);
        }
    }

    #endregion
}