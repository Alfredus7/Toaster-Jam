using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ChangedColor : MonoBehaviour
{
    [Header("Settings")]
    public Image targetImage;
    public Color toggleColor = Color.blue;
    public float flashTime = 0.2f;
    private Color originalColor;
    public bool isToggled = false;

    private bool isCurrentlyToggled = false; // Seguimiento interno del estado

    void Start()
    {
        if (targetImage != null)
        {
            originalColor = targetImage.color;
            isCurrentlyToggled = (targetImage.color == toggleColor);
        }
    }

    // ÚNICO método necesario - usar en el evento OnClick
    public void ChangeColor()
    {
        if (targetImage == null) return;

        if (isToggled)
        {
            // Modo Toggle
            if (isCurrentlyToggled)
            {
                targetImage.color = originalColor;
            }
            else
            {
                targetImage.color = toggleColor;
            }
            isCurrentlyToggled = !isCurrentlyToggled;
        }
        else
        {
            // Modo Flash
            StartCoroutine(FlashEffect());
        }
    }

    private IEnumerator FlashEffect()
    {
        targetImage.color = toggleColor;
        yield return new WaitForSeconds(flashTime);
        targetImage.color = originalColor;
    }
}