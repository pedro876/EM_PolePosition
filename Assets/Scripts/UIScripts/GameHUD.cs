using System.Collections;
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
            textLaps.text = "LAP: " + player.CurrentLap + "/" + polePosition.maxLaps;
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
            else
                countdownText.text = secondsLeft.ToString();
        }
    }

    /*
     * Esta corrutina se encarga de quitar el mensaje GO! de la interfaz a los dos segundos de aparecer
     */
    IEnumerator RemoveCountdown()
    {
        yield return new WaitForSeconds(2);
        countdownText.gameObject.SetActive(false);
    }
}
