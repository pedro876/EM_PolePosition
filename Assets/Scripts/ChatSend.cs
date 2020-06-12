using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ChatSend : MonoBehaviour
{

    [SerializeField] UnityEvent onEnterDown;
    
    /*
     * Invoca un evento que mandará un mensaje por el chat
     */
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            onEnterDown.Invoke();
        }
    }
}
