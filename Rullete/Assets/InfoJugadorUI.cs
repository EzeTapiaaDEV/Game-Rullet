using UnityEngine;
using TMPro;

public class InfoJugadorUI : MonoBehaviour
{
    [Header("Referencias de Texto UI")]
    public TextMeshProUGUI textoNombre; 

    public void ActualizarNombre(string nombre)
    {
        if (textoNombre != null)
        {
            textoNombre.text = nombre;
        }
    }
}