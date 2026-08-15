using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControladorRival : MonoBehaviour
{
    [Header("Identificación")]
    public string nombreRival = "Jugador 2";

    [Header("Componentes y Arma Personal")]
    public Animator animatorRival;
    public GameObject revolverRival;      // El revólver que está en la mesa al lado del rival
    public Transform manoSocketRival;     // El punto/hueso de la mano del rival donde se acomoda el revólver

    [Header("Puntos de Referencia del Rival")]
    public Transform manoDerecha;
    public Transform[] slotsManoRival = new Transform[5];

    [Header("Inteligencia Artificial del Rival")]
    [Range(0f, 1f)] 
    [Tooltip("Probabilidad de que el bot decida mentir (0 = Siempre dice la verdad, 1 = Siempre miente)")]
    public float probabilidadMentira = 0f; // 0% por defecto para que sea completamente sincero si tiene cartas verdaderas

    private List<GameObject> cartasEnMano3D = new List<GameObject>();

    public void RecibirCarta3D(GameObject carta)
    {
        if (carta != null && !cartasEnMano3D.Contains(carta))
        {
            cartasEnMano3D.Add(carta);
            carta.SetActive(true);
        }
    }

    /// <summary>
    /// Selecciona inteligentemente qué cartas tirar priorizando la verdad (si tiene verdaderas, tira esas).
    /// </summary>
    public List<GameObject> TirarCartasInteligente(int cantidadDeseada, MazoManager mazo, string cartaDeclarada)
    {
        List<GameObject> cartasATirar = new List<GameObject>();
        cartasEnMano3D.RemoveAll(c => c == null);

        if (cartasEnMano3D.Count == 0) return cartasATirar;

        int cantidadReal = Mathf.Clamp(cantidadDeseada, 1, cartasEnMano3D.Count);

        // Clasificar cartas que tiene en la mano entre verdaderas (coinciden con la mesa) y mentiras
        List<GameObject> cartasVerdaderasDisponibles = new List<GameObject>();
        List<GameObject> cartasMentiraDisponibles = new List<GameObject>();

        string decla = cartaDeclarada.ToLower();

        foreach (GameObject carta in cartasEnMano3D)
        {
            if (carta == null) continue;
            string nombreCarta = carta.name.ToLower();
            bool esDeLaMesa = false;

            if (decla == "as" && (nombreCarta.Contains("as") || nombreCarta.Contains("a_") || nombreCarta.StartsWith("a"))) esDeLaMesa = true;
            else if (decla == "rey" && (nombreCarta.Contains("rey") || nombreCarta.Contains("k_") || nombreCarta.StartsWith("k"))) esDeLaMesa = true;
            else if (decla == "reina" && (nombreCarta.Contains("reina") || nombreCarta.Contains("q_") || nombreCarta.StartsWith("q"))) esDeLaMesa = true;
            
            if (nombreCarta.Contains("joker")) esDeLaMesa = true; // El Joker sirve como comodín/verdad

            if (esDeLaMesa) cartasVerdaderasDisponibles.Add(carta);
            else cartasMentiraDisponibles.Add(carta);
        }

        // Si tiene al menos una carta verdadera, forzamos al bot a tirar primero las verdaderas 
        // (a menos que el azar de la probabilidad de mentira extrema decida lo contrario, pero con probabilidadMentira = 0 siempre elegirá verdad si la tiene).
        bool botQuiereMentir = (cartasVerdaderasDisponibles.Count == 0) || (Random.value < probabilidadMentira);

        for (int i = 0; i < cantidadReal; i++)
        {
            GameObject cartaSeleccionada = null;

            // Si no quiere mentir y hay verdaderas disponibles, sacamos de las verdaderas
            if (!botQuiereMentir && cartasVerdaderasDisponibles.Count > 0)
            {
                int indexRand = Random.Range(0, cartasVerdaderasDisponibles.Count);
                cartaSeleccionada = cartasVerdaderasDisponibles[indexRand];
                cartasVerdaderasDisponibles.RemoveAt(indexRand);
            }
            // Si quiere mentir o ya no quedan verdaderas, tiramos de las mentiras (si hay)
            else if (cartasMentiraDisponibles.Count > 0)
            {
                int indexRand = Random.Range(0, cartasMentiraDisponibles.Count);
                cartaSeleccionada = cartasMentiraDisponibles[indexRand];
                cartasMentiraDisponibles.RemoveAt(indexRand);
            }
            // Como última alternativa si se acabaron las mentiras, agarra lo que quede de verdaderas
            else if (cartasVerdaderasDisponibles.Count > 0)
            {
                int indexRand = Random.Range(0, cartasVerdaderasDisponibles.Count);
                cartaSeleccionada = cartasVerdaderasDisponibles[indexRand];
                cartasVerdaderasDisponibles.RemoveAt(indexRand);
            }

            if (cartaSeleccionada != null)
            {
                cartasEnMano3D.Remove(cartaSeleccionada);
                cartasATirar.Add(cartaSeleccionada);
            }
        }

        StartCoroutine(RutinaTirarCartas(cartasATirar, mazo));
        return cartasATirar;
    }

    // Método de compatibilidad por si se llama desde otra parte sin el parámetro de la carta declarada
    public List<GameObject> TirarCartas(int cantidadDeseada, MazoManager mazo)
    {
        return TirarCartasInteligente(cantidadDeseada, mazo, "Rey");
    }

    private IEnumerator RutinaTirarCartas(List<GameObject> cartasATirar, MazoManager mazo)
    {
        if (animatorRival != null)
        {
            animatorRival.SetBool("Mirar Cartas", true);
        }

        yield return new WaitForSeconds(0.8f);

        if (animatorRival != null)
        {
            animatorRival.SetBool("Mirar Cartas", false);
            animatorRival.Update(0f);

            int indiceCapa = animatorRival.GetLayerIndex("UpperBody");
            if (indiceCapa == -1) indiceCapa = 0;

            animatorRival.Play("Tirar Cartas", indiceCapa, 0f);
        }

        yield return new WaitForSeconds(0.2f);

        foreach (GameObject carta in cartasATirar)
        {
            if (carta != null && mazo != null)
            {
                mazo.LanzarCartaRivalAMesa(carta, manoDerecha);
                carta.SetActive(false); 
            }
        }
    }

    public int CantidadCartasRestantes()
    {
        cartasEnMano3D.RemoveAll(c => c == null);
        return cartasEnMano3D.Count;
    }
}