
using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    private const float PortraitSize = 64f;
    private const float Padding = 6f;
    private const float MinTextHeight = 48f;



    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        SerializedProperty textProp =
            property.FindPropertyRelative("text");

        float h = EditorGUIUtility.singleLineHeight;
        float textHeight = GetTextHeight(textProp);

        // Retrato + texto + dos títulos + dos desplegables
        return Padding
            + PortraitSize
            + Padding * 2
            + textHeight
            + Padding * 2
            + (h + Padding) * 4
            + Padding * 2;
    }




    public override void OnGUI(
        Rect pos,
        SerializedProperty property,
        GUIContent label)
    {
        SerializedProperty characterProp =
            property.FindPropertyRelative("character");

        SerializedProperty textProp =
            property.FindPropertyRelative("text");

        SerializedProperty backgroundProp =
            property.FindPropertyRelative("backgroundAction");

        SerializedProperty visibilityProp =
            property.FindPropertyRelative("characterVisibility");

        EditorGUI.BeginProperty(pos, label, property);

        // Fondo general
        EditorGUI.DrawRect(
            pos,
            new Color(0.13f, 0.13f, 0.13f, 1f)
        );

        Rect content = new Rect(
            pos.x + Padding,
            pos.y + Padding,
            pos.width - Padding * 2,
            pos.height - Padding * 2
        );

        // Retrato
        Rect portraitRect = new Rect(
            content.x,
            content.y,
            PortraitSize,
            PortraitSize
        );

        DrawCharacterPortrait(characterProp, portraitRect);

        DialogueCharacter ch =
            characterProp.objectReferenceValue as DialogueCharacter;

        string displayName =
            ch != null ? ch.characterName : "Sin personaje";

        float lineHeight = EditorGUIUtility.singleLineHeight;

        Rect nameRect = new Rect(
            portraitRect.xMax + Padding,
            content.y + 4,
            content.width - PortraitSize - Padding,
            lineHeight
        );

        EditorGUI.LabelField(
            nameRect,
            displayName,
            EditorStyles.boldLabel
        );

        // Selector de personaje
        Rect charRect = new Rect(
            nameRect.x,
            nameRect.yMax + Padding,
            nameRect.width,
            lineHeight
        );

        EditorGUI.PropertyField(
            charRect,
            characterProp,
            GUIContent.none
        );

        // Texto
        float textY =
            content.y + PortraitSize + Padding * 2;

        Rect textRect = new Rect(
            content.x,
            textY,
            content.width,
            GetTextHeight(textProp)
        );

        EditorGUI.PropertyField(
            textRect,
            textProp,
            GUIContent.none
        );

        // Configuración de fondo

        // ----- Opciones de fondo y personajes -----

        float y = textRect.yMax + Padding * 2;
        float optionHeight = EditorGUIUtility.singleLineHeight;

        // Título Fondo
        EditorGUI.LabelField(
            new Rect(content.x, y, content.width, optionHeight),
            "FONDO",
            EditorStyles.boldLabel
        );

        y += optionHeight + Padding;

        // Desplegable de fondo
        if (backgroundProp != null)
        {
            Rect fondoRect = new Rect(
                content.x,
                y,
                content.width,
                optionHeight
            );

            backgroundProp.enumValueIndex = EditorGUI.Popup(
                fondoRect,
                backgroundProp.enumValueIndex,
                backgroundProp.enumDisplayNames
            );
        }

        y += optionHeight + Padding * 2;

        // Título Personajes
        EditorGUI.LabelField(
            new Rect(content.x, y, content.width, optionHeight),
            "PERSONAJES",
            EditorStyles.boldLabel
        );

        y += optionHeight + Padding;

        // Desplegable de visibilidad
        if (visibilityProp != null)
        {
            Rect visibilityRect = new Rect(
                content.x,
                y,
                content.width,
                optionHeight
            );

            visibilityProp.enumValueIndex = EditorGUI.Popup(
                visibilityRect,
                visibilityProp.enumValueIndex,
                visibilityProp.enumDisplayNames
            );
        }

        EditorGUI.EndProperty();
    }


    private float GetTextHeight(SerializedProperty textProp)
    {
        int lineCount = Mathf.Max(
            1,
            textProp.stringValue.Split('\n').Length
        );

        float height =
            EditorGUIUtility.singleLineHeight * lineCount
            + Padding * 2;

        return Mathf.Max(height, MinTextHeight);
    }

    private void DrawCharacterPortrait(
        SerializedProperty characterProp,
        Rect portraitRect)
    {
        Sprite sprite = null;

        if (characterProp.objectReferenceValue != null)
        {
            DialogueCharacter dc =
                characterProp.objectReferenceValue as DialogueCharacter;

            sprite = dc != null ? dc.portrait : null;
        }

        Color frameColor =
            new Color(0.25f, 0.25f, 0.25f);

        EditorGUI.DrawRect(portraitRect, frameColor);

        if (sprite == null)
        {
            EditorGUI.HelpBox(
                portraitRect,
                "No\nPortrait",
                MessageType.None
            );
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

        GUI.DrawTextureWithTexCoords(
            portraitRect,
            tex,
            uv
        );
    }
}
