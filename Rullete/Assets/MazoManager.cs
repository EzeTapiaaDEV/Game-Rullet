using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazoManager : MonoBehaviour
{
    [Header("Prefabs de Cartas (3D)")]
    public GameObject prefabRey;
    public GameObject prefabReina;
    public GameObject prefabAs;
    public GameObject prefabJoker;

    [Header("Sprites de Cartas (2D para la UI)")]
    public Sprite spriteRey;
    public Sprite spriteReina;
    public Sprite spriteAs;
    public Sprite spriteJoker;

    [Header("Puntos de Referencia")]
    public Transform centroMesa;
    public Transform puntoManoDerecha;

    [Header("Puntos de Revelación (Opcionales para cartas en mesa)")]
    public Transform[] puntosRevelacionCartas;

    [Header("Slots del Abanico del Jugador Local")]
    public Transform[] slotsJugador = new Transform[5]; 

    [Header("Configuración de Reparto")]
    public float alturaApilado = 0.003f;
    public float velocidadReparto = 0.35f;

    [Header("Rotación Boca Abajo en Mesa")]
    public Vector3 rotacionBocaAbajo = new Vector3(0f, 0f, 180f);

    private List<GameObject> prefabsMazo = new List<GameObject>();
    private List<GameObject> cartasEnMesa = new List<GameObject>();
    private List<GameObject> cartasEnManoJugador3D = new List<GameObject>();
    private List<Sprite> spritesEnManoJugador = new List<Sprite>();
    private List<int> indicesPendientesLanzar = new List<int>();

    void Start()
    {
        IniciarNuevaRonda();
    }

    [ContextMenu("Iniciar Nueva Ronda")]
    public void IniciarNuevaRonda()
    {
        LimpiarMesa();
        CrearMazoLogico();
        BarajarMazo();
        CrearMazoFisicoEnMesa();
        StartCoroutine(RepartirCartasATodos());
    }

    public void LimpiarMesa()
    {
        foreach (GameObject carta in cartasEnMesa) if (carta != null) Destroy(carta);
        foreach (GameObject carta3D in cartasEnManoJugador3D) if (carta3D != null) Destroy(carta3D);

        cartasEnMesa.Clear();
        prefabsMazo.Clear();
        spritesEnManoJugador.Clear();
        cartasEnManoJugador3D.Clear();

        ControladorCartasUI controladorUI = FindObjectOfType<ControladorCartasUI>();
        if (controladorUI != null) controladorUI.GuardarManoJugador(new List<Sprite>());
    }

    public void LimpiarCartasReveladasDeMesa(List<GameObject> cartasReveladas)
    {
        if (cartasReveladas != null)
        {
            foreach (GameObject carta in cartasReveladas)
            {
                if (carta != null)
                {
                    if (cartasEnMesa.Contains(carta)) cartasEnMesa.Remove(carta);
                    Destroy(carta);
                }
            }
        }
    }

    void CrearMazoLogico()
    {
        for (int i = 0; i < 6; i++)
        {
            prefabsMazo.Add(prefabRey);
            prefabsMazo.Add(prefabReina);
            prefabsMazo.Add(prefabAs);
        }
        prefabsMazo.Add(prefabJoker);
        prefabsMazo.Add(prefabJoker);
    }

    void BarajarMazo()
    {
        for (int i = 0; i < prefabsMazo.Count; i++)
        {
            GameObject temp = prefabsMazo[i];
            int randomIndex = Random.Range(i, prefabsMazo.Count);
            prefabsMazo[i] = prefabsMazo[randomIndex];
            prefabsMazo[randomIndex] = temp;
        }
    }

    void CrearMazoFisicoEnMesa()
    {
        Vector3 posBase = centroMesa != null ? centroMesa.position : Vector3.zero;
        Quaternion rotBocaAbajo = Quaternion.Euler(rotacionBocaAbajo);

        for (int i = 0; i < prefabsMazo.Count; i++)
        {
            Vector3 posCarta = posBase + new Vector3(0, i * alturaApilado, 0);
            GameObject nuevaCarta = Instantiate(prefabsMazo[i], posCarta, rotBocaAbajo);
            if (centroMesa != null) nuevaCarta.transform.SetParent(centroMesa);
            cartasEnMesa.Add(nuevaCarta);
        }
    }

    IEnumerator RepartirCartasATodos()
    {
        yield return new WaitForSeconds(0.3f);

        for (int i = 0; i < slotsJugador.Length; i++)
        {
            if (cartasEnMesa.Count == 0) break;
            if (slotsJugador[i] == null) continue;

            int ultimoIndice = cartasEnMesa.Count - 1;
            GameObject carta = cartasEnMesa[ultimoIndice];
            cartasEnMesa.RemoveAt(ultimoIndice);

            cartasEnManoJugador3D.Add(carta);

            Sprite spriteCorrespondiente = ObtenerSpriteDePrefab(carta);
            if (spriteCorrespondiente != null) spritesEnManoJugador.Add(spriteCorrespondiente);

            // --- SONIDO AL REPARTIR CARTA AL JUGADOR ---
            if (AudioManager.instancia != null) AudioManager.instancia.ReproducirSonido(AudioManager.instancia.clipTirarCartas);

            StartCoroutine(MoverCartaASlot(carta, slotsJugador[i]));
            yield return new WaitForSeconds(0.1f);
        }

        ControladorRival[] rivales = FindObjectsOfType<ControladorRival>();
        foreach (ControladorRival rival in rivales)
        {
            if (rival == null) continue;

            for (int i = 0; i < rival.slotsManoRival.Length; i++)
            {
                if (cartasEnMesa.Count == 0) break;
                if (rival.slotsManoRival[i] == null) continue;

                int ultimoIndice = cartasEnMesa.Count - 1;
                GameObject carta = cartasEnMesa[ultimoIndice];
                cartasEnMesa.RemoveAt(ultimoIndice);

                rival.RecibirCarta3D(carta);

                // --- SONIDO AL REPARTIR CARTA A UN RIVAL ---
                if (AudioManager.instancia != null) AudioManager.instancia.ReproducirSonido(AudioManager.instancia.clipTirarCartas);

                StartCoroutine(MoverCartaASlot(carta, rival.slotsManoRival[i]));
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitForSeconds(velocidadReparto);

        ControladorCartasUI controladorUI = FindObjectOfType<ControladorCartasUI>();
        if (controladorUI != null) controladorUI.GuardarManoJugador(spritesEnManoJugador);
    }

    IEnumerator MoverCartaASlot(GameObject carta, Transform slotDestino)
    {
        Vector3 posInicial = carta.transform.position;
        Quaternion rotInicial = carta.transform.rotation;
        float tiempo = 0;

        carta.transform.SetParent(null, true);

        while (tiempo < velocidadReparto)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / velocidadReparto;
            carta.transform.position = Vector3.Lerp(posInicial, slotDestino.position, t);
            carta.transform.rotation = Quaternion.Slerp(rotInicial, slotDestino.rotation, t);
            yield return null;
        }

        carta.transform.SetParent(slotDestino, true);
        carta.transform.localPosition = Vector3.zero;
        carta.transform.localRotation = Quaternion.identity;
    }

    Sprite ObtenerSpriteDePrefab(GameObject cartaObj)
    {
        string nombre = cartaObj.name;
        if (nombre.Contains(prefabRey.name)) return spriteRey;
        if (nombre.Contains(prefabReina.name)) return spriteReina;
        if (nombre.Contains(prefabAs.name)) return spriteAs;
        if (nombre.Contains(prefabJoker.name)) return spriteJoker;
        return null;
    }

    public void PrepararCartasParaLanzar(List<int> indices)
    {
        indicesPendientesLanzar = new List<int>(indices);
    }

    public List<GameObject> EjecutarVueloCartasAMesa()
    {
        List<GameObject> cartasLanzadas3D = new List<GameObject>();

        if (indicesPendientesLanzar == null || indicesPendientesLanzar.Count == 0) 
            return cartasLanzadas3D;

        indicesPendientesLanzar.Sort((a, b) => b.CompareTo(a));

        foreach (int index in indicesPendientesLanzar)
        {
            if (index < cartasEnManoJugador3D.Count && cartasEnManoJugador3D[index] != null)
            {
                GameObject carta3D = cartasEnManoJugador3D[index];
                cartasEnManoJugador3D.RemoveAt(index);
                cartasLanzadas3D.Add(carta3D);

                if (index < spritesEnManoJugador.Count) spritesEnManoJugador.RemoveAt(index);

                StartCoroutine(VolarCartaHaciaMesa(carta3D));
            }
        }

        indicesPendientesLanzar.Clear();
        return cartasLanzadas3D;
    }

    IEnumerator VolarCartaHaciaMesa(GameObject carta)
    {
        carta.transform.SetParent(null, true);
        Vector3 posOrigen = puntoManoDerecha != null ? puntoManoDerecha.position : carta.transform.position;

        int cantidadEnPila = cartasEnMesa.Count;
        Vector3 posDestinoBase = centroMesa != null ? centroMesa.position : Vector3.zero;

        Vector3 posDestino = posDestinoBase + new Vector3(
            Random.Range(-0.02f, 0.02f),
            cantidadEnPila * alturaApilado,
            Random.Range(-0.02f, 0.02f)
        );

        cartasEnMesa.Add(carta);

        float rotacionAleatoriaY = Random.Range(-15f, 15f);
        Quaternion rotFinalBocaAbajo = Quaternion.Euler(rotacionBocaAbajo.x, rotacionBocaAbajo.y + rotacionAleatoriaY, rotacionBocaAbajo.z);
        Quaternion rotInicial = carta.transform.rotation;

        float t = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            float factor = t / 0.28f;
            carta.transform.position = Vector3.Lerp(posOrigen, posDestino, factor);
            carta.transform.rotation = Quaternion.Slerp(rotInicial, rotFinalBocaAbajo, factor);
            yield return null;
        }

        if (centroMesa != null) carta.transform.SetParent(centroMesa, true);
    }

    public void LanzarCartaRivalAMesa(GameObject carta, Transform puntoManoRival)
    {
        StartCoroutine(VolarCartaRivalHaciaMesa(carta, puntoManoRival));
    }

    private IEnumerator VolarCartaRivalHaciaMesa(GameObject carta, Transform puntoManoRival)
    {
        carta.transform.SetParent(null, true);
        Vector3 posOrigen = puntoManoRival != null ? puntoManoRival.position : carta.transform.position;

        int cantidadEnPila = cartasEnMesa.Count;
        Vector3 posDestinoBase = centroMesa != null ? centroMesa.position : Vector3.zero;

        Vector3 posDestino = posDestinoBase + new Vector3(
            Random.Range(-0.02f, 0.02f),
            cantidadEnPila * alturaApilado,
            Random.Range(-0.02f, 0.02f)
        );

        cartasEnMesa.Add(carta);

        float rotacionAleatoriaY = Random.Range(-15f, 15f);
        Quaternion rotFinalBocaAbajo = Quaternion.Euler(rotacionBocaAbajo.x, rotacionBocaAbajo.y + rotacionAleatoriaY, rotacionBocaAbajo.z);
        Quaternion rotInicial = carta.transform.rotation;

        float t = 0f;
        while (t < 0.28f)
        {
            t += Time.deltaTime;
            float factor = t / 0.28f;
            carta.transform.position = Vector3.Lerp(posOrigen, posDestino, factor);
            carta.transform.rotation = Quaternion.Slerp(rotInicial, rotFinalBocaAbajo, factor);
            yield return null;
        }

        if (centroMesa != null) carta.transform.SetParent(centroMesa, true);
    }

    public void RevelarUltimasCartasConDetalle(List<GameObject> cartasARevelar, List<bool> esMentiraIndividual)
    {
        StartCoroutine(RutinaAnimarRevelacionConDetalle(cartasARevelar, esMentiraIndividual));
    }

    private IEnumerator RutinaAnimarRevelacionConDetalle(List<GameObject> cartasARevelar, List<bool> esMentiraIndividual)
    {
        if (cartasARevelar == null || cartasARevelar.Count == 0) yield break;

        List<GameObject> cartasValidas = new List<GameObject>();
        foreach (var c in cartasARevelar)
        {
            if (c != null) cartasValidas.Add(c);
        }

        int cantidad = cartasValidas.Count;
        if (cantidad == 0) yield break;

        for (int i = 0; i < cantidad; i++)
        {
            GameObject carta = cartasValidas[i];
            Vector3 posDestino;
            Quaternion rotBocaArriba;

            if (puntosRevelacionCartas != null && i < puntosRevelacionCartas.Length && puntosRevelacionCartas[i] != null)
            {
                posDestino = puntosRevelacionCartas[i].position;
                rotBocaArriba = puntosRevelacionCartas[i].rotation;
            }
            else
            {
                float espaciadoX = 0.22f;
                float anchoTotal = (cantidad - 1) * espaciadoX;
                Vector3 centroFila = centroMesa != null ? centroMesa.position : Vector3.zero;

                posDestino = centroFila + new Vector3(
                    (i * espaciadoX) - (anchoTotal / 2f),
                    0.02f + (i * 0.002f),
                    0f
                );
                rotBocaArriba = Quaternion.Euler(0f, centroMesa != null ? centroMesa.eulerAngles.y : 0f, 0f);
            }

            StartCoroutine(AnimarMoverYDarVuelta(carta, posDestino, rotBocaArriba));
        }

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < cartasValidas.Count; i++)
        {
            if (cartasValidas[i] != null)
            {
                bool mentiraEstaCarta = (i < esMentiraIndividual.Count) ? esMentiraIndividual[i] : true;
                Color colorPintura = mentiraEstaCarta ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(0.25f, 1f, 0.25f, 1f);
                PintarCarta(cartasValidas[i], colorPintura);
            }
        }

        yield return new WaitForSeconds(0.6f);
    }

    private IEnumerator AnimarMoverYDarVuelta(GameObject carta, Vector3 posDestino, Quaternion rotDestino)
    {
        carta.transform.SetParent(null, true);
        Vector3 posInicial = carta.transform.position;
        Quaternion rotInicial = carta.transform.rotation;
        float t = 0;

        while (t < 0.45f)
        {
            t += Time.deltaTime;
            float factor = t / 0.45f;
            float elevacion = Mathf.Sin(factor * Mathf.PI) * 0.12f;

            carta.transform.position = Vector3.Lerp(posInicial, posDestino, factor) + new Vector3(0, elevacion, 0);
            carta.transform.rotation = Quaternion.Slerp(rotInicial, rotDestino, factor);
            yield return null;
        }

        carta.transform.position = posDestino;
        carta.transform.rotation = rotDestino;
    }

    private void PintarCarta(GameObject carta, Color nuevoColor)
    {
        Renderer[] renderers = carta.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            foreach (Material mat in rend.materials)
            {
                mat.color = nuevoColor;
            }
        }
    }

    public void LimpiarManoDeJugador(int indiceJugador)
    {
        if (indiceJugador == 0)
        {
            foreach (GameObject carta in cartasEnManoJugador3D)
            {
                if (carta != null) Destroy(carta);
            }
            cartasEnManoJugador3D.Clear();
            spritesEnManoJugador.Clear();

            if (slotsJugador != null)
            {
                foreach (Transform slot in slotsJugador)
                {
                    if (slot != null)
                    {
                        for (int i = slot.childCount - 1; i >= 0; i--)
                        {
                            Destroy(slot.GetChild(i).gameObject);
                        }
                    }
                }
            }

            ControladorCartasUI controladorUI = FindObjectOfType<ControladorCartasUI>();
            if (controladorUI != null) controladorUI.GuardarManoJugador(new List<Sprite>());
        }
        else
        {
            ControladorRival[] rivales = FindObjectsOfType<ControladorRival>();
            int rivalIndexReal = indiceJugador - 1;

            if (rivalIndexReal >= 0 && rivalIndexReal < rivales.Length)
            {
                ControladorRival rival = rivales[rivalIndexReal];
                if (rival != null && rival.slotsManoRival != null)
                {
                    foreach (Transform slot in rival.slotsManoRival)
                    {
                        if (slot != null)
                        {
                            for (int i = slot.childCount - 1; i >= 0; i--)
                            {
                                Destroy(slot.GetChild(i).gameObject);
                            }
                        }
                    }
                }
            }
        }
    }

    public int ObtenerCantidadCartasJugadorLocal()
    {
        if (cartasEnManoJugador3D == null) return 0;
        return cartasEnManoJugador3D.Count;
    }
}