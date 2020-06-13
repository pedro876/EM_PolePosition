﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuHUD : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private CustomNetworkManager m_NetworkManager;
    [SerializeField] private Button buttonHost;
    [SerializeField] private Button buttonClient;
    [SerializeField] private InputField inputFieldIP;
    [SerializeField] GameObject tempField;
    [SerializeField] Text tempFieldText;
    [SerializeField] GameObject tryConnectField;
    [SerializeField] Text tryConnectText;
    [SerializeField] float temporalMessageTime = 4.0f;

    public void SetTemporalMessage(string message = "", float time = 3.0f)
    {
        if (message != "")
        {
            temporalMessageTime = time;
            Debug.Log("Temp message: " + message);
            StopCoroutine("TemporalMessageCoroutine");
            tempField.SetActive(true);
            tempFieldText.text = message;
            StartCoroutine("TemporalMessageCoroutine");
        }
    }

    IEnumerator TemporalMessageCoroutine()
    {
        yield return new WaitForSeconds(temporalMessageTime);
        tempField.SetActive(false);
        tempFieldText.text = "";
    }

    /*
     * Tratará de conectar a localhost por defecto, si el inputField de la IP no está vacío lo utilizará en lugar de localhost
     */
    public void UpdateNetworkAdress()
    {
        if (inputFieldIP.text != "")
            m_NetworkManager.networkAddress = inputFieldIP.text;
    }

    public void HideNetworkButtons(bool hide = true)
    {
        Debug.Log("Hide: " + hide);
        tryConnectField.SetActive(hide);
        buttonHost.gameObject.SetActive(!hide);
        buttonClient.gameObject.SetActive(!hide);
        inputFieldIP.gameObject.SetActive(!hide);
        //tryConnectText.text = "Trying to connect...";
    }

    private void OnEnable()
    {
        HideNetworkButtons(false);
    }
}
