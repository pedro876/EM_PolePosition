using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;

public class CustomChat : NetworkBehaviour
{
    [SyncVar(hook = nameof(UpdateChatHistory))] public string chatHistory = "";
    [SerializeField] Text chatText;
    [SerializeField] Scrollbar scrollBar;

    private void Start()
    {
        scrollBar.onValueChanged.AddListener((val) => scrollBar.value = 0f);
    }

    void UpdateChatHistory(string ov, string nv)
    {
        chatText.text = nv;
    }

    public void Reset()
    {
        chatHistory = "";
        chatText.text = "";
    }
}