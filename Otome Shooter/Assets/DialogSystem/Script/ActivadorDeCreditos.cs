
using UnityEngine;

public class ActivadorCreditos : MonoBehaviour
{
    [Header("Panel de créditos")]
    public GameObject panelCreditos;

    void Awake()
    {
        if (panelCreditos != null)
            panelCreditos.SetActive(false);
    }

    public void MostrarCreditos()
    {
        if (panelCreditos != null)
            panelCreditos.SetActive(true);
    }

    public void OcultarCreditos()
    {
        if (panelCreditos != null)
            panelCreditos.SetActive(false);
    }
}
