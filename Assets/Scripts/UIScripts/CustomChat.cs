using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class CustomChat : NetworkBehaviour
{
    [SyncVar(hook = nameof(UpdateChatHistory))] public string chatHistory = "";
    [SerializeField] Text chatText;
    [SerializeField] ScrollRect scrollBar;

    /*
     * Situa la scrollbar al final para mantener visibles siempre los nuevos mensajes
     */
    void UpdateScrollBar()
    {
        scrollBar.verticalScrollbar.value = 0f;
        scrollBar.verticalScrollbar.onValueChanged.RemoveAllListeners();
    }

    /*
     * Actualiza el texto del chat
     */
    void UpdateChatHistory(string ov, string nv)
    {
        chatText.text = nv;
        scrollBar.verticalScrollbar.onValueChanged.AddListener((val) => UpdateScrollBar());
    }

    public void Reset()
    {
        chatHistory = "";
        chatText.text = "";
    }
}