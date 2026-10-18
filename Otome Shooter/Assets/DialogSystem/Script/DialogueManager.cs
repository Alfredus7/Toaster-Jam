using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Events;
using UnityEngine.Serialization;

// Lado de pantalla desde el que habla alguien (se usa en las respuestas de las opciones).
public enum DialogueSide { Left, Right, Narrator }
public enum BackgroundAction
{
    Keep,
    FadeToBlack,
    FadeToNormal
}



public enum CharacterVisibility
{
    Keep,
    Hide,
    Show,
    OnlyLeft,
    OnlyRight
}


public enum DialogueMusicAction
{
    Change,
    Stop,
    RestoreOriginal
}

[System.Serializable]
public class DialogueMusicEvent
{
    [Tooltip("Línea en la que se ejecuta. 0 = primera línea.")]
    public int atLine;

    public DialogueMusicAction action =
        DialogueMusicAction.Change;

    public AudioClip newMusic;
}


// Una opción (A o B) que el jugador puede elegir.
[System.Serializable]
public class ChoiceOption
{
    public string label;                                  // texto del botón
    [TextArea(2, 4)] public string response;              // diálogo que suelta al elegirla
    public DialogueSide responseSide = DialogueSide.Right; // quién dice la respuesta
    public Sprite responsePortrait;                       // (opcional) retrato/expresión al responder
    [Range(0, 1)] public int points;                      // lo que suma a la variable oculta (0 o 1)
}

// Un momento de elección: aparece justo después de una línea del diálogo.
[System.Serializable]
public class ChoicePoint
{
    [Tooltip("Índice de la línea después de la cual salen las opciones (0 = la primera línea)")]
    public int afterLine;
    public ChoiceOption optionA;
    public ChoiceOption optionB;
}

public class DialogueManager : MonoBehaviour
{
    [Header("Referencias UI")]

    [Header("Fondo narrador")]
    public CanvasGroup fondoNegro;
    public float duracionFade = 1f;

    private Coroutine fadeFondoCoroutine;


    [FormerlySerializedAs("characterImage")] // conserva la referencia que ya tenías en la escena
    public Image leftImage;                   // personaje de la izquierda (Aliza)
    public Image rightImage;                  // personaje de la derecha (Chico)
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Personajes (deben coincidir con 'characterName' de cada personaje)")]
    public string leftCharacterName = "Aliza";
    [Tooltip("Otros nombres que hablan desde la izquierda. Se muestran en cursiva (ej. Mente = pensamientos)")]
    public string[] leftAliases = { "Mente" };
    public string rightCharacterName = "Arthur";
    public Color activeColor = Color.white;                            // el que habla
    public Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);      // el que escucha

    [Header("Entrada del personaje derecho")]
    [Tooltip("Índice de la línea en la que aparece el personaje derecho. -1 = visible desde el inicio")]
    public int rightAppearsAtLine = -1;
    public float fadeDuration = 0.5f;

    [Header("Opciones")]
    public GameObject choicesPanel;
    public Button buttonA;
    public Button buttonB;
    public TextMeshProUGUI labelA;
    public TextMeshProUGUI labelB;
    public ChoicePoint[] choicePoints;


    [Header("Eventos de música")]
    public AudioSource musicSource;
    public DialogueMusicEvent[] musicEvents;

    private AudioClip originalMusic;
    private bool originalMusicLoop;


    [Header("Configuración")]
    public DialogueSequence currentDialogue;
    public float typingSpeed = 0.03f;
    public float autoDelay = 1f;

    [Header("Eventos")]
    public UnityEvent OnDialogueEnd;

    // Variable oculta: suma los puntos de cada opción elegida.
    // Se puede leer desde cualquier escena: DialogueManager.HiddenScore
    public static int HiddenScore { get; private set; }
    void Awake()
    {
        // Si este DialogueManager es el de una partida nueva, reseteamos
        HiddenScore = 0;
    }

    private int currentIndex = 0;
    private bool isActive = false;
    private bool isTyping = false;
    private bool autoMode = false;
    private bool waitingChoice = false;   // hay opciones en pantalla
    private bool showingResponse = false; // se está mostrando la respuesta de una opción
    private bool wasTyping = false;       // para detectar cuándo termina de escribirse una línea
    private string fullText;
    private Coroutine typingCoroutine;
    private Coroutine autoCoroutine;
    private Coroutine fadeCoroutine;
    private Material defaultLeftMaterial;
    private Material defaultRightMaterial;
    private DialogueSide currentSide = DialogueSide.Narrator;
    private float rightAlpha = 1f;        // 0 = personaje derecho oculto, 1 = visible

    void Start()
    {
        //HiddenScore = 0;
        if (fondoNegro != null)
            fondoNegro.alpha = 0f;

        if (leftImage != null)
        {
            defaultLeftMaterial = leftImage.material;
            leftImage.enabled = leftImage.sprite != null; // se oculta hasta tener sprite
        }
        if (rightImage != null)
        {
            defaultRightMaterial = rightImage.material;
            rightImage.enabled = rightImage.sprite != null;
        }

        if (choicesPanel != null)
            choicesPanel.SetActive(false);

        if (currentDialogue != null)
        {
            // Guardar la música que tenía originalmente la escena.
            if (musicSource != null)
            {
                originalMusic = musicSource.clip;
                originalMusicLoop = musicSource.loop;
            }
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

        // Si el personaje derecho debe aparecer más adelante, empieza invisible
        rightAlpha = rightAppearsAtLine >= 0 ? 0f : 1f;
        ApplyColors();

        ShowCurrentLine();
    }

    void CambiarFondo(BackgroundAction action)
    {
        if (fondoNegro == null) return;

        float destino;

        switch (action)
        {
            case BackgroundAction.FadeToBlack:
                destino = 1f;
                break;

            case BackgroundAction.FadeToNormal:
                destino = 0f;
                break;

            default:
                return;
        }

        if (fadeFondoCoroutine != null)
            StopCoroutine(fadeFondoCoroutine);

        fadeFondoCoroutine = StartCoroutine(
            FadeFondo(destino)
        );
    }


    void CambiarVisibilidadPersonajes(CharacterVisibility action)
    {
        switch (action)
        {
            case CharacterVisibility.Keep:
                // No cambiar la visibilidad actual
                break;

            case CharacterVisibility.Hide:
                if (leftImage != null)
                    leftImage.gameObject.SetActive(false);

                if (rightImage != null)
                    rightImage.gameObject.SetActive(false);
                break;

            case CharacterVisibility.Show:
                if (leftImage != null)
                    leftImage.gameObject.SetActive(true);

                if (rightImage != null)
                    rightImage.gameObject.SetActive(true);
                break;

            case CharacterVisibility.OnlyLeft:
                // Mostrar solo a Aliza
                if (leftImage != null)
                    leftImage.gameObject.SetActive(true);

                if (rightImage != null)
                    rightImage.gameObject.SetActive(false);
                break;

            case CharacterVisibility.OnlyRight:
                // Mostrar solo a Arthur
                if (leftImage != null)
                    leftImage.gameObject.SetActive(false);

                if (rightImage != null)
                    rightImage.gameObject.SetActive(true);
                break;
        }
    }



    IEnumerator FadeFondo(float alphaDestino)
    {
        float alphaInicial = fondoNegro.alpha;
        float tiempo = 0f;

        if (duracionFade <= 0f)
        {
            fondoNegro.alpha = alphaDestino;
            fadeFondoCoroutine = null;
            yield break;
        }

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;

            float progreso = Mathf.Clamp01(
                tiempo / duracionFade
            );

            fondoNegro.alpha = Mathf.Lerp(
                alphaInicial,
                alphaDestino,
                progreso
            );

            yield return null;
        }

        fondoNegro.alpha = alphaDestino;
        fadeFondoCoroutine = null;
    }


    // Ejecuta los eventos musicales al comenzar una línea del diálogo.
    void CheckMusicEvents()
    {
        if (musicSource == null || musicEvents == null)
            return;

        foreach (DialogueMusicEvent evento in musicEvents)
        {
            if (evento == null || evento.atLine != currentIndex)
                continue;

            switch (evento.action)
            {
                case DialogueMusicAction.Change:
                    if (evento.newMusic == null)
                    {
                        Debug.LogWarning(
                            "No hay canción asignada en la línea " + currentIndex
                        );
                        break;
                    }

                    musicSource.Stop();
                    musicSource.clip = evento.newMusic;
                    musicSource.loop = true;
                    musicSource.Play();
                    Debug.Log("Nueva canción en línea " + currentIndex);
                    break;

                case DialogueMusicAction.Stop:
                    musicSource.Stop();
                    Debug.Log("Música detenida en línea " + currentIndex);
                    break;

                case DialogueMusicAction.RestoreOriginal:
                    musicSource.Stop();
                    musicSource.clip = originalMusic;
                    musicSource.loop = originalMusicLoop;
                    if (originalMusic != null)
                        musicSource.Play();
                    Debug.Log("Música original restaurada en línea " + currentIndex);
                    break;
            }
        }
    }

    void ShowCurrentLine()
    {
        if (currentDialogue == null || currentDialogue.lines.Length == 0)
        {
            Debug.LogWarning("No hay diálogo asignado.");
            return;
        }

        showingResponse = false;
        var line = currentDialogue.lines[currentIndex];

        // Ejecuta eventos musicales asociados a esta línea, si los hay.
        CheckMusicEvents();

        // El personaje derecho entra en la línea indicada
        if (currentIndex == rightAppearsAtLine)
            ShowRight();

        // Decide de qué lado habla según el nombre del personaje
        DialogueSide side = DialogueSide.Narrator;
        bool isThought = false;

        if (line.character != null)
        {
            string who = line.character.characterName;

            if (SameName(who, leftCharacterName)) side = DialogueSide.Left;
            else if (IsLeftAlias(who)) { side = DialogueSide.Left; isThought = true; }
            else if (SameName(who, rightCharacterName)) side = DialogueSide.Right;
            else Debug.LogWarning("El personaje '" + who + "' no coincide con los nombres de izquierda/derecha del DialogueManager.");

            ApplyPortrait(side, line.character.portrait, line.character.Material);
            nameText.text = who;
        }
        else
        {
            nameText.text = "";
        }

        // Los pensamientos (alias de la izquierda) se muestran en cursiva
        dialogueText.fontStyle = isThought ? FontStyles.Italic : FontStyles.Normal;


        SetSpeakerHighlight(side);

        CambiarFondo(line.backgroundAction);

        CambiarVisibilidadPersonajes(line.characterVisibility);

        // Por defecto utilizamos el diálogo normal.
        string textoMostrar = line.text;

        // Si esta línea depende de los puntos,
        // seleccionamos el texto correspondiente.
        if (line.useScoreDialogue)
        {
            if (HiddenScore >= line.minimumScore)
            {
                textoMostrar = line.highScoreText;
            }
            else
            {
                textoMostrar = line.lowScoreText;
            }

            Debug.Log(
                "Puntos actuales: " + HiddenScore +
                " | Mínimo requerido: " + line.minimumScore
            );
        }

        TypeLine(textoMostrar);

    }

    // Pone el retrato en el lado que corresponde; el otro personaje se queda como estaba.
    void ApplyPortrait(DialogueSide side, Sprite portrait, Material mat)
    {
        Image img = side == DialogueSide.Left ? leftImage : side == DialogueSide.Right ? rightImage : null;
        if (img == null) return;

        Material def = side == DialogueSide.Left ? defaultLeftMaterial : defaultRightMaterial;

        if (portrait != null) img.sprite = portrait;
        img.enabled = img.sprite != null;
        img.material = mat != null ? mat : def;
    }

    // El que habla se ve normal; el otro, oscurecido. Narrador: los dos oscurecidos.
    void SetSpeakerHighlight(DialogueSide side)
    {
        currentSide = side;
        ApplyColors();
    }

    // Aplica colores (resaltado + transparencia del personaje derecho)
    void ApplyColors()
    {
        if (leftImage != null)
            leftImage.color = currentSide == DialogueSide.Left ? activeColor : inactiveColor;

        if (rightImage != null)
        {
            Color c = currentSide == DialogueSide.Right ? activeColor : inactiveColor;
            c.a *= rightAlpha;
            rightImage.color = c;
        }
    }

    // ---------- Entrada del personaje derecho ----------

    void ShowRight()
    {
        if (rightAlpha >= 1f) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        if (fadeDuration <= 0f)
        {
            rightAlpha = 1f;
            ApplyColors();
        }
        else
        {
            fadeCoroutine = StartCoroutine(FadeInRight());
        }
    }

    IEnumerator FadeInRight()
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            rightAlpha = Mathf.Clamp01(t / fadeDuration);
            ApplyColors();
            yield return null;
        }

        rightAlpha = 1f;
        ApplyColors();
        fadeCoroutine = null;
    }

    // ---------- Texto ----------

    void TypeLine(string text)
    {
        fullText = text;
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (autoCoroutine != null) StopCoroutine(autoCoroutine); // evita saltar dos líneas seguidas

        typingCoroutine = StartCoroutine(TypewriterUtility.TypeText(
            fullText,
            dialogueText,
            typingSpeed,
            (typing) => isTyping = typing,
            null, // No necesitamos setFullText ya que lo manejamos localmente
            autoMode,
            OnTypingFinished
        ));
    }

    void OnTypingFinished()
    {
        // Si después de esta línea toca elegir, salen las opciones (y no avanza solo)
        if (TryShowChoices()) return;

        if (autoMode && !isTyping)
            autoCoroutine = StartCoroutine(AutoAdvance());
    }

    // ---------- Opciones ----------

    bool TryShowChoices()
    {
        if (waitingChoice) return true;    // ya están en pantalla (evita mostrarlas dos veces)
        if (showingResponse) return false; // las respuestas no abren más opciones

        ChoicePoint point = FindChoicePoint(currentIndex);
        if (point == null) return false;

        ShowChoices(point);
        return true;
    }

    ChoicePoint FindChoicePoint(int lineIndex)
    {
        if (choicePoints == null) return null;

        foreach (var p in choicePoints)
        {
            if (p.afterLine == lineIndex) return p;
        }
        return null;
    }

    void ShowChoices(ChoicePoint point)
    {
        waitingChoice = true;
        if (autoCoroutine != null) StopCoroutine(autoCoroutine);

        labelA.text = point.optionA.label;
        labelB.text = point.optionB.label;

        buttonA.onClick.RemoveAllListeners();
        buttonB.onClick.RemoveAllListeners();
        buttonA.onClick.AddListener(() => Choose(point.optionA));
        buttonB.onClick.AddListener(() => Choose(point.optionB));

        choicesPanel.SetActive(true);
    }

    void Choose(ChoiceOption option)
    {
        HiddenScore += option.points;
        Debug.Log("Puntos ocultos: " + HiddenScore); // quítalo al final del desarrollo

        waitingChoice = false;
        choicesPanel.SetActive(false);

        // Sin respuesta escrita: pasa directo a la siguiente línea
        if (string.IsNullOrEmpty(option.response))
        {
            NextLine();
            return;
        }

        showingResponse = true;
        dialogueText.fontStyle = FontStyles.Normal;

        // Expresión opcional de quien responde
        if (option.responsePortrait != null)
            ApplyPortrait(option.responseSide, option.responsePortrait, null);

        SetSpeakerHighlight(option.responseSide);

        switch (option.responseSide)
        {
            case DialogueSide.Left: nameText.text = leftCharacterName; break;
            case DialogueSide.Right: nameText.text = rightCharacterName; break;
            default: nameText.text = ""; break;
        }

        TypeLine(option.response);
    }

    bool SameName(string a, string b)
    {
        return !string.IsNullOrEmpty(a) && !string.IsNullOrEmpty(b) &&
               string.Equals(a.Trim(), b.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    bool IsLeftAlias(string who)
    {
        if (leftAliases == null) return false;

        foreach (var alias in leftAliases)
        {
            if (SameName(who, alias)) return true;
        }
        return false;
    }

    // ---------- Avance ----------


    IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(autoDelay);

        if (!isActive || waitingChoice || isTyping)
            yield break;

        // No avanzar si hay opciones pendientes.
        if (TryShowChoices())
            yield break;

        NextLine();
    }


    //public void OnJump(InputValue value)
    //{
    //    if (!isActive) return;
    //    AdvanceLine();
    //}


    void Update()
    {
        if (!isActive || waitingChoice) return;

        // Comprobar las opciones mientras el texto está completo.
        // Así no dependemos de detectar el cambio de isTyping.
        if (!isTyping && !showingResponse)
        {
            if (TryShowChoices()) return;
        }

        bool clicked =
            (Keyboard.current != null &&
             Keyboard.current.spaceKey.wasPressedThisFrame) ||
            (Mouse.current != null &&
             Mouse.current.leftButton.wasPressedThisFrame);

        if (clicked)
        {
            AdvanceLine();
        }
    }



    public void AdvanceLine()
    {
        if (!isActive || waitingChoice) return;

        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            dialogueText.text = fullText;
            isTyping = false;

            if (TryShowChoices()) return;

            if (autoMode)
            {
                if (autoCoroutine != null)
                    StopCoroutine(autoCoroutine);

                autoCoroutine = StartCoroutine(AutoAdvance());
            }

            return;
        }

        // Antes de avanzar, verificar si existen opciones.
        // Esto evita saltarlas cuando se hace clic rápidamente.
        if (TryShowChoices()) return;

        if (autoCoroutine != null)
        {
            StopCoroutine(autoCoroutine);
            autoCoroutine = null;
        }

        NextLine();
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

        if (autoMode && !isTyping && !waitingChoice)
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
        if (waitingChoice) return;

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
        waitingChoice = false;
        if (choicesPanel != null) choicesPanel.SetActive(false);

        Debug.Log("Fin del diálogo");
        OnDialogueEnd?.Invoke();
        this.gameObject.SetActive(false);
    }
}