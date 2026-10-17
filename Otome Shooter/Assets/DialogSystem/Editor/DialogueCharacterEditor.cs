using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DialogueCharacter))]
public class DialogueCharacterEditor : Editor
{
    private const float PortraitSize = 180f;

    public override void OnInspectorGUI()
    {
        DialogueCharacter character = (DialogueCharacter)target;

        EditorGUILayout.Space(10);
        DrawSectionHeader("Character Creation");

        // Nombre
        EditorGUILayout.Space();
        character.characterName = EditorGUILayout.TextField("Name", character.characterName);

        // Portrait
        EditorGUILayout.Space(10);
        EditorGUI.BeginChangeCheck();
        character.portrait = (Sprite)EditorGUILayout.ObjectField("Sprite", character.portrait, typeof(Sprite), false);
        character.Material = (Material)EditorGUILayout.ObjectField("Material (Optional)", character.Material, typeof(Material), false);

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(character);

        EditorGUILayout.Space(8);

        if (character.portrait != null)
        {
            DrawPortraitSingleLayer(character.portrait, character.Material);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign a portrait to preview it.", MessageType.Info);
        }

        EditorGUILayout.Space(10);
    }

    private void DrawSectionHeader(string title)
    {
        GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = new Color(0.88f, 0.88f, 0.88f);

        Rect rect = GUILayoutUtility.GetRect(0, 32, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.16f));
        EditorGUI.LabelField(rect, title, style);
    }

    private void DrawPortraitSingleLayer(Sprite sprite, Material mat)
    {
        Rect total = GUILayoutUtility.GetRect(PortraitSize, PortraitSize + 20, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(total, new Color(0.12f, 0.12f, 0.12f));

        Rect frame = new Rect(total.x + 10, total.y + 10, total.width - 20, total.height - 20);
        EditorGUI.DrawRect(frame, new Color(0.26f, 0.26f, 0.26f));

        Rect imageRect = new Rect(frame.x + 8, frame.y + 8, frame.width - 16, frame.height - 16);
        EditorGUI.DrawRect(imageRect, new Color(0.18f, 0.18f, 0.18f));

        if (Event.current.type != EventType.Repaint)
            return;

        Texture2D tex = sprite.texture;
        if (tex == null)
            return;

        Rect texRect = sprite.textureRect;
        Rect uv = new Rect(
            texRect.x / tex.width,
            texRect.y / tex.height,
            texRect.width / tex.width,
            texRect.height / tex.height
        );

        if (mat == null)
        {
            GUI.DrawTextureWithTexCoords(imageRect, tex, uv);
        }
        else
        {
            mat.SetTexture("_MainTex", tex);
            Graphics.DrawTexture(imageRect, tex, uv, 0, 0, 0, 0, mat);
        }
    }
}
