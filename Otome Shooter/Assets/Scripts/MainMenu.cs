using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string nombreEscena = "VisualNovel";

    public void IniciarJuego()
    {
        SceneManager.LoadScene(nombreEscena);
    }
}
