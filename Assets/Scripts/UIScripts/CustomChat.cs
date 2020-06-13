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

    /*private void Start()
    {
        scrollBar.onValueChanged.AddListener((val) => scrollBar.value = 0f);
    }*/

    void UpdateScrollBar()
    {
        scrollBar.verticalScrollbar.value = 0f;
        scrollBar.verticalScrollbar.onValueChanged.RemoveAllListeners();
    }

    /*IEnumerator UpdateScrollBar()
    {
        
        yield return null;
        yield return null;

        scrollBar.value = 0f;
    }*/

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