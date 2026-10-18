
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;

    [TextArea(2, 5)]
    public string text;

    // Control del fondo
    public BackgroundAction backgroundAction =
        BackgroundAction.Keep;

    // Control de los personajes
    public CharacterVisibility characterVisibility =
        CharacterVisibility.Keep;

    // Diálogos que dependen de los puntos
    public bool useScoreDialogue = false;

    public int minimumScore = 3;

    [TextArea(2, 5)]
    public string lowScoreText;

    [TextArea(2, 5)]
    public string highScoreText;
}

[CreateAssetMenu(
    fileName = "NewDialogueSequence",
    menuName = "Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] lines;
}
