using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum Speaker { Girl, Boy, Narrator }

// Una opción que el jugador puede elegir.
[Serializable]
public class DialogueOption
{
    public string label;                                   // texto del botón
    [TextArea(2, 4)] public string response;               // diálogo que suelta al elegirla
    public Speaker responseSpeaker = Speaker.Boy;          // quién dice la respuesta
    [Range(0, 1)] public int points;                       // lo que suma a la variable oculta (0 o 1)
}

// Una línea del guion. Si hasChoice está activo, después del texto salen A y B.
[Serializable]
public class DialogueLine
{
    public Speaker speaker;
    [TextArea(2, 4)] public string text;
    public bool hasChoice;
    public DialogueOption optionA;
    public DialogueOption optionB;
}

public class VisualNovelManager : MonoBehaviour
{
    [Header("Personajes")]
    public Image girlImage;                                // izquierda
    public Image boyImage;                                 // derecha
    public string girlName = "Chica";
    public string boyName = "Chico";
    public Color activeColor = Color.white;                // personaje que habla
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Caja de diálogo (abajo)")]
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public float secondsPerChar = 0.03f;

    [Header("Opciones")]
    public GameObject choicesPanel;
    public Button buttonA;
    public Button buttonB;
    public TMP_Text labelA;
    public TMP_Text labelB;

    [Header("Guion")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("Al terminar (recibe el puntaje oculto)")]
    public UnityEvent<int> onFinished;

    // Variable oculta: suma los puntos de cada opción elegida.
    // Se puede leer desde cualquier script: VisualNovelManager.HiddenScore
    public static int HiddenScore { get; private set; }

    int index;
    bool isTyping;
    bool waitingChoice;
    bool showingResponse;
    bool finished;
    Coroutine typing;

    void Start()
    {
        HiddenScore = 0;
        choicesPanel.SetActive(false);

        if (lines.Count == 0)
        {
            Debug.LogWarning("VisualNovelManager: el guion está vacío. Usa 'Cargar ejemplo' o añade líneas.");
            return;
        }

        index = 0;
        ShowLine();
    }

    void Update()
    {
        if (AdvancePressed()) Advance();
    }

    // Click izquierdo o barra espaciadora (funciona con el input viejo y el nuevo).
    bool AdvancePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = UnityEngine.InputSystem.Mouse.current;
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return (mouse != null && mouse.leftButton.wasPressedThisFrame) ||
               (kb != null && kb.spaceKey.wasPressedThisFrame);
#else
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
#endif
    }

    void Advance()
    {
        if (finished || waitingChoice || lines.Count == 0) return;

        // Si el texto aún se está escribiendo, un click lo completa.
        if (isTyping)
        {
            FinishTyping();
            return;
        }

        index++;
        ShowLine();
    }

    void ShowLine()
    {
        if (index >= lines.Count)
        {
            Finish();
            return;
        }

        showingResponse = false;
        var line = lines[index];
        Say(line.speaker, line.text);
    }

    void Say(Speaker who, string text)
    {
        SetSpeaker(who);
        if (typing != null) StopCoroutine(typing);
        typing = StartCoroutine(Type(text));
    }

    IEnumerator Type(string text)
    {
        isTyping = true;
        dialogueText.text = text;
        dialogueText.maxVisibleCharacters = 0;
        dialogueText.ForceMeshUpdate();

        int total = dialogueText.textInfo.characterCount;
        var wait = new WaitForSeconds(secondsPerChar);
        for (int i = 1; i <= total; i++)
        {
            dialogueText.maxVisibleCharacters = i;
            yield return wait;
        }

        EndTyping();
    }

    void FinishTyping()
    {
        if (typing != null) StopCoroutine(typing);
        dialogueText.maxVisibleCharacters = int.MaxValue;
        EndTyping();
    }

    void EndTyping()
    {
        isTyping = false;
        typing = null;

        // Si esta línea tiene opciones (y no estamos en una respuesta), se muestran al terminar el texto.
        if (!showingResponse && lines[index].hasChoice)
            ShowChoices(lines[index]);
    }

    void ShowChoices(DialogueLine line)
    {
        waitingChoice = true;
        choicesPanel.SetActive(true);

        labelA.text = line.optionA.label;
        labelB.text = line.optionB.label;

        buttonA.onClick.RemoveAllListeners();
        buttonB.onClick.RemoveAllListeners();
        buttonA.onClick.AddListener(() => Choose(line.optionA));
        buttonB.onClick.AddListener(() => Choose(line.optionB));
    }

    void Choose(DialogueOption option)
    {
        HiddenScore += option.points;
        waitingChoice = false;
        choicesPanel.SetActive(false);

        // Sin respuesta escrita: pasa directo a la siguiente línea.
        if (string.IsNullOrEmpty(option.response))
        {
            index++;
            ShowLine();
            return;
        }

        showingResponse = true;
        Say(option.responseSpeaker, option.response);
    }

    // El personaje que habla se ve normal; el otro, oscurecido.
    void SetSpeaker(Speaker who)
    {
        girlImage.color = who == Speaker.Girl ? activeColor : inactiveColor;
        boyImage.color = who == Speaker.Boy ? activeColor : inactiveColor;

        switch (who)
        {
            case Speaker.Girl: speakerText.text = girlName; break;
            case Speaker.Boy: speakerText.text = boyName; break;
            default: speakerText.text = ""; break;
        }
    }

    void Finish()
    {
        finished = true;
        onFinished?.Invoke(HiddenScore);
    }

    // Menú del componente (⋮ → Cargar ejemplo) para probar rápido.
    [ContextMenu("Cargar ejemplo")]
    void LoadExample()
    {
        lines = new List<DialogueLine>
        {
            new DialogueLine { speaker = Speaker.Girl, text = "Hola... ¿me estabas esperando?" },
            new DialogueLine
            {
                speaker = Speaker.Boy,
                text = "Sí. ¿Quieres caminar conmigo hasta la salida?",
                hasChoice = true,
                optionA = new DialogueOption { label = "Claro, vamos.", response = "Me alegra. Vamos, entonces.", responseSpeaker = Speaker.Boy, points = 1 },
                optionB = new DialogueOption { label = "Hoy no puedo.", response = "Ah... entiendo. Será otro día.", responseSpeaker = Speaker.Boy, points = 0 }
            },
            new DialogueLine { speaker = Speaker.Girl, text = "Bueno... nos vemos luego." }
        };
    }
}
