using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Cargar escena por índice (número en Build Settings)
    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneIndex);
    }

    // Cargar escena por nombre (debe coincidir exactamente con el nombre en Build Settings)
    public void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1.0f;
        SceneManager.LoadScene(sceneName);
    }

    // Salir del juego (funciona en build, no en editor)
    public void QuitGame()
    {

        Application.Quit();
        Debug.Log("El juego se ha cerrado."); // Solo visible en el Editor
    }
    // En MenuConfigUI, agrega este métdo
    public void ResetProgress()
    {
        LevelProgressManager.Instance.ResetLevelProgress();
        Debug.Log("Progreso reiniciado");
    }

    // Reiniciar la escena actual
    public void ReloadScene()
    {
        Time.timeScale = 1.0f;
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}
