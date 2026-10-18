
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;

    [TextArea(2, 5)]
    public string text;
    public BackgroundAction backgroundAction = BackgroundAction.Keep;
    public CharacterVisibility characterVisibility = CharacterVisibility.Keep;
}

[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] lines;
}
