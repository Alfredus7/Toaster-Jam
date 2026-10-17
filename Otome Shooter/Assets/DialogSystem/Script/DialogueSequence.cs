using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(2, 5)] public string text;
}

[CreateAssetMenu(fileName = "NewDialogueSequence", menuName = "Dialogue/Sequence")]
public class DialogueSequence : ScriptableObject
{
    public DialogueLine[] lines;
}

