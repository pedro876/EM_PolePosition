﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

[Serializable]
public class UIPlayer
{
    public Text textSlot;
    public Text readySlot;
    public bool ready;

    public void SetReady(bool _ready, Color textColor)
    {
        ready = _ready;
        if (readySlot)
        {
            readySlot.gameObject.SetActive(ready);
            readySlot.color = textColor;
        }
    }
}

public class RoomHUD : MonoBehaviour
{
    [SerializeField] PolePositionManager polePosition;

    [Header("Waiting Room")]
    [SerializeField] public GameObject[] hostOptions;
    [SerializeField] public UIPlayer[] uiPlayers;
    [SerializeField] private Button[] colorButtons;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button notReadyButton;
    [SerializeField] private Button startButton;

    private void Awake()
    {
        //foreach (GameObject option in hostOptions) option.SetActive(false);
        foreach (UIPlayer uiP in uiPlayers)
        {
            uiP.SetReady(false, Color.white);
            uiP.textSlot.gameObject.SetActive(false);
        }
    }

    public void ActivateRoomHostOptions(bool active)
    {
        foreach (GameObject option in hostOptions) option.SetActive(active);
    }

    /*
     * Añade funcionalidad a los botones de la interfaz
     */
    public void SetColorButtonsFunctions(PlayerInfo localPlayer)
    {
        foreach (Button colorButton in colorButtons)
        {
            colorButton.onClick.RemoveAllListeners();
            colorButton.onClick.AddListener(() => localPlayer.CmdChangeColor(colorButton.colors.normalColor));
        }
    }

    /*
     * Añade funcionalidad a los botones de ready, not ready y start
     */
    public void SetReadyButtonsFunctions(PlayerInfo localPlayer)
    {
        readyButton.onClick.RemoveAllListeners();
        notReadyButton.onClick.RemoveAllListeners();
        startButton.onClick.RemoveAllListeners();

        if (!polePosition) polePosition = FindObjectOfType<PolePositionManager>();
        if (localPlayer.isServer)
        {
            readyButton.gameObject.SetActive(false);
            notReadyButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(true);
            startButton.onClick.AddListener(() => polePosition.StartGame());

        }
        else
        {
            readyButton.gameObject.SetActive(!localPlayer.ready);
            notReadyButton.gameObject.SetActive(localPlayer.ready);
            startButton.gameObject.SetActive(false);
            readyButton.onClick.AddListener(() => localPlayer.CmdSetReady(true));
            notReadyButton.onClick.AddListener(() => localPlayer.CmdSetReady(false));
        }
    }

    /*
     * Añade un slot en la lista de jugadores de la sala de espera para mostrar su información
     */
    public void AddPlayerToRoomUI(PlayerInfo player, List<PlayerInfo> players)
    {
        player.uiPlayerIndex = players.Count - 1;
    }

    /*
     * Reasigna el índice de slot para mostrar información para cada jugador
     */
    public void ReAssignUIPlayers(List<PlayerInfo> players, int maxPlayers)
    {
        for (int i = 0; i < players.Count; i++)
            players[i].uiPlayerIndex = i;
        RemoveLostPlayersFromUI(players, maxPlayers);
    }

    /*
     * Desactiva los slots de información que sobran, ya que es posible que un jugador salga de la partida
     */
    public void RemoveLostPlayersFromUI(List<PlayerInfo> players, int maxPlayers)
    {
        for (int j = players.Count; j < maxPlayers; j++)
        {
            uiPlayers[j].SetReady(false, Color.black);
            if (uiPlayers[j].textSlot) uiPlayers[j].textSlot.gameObject.SetActive(false);
        }
    }
}