using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Events;

public class DialogueManager : MonoBehaviour
{
    [Header("Referencias UI")]
    public Image characterImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Configuración")]
    public DialogueSequence currentDialogue;
    public float typingSpeed = 0.03f;
    public float autoDelay = 1f;

    [Header("Eventos")]
    public UnityEvent OnDialogueEnd;

    private int currentIndex = 0;
    private bool isActive = false;
    private bool isTyping = false;
    private bool autoMode = false;
    private string fullText;
    private Coroutine typingCoroutine;
    private Coroutine autoCoroutine;
    private Material defaultMaterial;

    void Start()
    {
        if (characterImage != null)
            defaultMaterial = characterImage.material;

        if (currentDialogue != null)
        {
            StartDialogue(currentDialogue);
        }
        else
        {
            Debug.LogWarning("No hay diálogo asignado al iniciar.");
        }
    }

    public void StartDialogue(DialogueSequence dialogue)
    {
        currentDialogue = dialogue;
        currentIndex = 0;
        isActive = true;
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentDialogue == null || currentDialogue.lines.Length == 0)
        {
            Debug.LogWarning("No hay diálogo asignado.");
            return;
        }

        var line = currentDialogue.lines[currentIndex];

        // Actualiza retrato y nombre
        if (line.character != null)
        {
            characterImage.sprite = line.character.portrait;
            nameText.text = line.character.characterName;
            characterImage.material = line.character.Material != null ? line.character.Material : defaultMaterial;
        }
        else
        {
            nameText.text = "";
            characterImage.sprite = null;
            characterImage.material = defaultMaterial;
        }

        // Usar la utilidad de typewriter
        fullText = line.text;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypewriterUtility.TypeText(
            fullText,
            dialogueText,
            typingSpeed,
            (typing) => isTyping = typing,
            null, // No necesitamos setFullText ya que lo manejamos localmente
            autoMode,
            () => {
                if (autoMode && !isTyping)
                    autoCoroutine = StartCoroutine(AutoAdvance());
            }
        ));
    }

    IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoDelay);
        NextLine();
    }

    public void OnJump(InputValue value)
    {
        if (!isActive) return;
        AdvanceLine();
    }

    public void AdvanceLine()
    {
        if (!isActive) return;

        if (isTyping)
        {
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            dialogueText.text = fullText;
            isTyping = false;
        }
        else
        {
            NextLine();
        }
    }

    public void SkipDialogue()
    {
        if (!isActive) return;

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (autoCoroutine != null) StopCoroutine(autoCoroutine);

        EndDialogue();
    }

    public void ToggleAuto()
    {
        autoMode = !autoMode;
        Debug.Log("Modo automático: " + (autoMode ? "Activado" : "Desactivado"));

        if (autoMode && !isTyping)
        {
            if (autoCoroutine != null) StopCoroutine(autoCoroutine);
            autoCoroutine = StartCoroutine(AutoAdvance());
        }
        else if (!autoMode && autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
        }
    }

    public void NextLine()
    {
        currentIndex++;

        if (currentIndex < currentDialogue.lines.Length)
        {
            ShowCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        isActive = false;
        autoMode = false;
        Debug.Log("Fin del diálogo");
        OnDialogueEnd?.Invoke();
        this.gameObject.SetActive(false);
    }
}