using UnityEngine;

public class MecanicaRevolver : MonoBehaviour
{
    [Header("Configuración del Tambor")]
    public int totalBalas = 6;
    public int posicionBalaMortal = 3; 
    private int recamaraActual = 0;   

    void Start()
    {
        ReiniciarTambor();
    }

    public bool ApretarGatillo()
    {
        bool disparoMortal = (recamaraActual == posicionBalaMortal);
        recamaraActual++;
        return disparoMortal;
    }

    public string ObtenerEstadoRecamara()
    {
        return recamaraActual + "/" + totalBalas;
    }

    public void ReiniciarTambor()
    {
        posicionBalaMortal = Random.Range(0, totalBalas);
        recamaraActual = 0;
    }
}