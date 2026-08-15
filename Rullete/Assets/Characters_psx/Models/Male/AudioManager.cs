using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Esto permite acceder al AudioManager desde cualquier otro script fácilmente
    public static AudioManager instancia;

    [Header("Reproductor")]
    public AudioSource sourceSFX;

    [Header("Efectos de Sonido")]
    public AudioClip clipClick;
    public AudioClip clipDesconfiar;
    public AudioClip clipDisparoMortal;
    public AudioClip clipGatilloVacio;
    public AudioClip clipTirarCartas;

    void Awake()
    {
        // Configuramos el Singleton
        if (instancia == null)
        {
            instancia = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Método que llamaremos desde otros scripts para reproducir un sonido
    public void ReproducirSonido(AudioClip clip)
    {
        if (sourceSFX != null && clip != null)
        {
            // PlayOneShot permite que los sonidos se superpongan si ocurren al mismo tiempo
            sourceSFX.PlayOneShot(clip);
        }
    }
}