using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string nombreEscenaShooter = "shooter";


    public void IrAlShooter()
    {
        GameManager.Instance.hiddenScore = DialogueManager.HiddenScore;

        Debug.Log("Entrando al Shooter con " + GameManager.Instance.hiddenScore);

        SceneManager.LoadScene(nombreEscenaShooter);
    }
}