using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ControladorCartasUI : MonoBehaviour
{
    [Header("Panel Principal")]
    public GameObject panelCartasUI;

    [Header("Botón Ver Cartas")]
    public GameObject botonVerCartasObjeto;

    [Header("Las 5 Cartas Fijas en la UI")]
    public Button[] botonesCartasUI = new Button[5];

    [Header("Botón Lanzar")]
    public GameObject botonLanzarObjeto;
    public Button botonLanzarComponente;

    [Header("Feedback Visual")]
    public float elevacionSeleccion = 20f;
    public Color colorSeleccionado = new Color(0.8f, 1f, 0.8f, 1f);

    private List<Sprite> imagenesManoGuardadas = new List<Sprite>();
    private List<int> indicesSeleccionados = new List<int>();
    private Vector3[] posicionesOriginales = new Vector3[5];

    void Awake()
    {
        for (int i = 0; i < botonesCartasUI.Length; i++)
        {
            if (botonesCartasUI[i] != null)
                posicionesOriginales[i] = botonesCartasUI[i].transform.localPosition;
        }
    }

    void Start()
    {
        OcultarUI();

        for (int i = 0; i < botonesCartasUI.Length; i++)
        {
            int index = i;
            if (botonesCartasUI[i] != null)
            {
                botonesCartasUI[i].onClick.RemoveAllListeners();
                botonesCartasUI[i].onClick.AddListener(() => TocasteCartaUI(index));
            }
        }

        if (botonLanzarComponente != null)
        {
            botonLanzarComponente.onClick.RemoveAllListeners();
            botonLanzarComponente.onClick.AddListener(LanzarCartasSeleccionadas);
        }
    }

    public void GuardarManoJugador(List<Sprite> imagenesCartas)
    {
        imagenesManoGuardadas = new List<Sprite>(imagenesCartas);
        RefrescarUI();

        if (botonVerCartasObjeto != null)
        {
            botonVerCartasObjeto.SetActive(imagenesManoGuardadas.Count > 0);
        }
    }

    private void RefrescarUI()
    {
        for (int i = 0; i < botonesCartasUI.Length; i++)
        {
            if (botonesCartasUI[i] != null)
            {
                botonesCartasUI[i].transform.localPosition = posicionesOriginales[i];
                Image img = botonesCartasUI[i].GetComponent<Image>();
                if (img != null) img.color = Color.white;

                if (i < imagenesManoGuardadas.Count && imagenesManoGuardadas[i] != null)
                {
                    botonesCartasUI[i].gameObject.SetActive(true);
                    if (img != null) img.sprite = imagenesManoGuardadas[i];
                }
                else
                {
                    botonesCartasUI[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void MostrarUI()
    {
        if (imagenesManoGuardadas.Count == 0) return;

        indicesSeleccionados.Clear();
        RefrescarUI();

        if (panelCartasUI != null) panelCartasUI.SetActive(true);
        if (botonLanzarObjeto != null) botonLanzarObjeto.SetActive(true);

        ActualizarEstadoBotonLanzar();
    }

    public void OcultarUI()
    {
        if (panelCartasUI != null) panelCartasUI.SetActive(false);
        if (botonLanzarObjeto != null) botonLanzarObjeto.SetActive(false);
    }

    private void TocasteCartaUI(int indiceCarta)
    {
        Button btn = botonesCartasUI[indiceCarta];
        Image img = btn.GetComponent<Image>();

        if (indicesSeleccionados.Contains(indiceCarta))
        {
            indicesSeleccionados.Remove(indiceCarta);
            btn.transform.localPosition = posicionesOriginales[indiceCarta];
            if (img != null) img.color = Color.white;
        }
        else
        {
            indicesSeleccionados.Add(indiceCarta);
            btn.transform.localPosition = posicionesOriginales[indiceCarta] + new Vector3(0, elevacionSeleccion, 0);
            if (img != null) img.color = colorSeleccionado;
        }

        ActualizarEstadoBotonLanzar();
    }

    private void ActualizarEstadoBotonLanzar()
    {
        if (botonLanzarComponente != null)
        {
            botonLanzarComponente.interactable = indicesSeleccionados.Count > 0;
        }
    }

    public void LanzarCartasSeleccionadas()
    {
        if (indicesSeleccionados.Count == 0) return;

        List<GameObject> cartasLanzadas = new List<GameObject>();

        MazoManager mazo = FindObjectOfType<MazoManager>();
        if (mazo != null)
        {
            mazo.PrepararCartasParaLanzar(new List<int>(indicesSeleccionados));
            cartasLanzadas = mazo.EjecutarVueloCartasAMesa();
        }

        indicesSeleccionados.Sort((a, b) => b.CompareTo(a));
        foreach (int index in indicesSeleccionados)
        {
            if (index < imagenesManoGuardadas.Count)
            {
                imagenesManoGuardadas.RemoveAt(index);
            }
        }

        indicesSeleccionados.Clear();
        OcultarUI();

        if (imagenesManoGuardadas.Count == 0)
        {
            if (botonVerCartasObjeto != null)
            {
                botonVerCartasObjeto.SetActive(false);
            }
        }

        ControladorJugadorMovil jugador = FindObjectOfType<ControladorJugadorMovil>();
        if (jugador != null)
        {
            jugador.EjecutarAnimacionLanzar();
        }

        TurnManager turnos = FindObjectOfType<TurnManager>();
        if (turnos != null)
        {
            turnos.NotificarJugadorTiro(cartasLanzadas, false);
        }
    }
}