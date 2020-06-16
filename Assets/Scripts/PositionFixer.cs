﻿using System.Collections;
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

    Vector3 actualPos = Vector3.zero, actualVel = Vector3.zero, actualAcc = Vector3.zero;
    Quaternion actualRot = Quaternion.identity;
    double lastPackage = 0f;

    [SerializeField] float maxDistance = 2.0f;

    /*
     * Desactivará el network transform en caso de ser cliente y localPlayer,
     * en otro caso, si no es servidor se desactivará a si mismo
     */
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

    /*
     * Ajustará la posición del cliente respecto a la del servidor
     */
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
    }

    /*
     * Enviará periódicamente la información de posición y velocidad al cliente
     */
    IEnumerator IntervalCoroutine()
    {
        while (true)
        {
            lastPackage = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            RpcUpdateTransform(actualPos, actualRot, actualVel, actualAcc, lastPackage);
            yield return new WaitForSeconds(transformSyncInterval);
        }
    }

    /*
     * Actualiza las variables que utilizará el cliente para ajustar su posición respecto a la del servidor
     */
    [ClientRpc]
    void RpcUpdateTransform(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 acc, double packageTime)
    {

        if (isServer) return;
        if (packageTime < lastPackage)
        {
            Debug.Log("discard package");
            return; //el paquete es antiguo, queda descartado
        }

        lastPackage = packageTime;
        actualPos = pos;
        actualRot = rot;
        actualVel = vel;

        timeCont = 0f;

        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.velocity = actualVel;

        actualAcc = acc;
        fixing = true;
    }
}
