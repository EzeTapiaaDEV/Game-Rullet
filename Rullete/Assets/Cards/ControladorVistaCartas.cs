using UnityEngine;

public class ControladorVistaCartas : MonoBehaviour
{
    private Animator animator;

    [Header("Referencia al Punto Contenedor")]
    public Transform cartasPoint; // Arrastra tu objeto 'CARTAPOINT'

    [Header("1. Rotación cuando está BOCA ABAJO en la MESA")]
    [Tooltip("Ángulo local de CARTAPOINT para apoyarse en la mesa")]
    public Vector3 rotacionEnMesa = new Vector3(90f, 0f, 0f);

    [Header("2. Rotación cuando SUBE A LA CARA")]
    [Tooltip("Ángulo local de CARTAPOINT para ver las caras de frente")]
    public Vector3 rotacionAlMirar = new Vector3(0f, 0f, 0f);

    [Header("Ajuste Suave")]
    public float velocidadTransicion = 10f;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (cartasPoint == null || animator == null) return;

        // Lee el parámetro corregido
        bool mirando = animator.GetBool("MirandoCartas");

        // Elige la rotación según la animación activa
        Vector3 anguloObjetivo = mirando ? rotacionAlMirar : rotacionEnMesa;
        Quaternion rotObjetivo = Quaternion.Euler(anguloObjetivo);

        // Aplica la rotación suavemente
        cartasPoint.localRotation = Quaternion.Slerp(cartasPoint.localRotation, rotObjetivo, Time.deltaTime * velocidadTransicion);
    }
}