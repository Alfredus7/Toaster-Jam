
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string nombreEscenaShooter = "shooter";

    public void IrAlShooter()
    {
        // Consultar los puntos antes de cambiar de escena
        int puntos = DialogueManager.HiddenScore;

        Debug.Log(
            "Entrando al Shooter con " + puntos + " puntos."
        );

        // Cargar la siguiente escena
        SceneManager.LoadScene(nombreEscenaShooter);
    }
}
