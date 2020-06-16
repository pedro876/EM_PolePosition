using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChooseNameHUD : MonoBehaviour
{
    [Header("Choose Name")]
    [SerializeField] private InputField inputFieldName;
    public string playerName = "PLAYER";

    /*
     * Al iniciar el juego, guarda el nombre introducido en la interfaz si no está vacío
     */
    private void OnDisable()
    {
        if (!inputFieldName.text.Equals(""))
        {
            playerName = inputFieldName.text.ToUpper();
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
