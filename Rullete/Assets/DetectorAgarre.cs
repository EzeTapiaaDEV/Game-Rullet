using UnityEngine;

public class DetectorAgarre : MonoBehaviour
{
    private ControladorJugadorMovil controlador;
    public bool esperandoAgarre = false;

    void Awake()
    {
        controlador = FindObjectOfType<ControladorJugadorMovil>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Solo agarra si el controlador activó la bandera y tocó el objeto con el tag "Mano"
        if (esperandoAgarre && other.CompareTag("Mano"))
        {
            esperandoAgarre = false;
            if (controlador != null)
            {
                controlador.EquiparRevolverEnMano();
            }
        }
    }
}