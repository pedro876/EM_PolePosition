﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameHUD : MonoBehaviour
{
    [SerializeField] private PolePositionManager polePosition;

    [Header("In-Game HUD")]
    [SerializeField] public Text textSpeed;
    [SerializeField] public Text textLaps;
    [SerializeField] public Text textPosition;
    [SerializeField] public Text countdownText;
    [SerializeField] public Text backwardsText;
    [SerializeField] public Text bestLapText;
    [SerializeField] public Text waitingText;

    /*
     * Activa o desactiva los elementos del GameHUD no relevantes en la vuelta de clasificación
     */
    public void HideLapsAndRaceOrder(bool hide)
    {
        textLaps.transform.parent.gameObject.SetActive(!hide);
        textPosition.transform.parent.gameObject.SetActive(!hide);
        bestLapText.transform.parent.gameObject.SetActive(!hide);
        if (!hide) Reset();
    }

    public void HideWaitingText(bool hide)
    {
        waitingText.gameObject.SetActive(!hide);
    }

    public void UpdateSpeed(PlayerInfo player)
    {
        if (player.isLocalPlayer)
            textSpeed.text = "Speed " + (int)(player.Speed * 5f) + " Km/h";
    }

    public void UpdatePosition(string text)
    {
        textPosition.text = text;
    }
    
    public void UpdateLap(PlayerInfo player)
    {
        if (player.isLocalPlayer)
        {
            if(player.CurrentLap == 0)
            {
                textLaps.text = "LAP: -/-";
            }
            else
            {
                textLaps.text = "LAP: " + player.CurrentLap + "/" + polePosition.maxLaps;
            }
            
        }
            
    }

    public void UpdateBestLap(PlayerInfo player)
    {
        if (player.isLocalPlayer)
            bestLapText.text = "Best lap: " + player.bestLap;
    }

    /*
     * Se encarga de actualizar la información del countdown en pantalla, en caso de no estar activo el countdown indicará el número de jugadores actual respecto al total
     */
    public void UpdateCountdownText(int secondsLeft)
    {
        if (countdownText != null && countdownText.gameObject.activeSelf)
        {
            if (secondsLeft == 0)
            {
                countdownText.text = "GO!";
                StartCoroutine("RemoveCountdown");
            }
            else if(secondsLeft == 4)
            {
                countdownText.text = "CLASIFICATION LAP";
            }
            else
                countdownText.text = secondsLeft.ToString();
        }
    }

    /*
     * Quita el mensaje GO! de la interfaz a los dos segundos de aparecer
     */
    IEnumerator RemoveCountdown()
    {
        yield return new WaitForSeconds(2);
        countdownText.gameObject.SetActive(false);
    }

    public void Reset()
    {
        backwardsText.gameObject.SetActive(false);
        StopCoroutine("RemoveCountdown");
        countdownText.gameObject.SetActive(true);
        textLaps.text = "LAP: -/-";
        textSpeed.text = "Speed 0 Km/h";
        bestLapText.text = "Best lap: --:--:--";
        waitingText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        Reset();
    }

    private void OnDisable()
    {
        Reset();
    }
}
