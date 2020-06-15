using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class PositionPredictor : NetworkBehaviour
{
    public enum PredictionType { velocity, longInterpolation, directToPos}

    #region References

    private Rigidbody rb;

    #endregion

    #region Vars
    [SerializeField] PredictionType predictionType = PredictionType.velocity;
    [SerializeField] float velocityMultiplier = 1.0f;
    [SerializeField] Vector3 compVelMult = Vector3.one;
    [SerializeField] float syncPosInterval = 0.1f;
    [SerializeField] float lerpAmountVelocity = 10f;
    [SerializeField] float lerpDistanceHelper = 10f;
    private long lastInstantServer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;

    private DateTime lastPackageTime;
    Vector3 actualPosition;
    Quaternion actualRotation;
    Vector3 lastPosition;
    Vector3 lastDir;
    Vector3 actualVelocity = Vector3.zero;
    Vector3 actualAcceleration = Vector3.zero;

    #endregion

    private void Awake()
    {

        rb = GetComponent<Rigidbody>();
    }

    [Server]
    private void Start()
    {
        StartCoroutine("SyncIntervalCoroutine");
    }

    [Client]
    private void LateUpdate()
    {
        if (!isServer)
        {
            UpdatePosition();
        }
        else
        {
            actualAcceleration = (rb.velocity - actualVelocity) / Time.deltaTime;
            actualVelocity = rb.velocity;
        }
    }


    [Client]
    void UpdatePosition()
    {
        //lastVelocity = lastVelocity + lastAcceleration * Time.deltaTime;
        switch (predictionType)
        {
            case PredictionType.velocity:
                VelocityPrediction();
                break;
            case PredictionType.longInterpolation:
                LongInterpolationPrediction();
                break;
            case PredictionType.directToPos:
                DirectToPosition();
                break;
            default:
                VelocityPrediction();
                break;
        }
        
    }

    void DirectToPosition()
    {
        transform.position = actualPosition;
        transform.rotation = actualRotation;
    }

    void LongInterpolationPrediction()
    {
        //Vector3 newPos = transform.position + lastDir * Time.deltaTime / syncInterval;
        //transform.position = newPos;
        transform.position = Vector3.Lerp(transform.position, actualPosition, lerpAmountVelocity * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, actualRotation, lerpAmountVelocity * Time.deltaTime);
        //pos = lastPosition + |dir| * deltaTime/syncInterval

        /*float distance = (lastPosition - transform.position).magnitude;
        transform.position = 
            Vector3.Lerp(transform.position, lastPosition, 
            distance * lerpDistanceHelper);
        transform.rotation = 
            Quaternion.Lerp(transform.rotation, lastRotation, 
            distance * lerpDistanceHelper);*/
    }

    void VelocityPrediction()
    {
        float timeDifSeconds = (float)(DateTime.Now - lastPackageTime).TotalSeconds;
        Debug.Log(timeDifSeconds);
        Vector3 newPos = actualPosition + actualVelocity * timeDifSeconds + 0.5f * actualAcceleration * Mathf.Pow(timeDifSeconds, 2f);
        newPos = Vector3.Lerp(transform.position, newPos, lerpAmountVelocity * Time.deltaTime);
        transform.position = newPos;
        Quaternion newRot = Quaternion.Lerp(transform.rotation, actualRotation, lerpAmountVelocity * Time.deltaTime);
        transform.rotation = newRot;
    }

    IEnumerator SyncIntervalCoroutine()
    {
        while (true)
        {
            SendInfo();
            yield return new WaitForSeconds(syncPosInterval);
        }
    }

    [Server]
    private void SendInfo()
    {
        actualPosition = transform.position;
        actualRotation = transform.rotation;
        lastInstantServer = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        RpcReceiveInfo(actualPosition, actualRotation, actualVelocity, actualAcceleration, lastInstantServer);
    }

    [ClientRpc]
    private void RpcReceiveInfo(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 acc, long instant)
    {
        if (instant < lastInstantServer)
        {
            Debug.Log("discard package");
            return;
        }

        lastInstantServer = instant;
        lastPackageTime = DateTime.Now;

        actualPosition = pos;
        actualRotation = rot;
        actualVelocity = new Vector3(vel.x * compVelMult.x, vel.y * compVelMult.y, vel.z * compVelMult.z) * velocityMultiplier;
        actualAcceleration = acc;
        lastDir = actualPosition - transform.position;
        lastPosition = transform.position;
    }
}
