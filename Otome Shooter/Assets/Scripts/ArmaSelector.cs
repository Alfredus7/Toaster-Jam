using UnityEngine;

public class ArmaSelector : MonoBehaviour
{
    [Tooltip("Arma 1 (se activa con ID 0 o 1)")]
    public GameObject arma1;

    [Tooltip("Arma 2 (se activa con ID 2 o 3)")]
    public GameObject arma2;

    void Start()
    {
        int id = GameManager.Instance != null
            ? GameManager.Instance.hiddenScore
            : 0;

        // Desactivo ambas por seguridad
        if (arma1 != null) arma1.SetActive(false);
        if (arma2 != null) arma2.SetActive(false);

        // 0-1 → Arma 1 | 2-3 → Arma 2
        if (id <= 1)
        {
            if (arma1 != null) arma1.SetActive(true);
        }
        else
        {
            if (arma2 != null) arma2.SetActive(true);
        }

        Debug.Log("Arma activada en shooter con ID: " + id);
    }
}