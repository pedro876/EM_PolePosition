using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseNameHUD : MonoBehaviour
{
    [Header("Choose Name")]
    [SerializeField] private InputField inputFieldName;
    public string playerName = "PLAYER";

    private void OnDisable()
    {
        if (!inputFieldName.text.Equals(""))
        {
            playerName = inputFieldName.text.ToUpper();
        }
    }
}
