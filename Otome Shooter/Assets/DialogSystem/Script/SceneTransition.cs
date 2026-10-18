using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string nombreEscenaShooter = "shooter";

    public void IrAlShooter()
    {
        GameManager.Instance.hiddenScore = DialogueManager.HiddenScore;
        Debug.Log("Entrando al Shooter con " + GameManager.Instance.hiddenScore);

        // *** Asegurar que la nueva escena arranque en tiempo normal ***
        Time.timeScale = 1f;
        Cursor.visible = true;

        SceneManager.LoadScene(nombreEscenaShooter);
    }
}