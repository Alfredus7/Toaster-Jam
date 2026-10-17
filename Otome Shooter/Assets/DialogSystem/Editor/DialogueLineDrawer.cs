using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    private const float PortraitSize = 64f;
    private const float Padding = 6f;
    private const float MinTextHeight = 48f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty textProp = property.FindPropertyRelative("text");
        int lineCount = Mathf.Max(1, textProp.stringValue.Split('\n').Length);

        float textHeight = EditorGUIUtility.singleLineHeight * lineCount + Padding * 2;
        textHeight = Mathf.Max(textHeight, MinTextHeight);

        return PortraitSize + textHeight + Padding * 4 + EditorGUIUtility.singleLineHeight * 2;
    }

    public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label)
    {
        SerializedProperty characterProp = property.FindPropertyRelative("character");
        SerializedProperty textProp = property.FindPropertyRelative("text");

        EditorGUI.BeginProperty(pos, label, property);

        // ----- Fondo general -----
        Color bg = new Color(0.13f, 0.13f, 0.13f, 1f);
        EditorGUI.DrawRect(pos, bg);

        pos.x += Padding;
        pos.y += Padding;
        pos.width -= Padding * 2;

        // ----- Zona retrato + nombre -----
        Rect portraitRect = new Rect(pos.x, pos.y, PortraitSize, PortraitSize);
        Rect nameRect = new Rect(
            pos.x + PortraitSize + Padding,
            pos.y + 4,
            pos.width - PortraitSize - Padding,
            EditorGUIUtility.singleLineHeight
        );

        DrawCharacterPortrait(characterProp, portraitRect);

        DialogueCharacter ch = characterProp.objectReferenceValue as DialogueCharacter;
        string displayName = ch != null ? ch.characterName : "Sin personaje";
        EditorGUI.LabelField(nameRect, displayName, EditorStyles.boldLabel);

        // Selector del personaje
        Rect charFieldRect = new Rect(
            nameRect.x,
            nameRect.y + EditorGUIUtility.singleLineHeight + Padding,
            nameRect.width,
            EditorGUIUtility.singleLineHeight
        );
        EditorGUI.PropertyField(charFieldRect, characterProp, GUIContent.none);

        // ----- Texto del diálogo -----
        float textStartY = pos.y + PortraitSize + Padding * 2;
        float textHeight = GetTextHeight(textProp);

        Rect textRect = new Rect(pos.x, textStartY, pos.width, textHeight);
        EditorGUI.PropertyField(textRect, textProp, new GUIContent(" ")); // sin label

        EditorGUI.EndProperty();
    }

    private float GetTextHeight(SerializedProperty textProp)
    {
        int lineCount = Mathf.Max(1, textProp.stringValue.Split('\n').Length);
        float height = EditorGUIUtility.singleLineHeight * lineCount + Padding * 2;
        return Mathf.Max(height, MinTextHeight);
    }

    private void DrawCharacterPortrait(SerializedProperty characterProp, Rect portraitRect)
    {
        Sprite sprite = null;
        if (characterProp.objectReferenceValue != null)
        {
            DialogueCharacter dc = characterProp.objectReferenceValue as DialogueCharacter;
            sprite = dc != null ? dc.portrait : null;
        }

        Color frameColor = new Color(0.25f, 0.25f, 0.25f);
        EditorGUI.DrawRect(portraitRect, frameColor);

        if (sprite == null)
        {
            EditorGUI.HelpBox(portraitRect, "No\nPortrait", MessageType.None);
            return;
        }

        Texture2D tex = sprite.texture;
        Rect texRect = sprite.textureRect;

        Rect uv = new Rect(
            texRect.x / tex.width,
            texRect.y / tex.height,
            texRect.width / tex.width,
            texRect.height / tex.height
        );

        GUI.DrawTextureWithTexCoords(portraitRect, tex, uv);
    }
}
