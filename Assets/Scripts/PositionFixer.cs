using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;

public class PositionFixer : NetworkBehaviour
{
    [SerializeField] float lerpAmount = 10f;
    [SerializeField] float transformSyncInterval = 0.1f;
    [SerializeField] float fixPosTime = 0.05f;
    float timeCont = 0f;
    private bool fixing = true;
    Rigidbody rb;

    //Vector3 lastPos;
    //Quaternion lastRot;
    Vector3 actualPos = Vector3.zero, actualVel = Vector3.zero, actualAcc = Vector3.zero;
    Quaternion actualRot = Quaternion.identity;
    double lastPackage = 0f;
    //DateTime instant = DateTime.Now;

    [SerializeField] float maxDistance = 2.0f;

    private void Start()
    {
        //GET REFERENCES
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (isLocalPlayer && !isServer)
        {
            GetComponent<NetworkTransform>().enabled = false;
        } else if(!isServer)
        {
            this.enabled = false;
        }

        if (isServer)
        {
            actualPos = transform.position;
            actualRot = transform.rotation;
            actualVel = rb.velocity;
            StartCoroutine("IntervalCoroutine");
        }
        
    }

    [Server]
    private void Update()
    {
        actualPos = transform.position;
        actualRot = transform.rotation;
        actualAcc = (rb.velocity - actualVel) / Time.deltaTime;
        actualVel = rb.velocity;
    }

    private void LateUpdate()
    {
        if (!isServer)
        {
            FixPos();
        }
    }

    void FixPos()
    {
        if (!isLocalPlayer) return;

        if (fixing)
        {
            timeCont += Time.deltaTime;
            Vector3 predictPos = actualPos + actualVel * timeCont + 0.5f * actualAcc * Mathf.Pow(timeCont, 2f);

            float distance = (predictPos - transform.position).magnitude;
            if(distance > maxDistance)
            {
                Debug.Log("too far, fixing");
                transform.position = predictPos;
                transform.rotation = actualRot;
                fixing = false;
            } else
            {
                transform.position = Vector3.Lerp(transform.position, predictPos, Time.deltaTime * lerpAmount);
                transform.rotation = Quaternion.Lerp(transform.rotation, actualRot, Time.deltaTime * lerpAmount);
                fixing = timeCont >= fixPosTime ? false : true;
            }
            
        }

        //float timeDif = (float)(DateTime.Now - instant).TotalSeconds;
        //
        //Vector3 predictPos = actualPos + actualVel * timeDif + 0.5f * actualAcc * Mathf.Pow(timeDif, 2f);
        //
        //float distance = (actualPos - transform.position).magnitude;
        //if(distance > maxDistance)
        //{
        //    Debug.Log("resetPos");
        //    transform.position = /*predictPos*/actualPos;
        //    transform.rotation = actualRot;
        //}

    }

    IEnumerator IntervalCoroutine()
    {
        while (true)
        {
            lastPackage = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            RpcUpdateTransform(actualPos, actualRot, actualVel, actualAcc, lastPackage);
            yield return new WaitForSeconds(transformSyncInterval);
        }
    }

    [ClientRpc]
    void RpcUpdateTransform(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 acc, double packageTime)
    {

        if (isServer/* || !isLocalPlayer*/) return;
        if (packageTime < lastPackage)
        {
            Debug.Log("discard package");
            return; //el paquete es antiguo, queda descartado
        }

        //Debug.Log("updating transform info");

        lastPackage = packageTime;
        actualPos = pos;
        actualRot = rot;
        actualVel = vel;

        timeCont = 0f;

        //lastPos = transform.position;
        //lastRot = transform.rotation;

        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.velocity = actualVel;
        //rb.angularVelocity = angVel;
        //transform.position = actualPos;
        //transform.rotation = actualRot;

        actualAcc = acc;
        //instant = DateTime.Now;
        fixing = true;
    }
    /*IEnumerator StopFixingCoroutine()
    {
        yield return new WaitForSeconds(fixPosTime);
        fixing = false;
    }*/
}
