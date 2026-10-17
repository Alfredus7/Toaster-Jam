using UnityEngine;

[System.Serializable]
public class LevelEntryData
{
    [Tooltip("ID único del nivel (por ejemplo 0, 1, 2...)")]
    public int levelID;

    [Tooltip("Objeto UI que representa este nivel en el menú")]
    public GameObject entryObject;

    [Tooltip("Nombre opcional para depuración o mostrar en UI")]
    public string displayName;
}

public class LevelMenuController : MonoBehaviour
{
    [Header("Entradas de niveles")]
    public LevelEntryData[] levelEntries;

    [Header("UI de diálogo introductorio")]
    public GameObject Dialog;

    private void Start()
    {
        if (LevelProgressManager.Instance == null)
        {
            Debug.LogError("❌ No se encontró LevelProgressManager en la escena.");
            return;
        }

        UpdateLevelEntries();

        // Mostrar diálogo solo si el nivel 1 NO está desbloqueado
        if (!LevelProgressManager.Instance.IsLevelUnlocked(1))
        {
            Debug.Log("🗨️ Mostrando diálogo introductorio (nivel 1 no desbloqueado)...");
            Dialog.SetActive(true);
        }
        else
        {
            Debug.Log("ℹ️ Nivel 1 ya está desbloqueado, no se muestra diálogo.");
            Dialog.SetActive(false);
        }
    }

    private void UpdateLevelEntries()
    {
        foreach (var entry in levelEntries)
        {
            if (entry.entryObject == null)
            {
                Debug.LogWarning($"⚠️ El nivel con ID {entry.levelID} no tiene GameObject asignado.");
                continue;
            }

            bool unlocked = LevelProgressManager.Instance.IsLevelUnlocked(entry.levelID);
            entry.entryObject.SetActive(unlocked);
        }
    }

    // Desbloquear Nivel 1 al terminar el diálogo introductorio
    public void EndDialog()
    {
        LevelProgressManager.Instance.UnlockLevel(1);
        Debug.Log("📜 Diálogo introductorio completado, nivel 1 desbloqueado.");

        UpdateLevelEntries();

        // Opcional: cerrar el diálogo después de desbloquear
        Dialog.SetActive(false);
    }
}