
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogoFinal : MonoBehaviour
{
    [Header("Interfaz")]
    public GameObject panelFinal;

    [Header("Escenas")]
    public string escenaShooter = "shooter";
    public string escenaMenu = "Menu";

    void Awake()
    {
        // El menú comienza oculto.
        if (panelFinal != null)
            panelFinal.SetActive(false);
    }

    // Se ejecuta cuando termina el DialogueManager.
    public void MostrarMenuFinal()
    {
        if (panelFinal != null)
            panelFinal.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("Se mostró el menú final.");
    }

    // Reiniciar el shooter conservando los puntos.
    public void ReintentarShooter()
    {
        Time.timeScale = 1f;

        Debug.Log(
            "Reintentando Shooter con " +
            DialogueManager.HiddenScore + " puntos."
        );

        SceneManager.LoadScene(escenaShooter);
    }

    // Regresar al menú principal.
    public void IrAlMenuPrincipal()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(escenaMenu);
    }
}
