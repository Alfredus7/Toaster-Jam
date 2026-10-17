using System.Collections;
using TMPro;
using UnityEngine;

public static class TypewriterUtility
{
    public static IEnumerator TypeText(string text, TextMeshProUGUI dialogueText, float typingSpeed, System.Action<bool> setIsTyping, System.Action<string> setFullText = null, bool autoMode = false, System.Action autoAdvanceCallback = null)
    {
        setIsTyping?.Invoke(true);
        dialogueText.text = "";

        // Opcional: guardar el texto completo si se necesita
        setFullText?.Invoke(text);

        foreach (char c in text)
        {
            dialogueText.text += c;

            // Ajusta la pausa según el carácter
            float delay = typingSpeed;

            if (c == '…') delay = typingSpeed * 7f;  // pausa larga
            else if (c == '—' || c == '-') delay = typingSpeed * 5f; // pausa media
            else if (c == '.' || c == '!' || c == '?') delay = typingSpeed * 3f; // pausa corta
            else if (c == ',') delay = typingSpeed * 2f; // pausa muy corta

            yield return new WaitForSeconds(delay);
        }

        setIsTyping?.Invoke(false);

        // Si el modo automático está activo, ejecuta el callback
        if (autoMode && autoAdvanceCallback != null)
        {
            autoAdvanceCallback();
        }
    }
}