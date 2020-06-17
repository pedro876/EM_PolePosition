using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingHUD : MonoBehaviour
{
    [Header("Ranking HUD")]
    [SerializeField] private Text[] playerTexts;
    [SerializeField] private Text[] timeTexts;
    [SerializeField] private Button roomButton;
    private int textIndex = 0;

    /*private void Awake()
    {
        foreach (Text t in playerTexts) t.gameObject.SetActive(false);
    }*/

    public void ActivateRoomHostOptions(bool active)
    {
        roomButton.gameObject.SetActive(active);
    }


    public void AddPlayerToRanking(string newName, string bestTime)
    {
        if (textIndex < 4)
        {
            Debug.Log("Adding " + newName + " to ranking");

            playerTexts[textIndex].gameObject.SetActive(true);
            playerTexts[textIndex].text = newName;
            timeTexts[textIndex].gameObject.SetActive(true);
            timeTexts[textIndex].text = bestTime == "00:00:00" || bestTime == "" ? "--:--:--" : bestTime;
            textIndex++;
        }
    }

    public void Reset()
    {
        textIndex = 0;
        foreach (Text t in timeTexts)
            t.text = "--:--:--";
        foreach (Text t in playerTexts)
        {
            t.text = "";
            t.gameObject.SetActive(false);
        }
    }

    private void OnDisable()
    {
        Reset();
    }
}
