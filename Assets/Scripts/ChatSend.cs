using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ChatSend : MonoBehaviour
{

    [SerializeField] UnityEvent onEnterDown;
    
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            onEnterDown.Invoke();
        }
    }
}
