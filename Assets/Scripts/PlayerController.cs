using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/*
	Documentation: https://mirror-networking.com/docs/Guides/NetworkBehaviour.html
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/


/*
 * Clase utilizada unicamente por el Servidor.
 * No recibirá los inputs sino que se le pasarán desde PlayerInfo y se aplicarán al jugador correspondiente.
 * */

public class PlayerController : NetworkBehaviour
{
    #region Vars

    [Header("Movement")] public List<AxleInfo> axleInfos;
    public float forwardMotorTorque = 100000;
    public float backwardMotorTorque = 50000;
    public float maxSteeringAngle = 15;
    public float engineBrake = 1e+12f;
    public float footBrake = 1e+24f;
    public float topSpeed = 200f;
    public float downForce = 1000f;
    public float slipLimit = 0.2f;
    private float CurrentRotation { get; set; }

    private PlayerInfo m_PlayerInfo;
    private Rigidbody m_Rigidbody;
    private float m_SteerHelper = 0.8f;

    [Header("Save Player Options")]
    [SerializeField] CircuitController circuitController;
    [SerializeField] float saveTime = 1.0f;
    [SerializeField] float speedThreshold = 2.0f;
    [SerializeField] float angleRange = 30f;
    bool coroutineCalled = false;

    #endregion

    #region Unity Callbacks
    public void Awake()
    {
        GetRefs();
    }

    void GetRefs()
    {
        if(m_Rigidbody == null)m_Rigidbody = GetComponent<Rigidbody>();
        if(m_PlayerInfo == null) m_PlayerInfo = GetComponent<PlayerInfo>();
        if(!circuitController) circuitController = FindObjectOfType<CircuitController>();
    }

    public void Update()
    {
        if (!(isLocalPlayer || isServer)) return;

        m_PlayerInfo.SetSpeed(m_Rigidbody.velocity.magnitude);
        CheckMustSave();
    }

    #endregion

    #region savePlayerMethods
    [Server]
    private void CheckMustSave()
    {
        if (m_PlayerInfo.mustSave)
        {
            SavePlayer();
            m_PlayerInfo.mustSave = false;
        }
        else if (!coroutineCalled && m_PlayerInfo.Speed < speedThreshold)
        {
            if (Vector3.Angle(-transform.up, Vector3.up) < angleRange)
            {
                StartCoroutine("SavePlayerDelay");
                coroutineCalled = true;
            }
        }
    }

    IEnumerator SavePlayerDelay()
    {
        yield return new WaitForSeconds(saveTime);
        if (Vector3.Angle(-transform.up, Vector3.up) < angleRange)
        {
            SavePlayer();
        }
        coroutineCalled = false;
    }

    [Server]
    void SavePlayer()
    {
        int segId;
        Vector3 posProj;
        float dist;
        float arcLen = circuitController.ComputeClosestPointArcLength(transform.position, out segId, out posProj, out dist);
        transform.position = posProj;
        Vector3 dir = circuitController.GetSegment(segId);
        transform.forward = dir;
        m_Rigidbody.velocity = Vector3.zero;
        m_Rigidbody.angularVelocity = Vector3.zero;
        m_PlayerInfo.SetSpeed(0);
        m_PlayerInfo.RpcResetCam();
    }



    #endregion savePlayerMethods

    #region Movement

    public void FixedUpdate()
    {
        if (!(isLocalPlayer || isServer)) return;

        float InputSteering = Mathf.Clamp(m_PlayerInfo.axisHorizontal, -1, 1);
        float InputAcceleration = Mathf.Clamp(m_PlayerInfo.axisVertical, -1, 1);
        float InputBrake = Mathf.Clamp(m_PlayerInfo.axisBrake, 0, 1);

        float steering = maxSteeringAngle * InputSteering;

        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }

            if (axleInfo.motor)
            {
                if (InputAcceleration > float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = forwardMotorTorque;
                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.motorTorque = forwardMotorTorque;
                    axleInfo.rightWheel.brakeTorque = 0;
                }

                if (InputAcceleration < -float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = -backwardMotorTorque;
                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.motorTorque = -backwardMotorTorque;
                    axleInfo.rightWheel.brakeTorque = 0;
                }

                if (Math.Abs(InputAcceleration) < float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = 0;
                    axleInfo.leftWheel.brakeTorque = engineBrake;
                    axleInfo.rightWheel.motorTorque = 0;
                    axleInfo.rightWheel.brakeTorque = engineBrake;
                }

                if (InputBrake > 0)
                {
                    axleInfo.leftWheel.brakeTorque = footBrake;
                    axleInfo.rightWheel.brakeTorque = footBrake;
                }
            }

            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }

        SteerHelper();
        SpeedLimiter();
        AddDownForce();
        TractionControl();
    }

    #endregion

    #region Methods

    // crude traction control that reduces the power to wheel if the car is wheel spinning too much
    private void TractionControl()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit wheelHitLeft;
            WheelHit wheelHitRight;
            axleInfo.leftWheel.GetGroundHit(out wheelHitLeft);
            axleInfo.rightWheel.GetGroundHit(out wheelHitRight);

            if (wheelHitLeft.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitLeft.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.leftWheel.motorTorque -= axleInfo.leftWheel.motorTorque * howMuchSlip * slipLimit;
            }

            if (wheelHitRight.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitRight.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.rightWheel.motorTorque -= axleInfo.rightWheel.motorTorque * howMuchSlip * slipLimit;
            }
        }
    }

    // this is used to add more grip in relation to speed
    private void AddDownForce()
    {
        foreach (var axleInfo in axleInfos)
        {
            if(axleInfo.leftWheel.attachedRigidbody && axleInfo.rightWheel.attachedRigidbody)
            {
                axleInfo.leftWheel.attachedRigidbody.AddForce(
                    -Vector3.up * (downForce * axleInfo.leftWheel.attachedRigidbody.velocity.magnitude));
                axleInfo.rightWheel.attachedRigidbody.AddForce(
                    -Vector3.up * (downForce * axleInfo.rightWheel.attachedRigidbody.velocity.magnitude));
            }
        }
    }

    private void SpeedLimiter()
    {
        float speed = m_Rigidbody.velocity.magnitude;
        if (speed > topSpeed)
            m_Rigidbody.velocity = topSpeed * m_Rigidbody.velocity.normalized;
    }

    // finds the corresponding visual wheel
    // correctly applies the transform
    public void ApplyLocalPositionToVisuals(WheelCollider col)
    {
        if (col.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = col.transform.GetChild(0);
        Vector3 position;
        Quaternion rotation;
        col.GetWorldPose(out position, out rotation);
        var myTransform = visualWheel.transform;
        myTransform.position = position;
        myTransform.rotation = rotation;
    }

    private void SteerHelper()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit[] wheelHit = new WheelHit[2];
            axleInfo.leftWheel.GetGroundHit(out wheelHit[0]);
            axleInfo.rightWheel.GetGroundHit(out wheelHit[1]);
            foreach (var wh in wheelHit)
            {
                if (wh.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }
        }

// this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
        if (Mathf.Abs(CurrentRotation - transform.eulerAngles.y) < 10f)
        {
            var turnAdjust = (transform.eulerAngles.y - CurrentRotation) * m_SteerHelper;
            Quaternion velRotation = Quaternion.AngleAxis(turnAdjust, Vector3.up);
            m_Rigidbody.velocity = velRotation * m_Rigidbody.velocity;
        }

        CurrentRotation = transform.eulerAngles.y;
    }

    #endregion
}