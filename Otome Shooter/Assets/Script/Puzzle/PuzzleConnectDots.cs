using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PuzzleConnectDots : MonoBehaviour
{
    #region // ===================== Variables =====================

    [Header("Configuración")]
    [SerializeField] private Transform gridParent;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Sprite start, empty, Fails, end, dot, path;
    [SerializeField] private Color InBlankColor = new Color(0f, 1f, 0.53f);

    [Header("Eventos")]
    public UnityEvent OnPuzzleStart, OnPuzzleStartDraw, OnPuzzleFail, OnPuzzleConectNode, OnPuzzleEnd;

    private List<Cell> cells = new List<Cell>();
    private bool isDrawing = false;
    private bool isLocked = false;
    private Cell startCell;
    private Color currentColor;

    private Dictionary<Color, List<Cell>> paths = new Dictionary<Color, List<Cell>>();
    private Dictionary<Color, bool> completedPaths = new Dictionary<Color, bool>();

    private Vector2[] directions = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };
    private float neighborThreshold = 1.2f;

    // Para typewriter
    private bool isTypingMessage = false;
    private Coroutine typingCoroutine;
    private float messageTypingSpeed = 0.03f;

    // Mensajes temporales
    private bool showingTemporaryMessage = false;
    private string defaultMessage = "Conecta los nodos para reparar el circuito";

    #endregion

    #region // ===================== Inicialización =====================

    private void OnEnable() => InitializePuzzle();

    private void InitializePuzzle()
    {
        cells = gridParent?.Cast<Transform>()
                   .Select(t => t.GetComponent<Cell>())
                   .Where(c => c != null).ToList() ?? cells;

        ClearMessage();
        ApplySpritesToCells();
        cells.ForEach(cell => cell.Init(this, InBlankColor));

        // Restaurar visualmente los paths existentes
        RestorePathsVisuals();

        OnPuzzleStart?.Invoke();
    }

    private void ApplySpritesToCells()
    {
        foreach (Cell cell in cells)
        {
            Image img = cell.GetComponent<Image>();
            if (img == null) continue;

            img.sprite = cell.type switch
            {
                Cell.CellType.Start => start,
                Cell.CellType.Fails => Fails,
                Cell.CellType.End => end,
                Cell.CellType.Dot => dot,
                _ => empty
            };
        }
    }

    #endregion

    #region // ===================== Dibujar Paths =====================

    public void StartDrawing(Cell cell)
    {
        if (isLocked || (!isDrawing && completedPaths.ContainsKey(cell.GetDotColor()) && completedPaths[cell.GetDotColor()]))
        {
            ShowTemporaryMessage("¡Este nodo ya está conectado!", 1.5f);
            return;
        }

        if (cell.type != Cell.CellType.Start) return;

        startCell = cell;
        currentColor = cell.GetDotColor();
        isDrawing = true;
        paths[currentColor] = new List<Cell> { cell };

        OnPuzzleStartDraw?.Invoke();
    }

    public void ContinueDrawing(Cell cell)
    {
        if (!isDrawing || !paths.ContainsKey(currentColor) || paths[currentColor].Count == 0) return;

        if (completedPaths.ContainsKey(currentColor) && completedPaths[currentColor])
        {
            FailPath("¡Este camino ya está completado!");
            return;
        }

        if (cell.type == Cell.CellType.Start && cell != startCell)
        {
            FailPath("No puedes pasar por otro nodo de inicio");
            return;
        }

        if (IsCellPartOfCompletedPath(cell))
        {
            FailPath("No puedes pasar por rutas ya completadas");
            return;
        }

        if (paths[currentColor].Contains(cell)) return;

        // Lógica para Fails, End y Dot
        if (cell.type == Cell.CellType.Fails)
        {
            if (!cell.HasColor() || cell.GetDotColor() == currentColor)
            {
                FailPath("¡Cortocircuito! Ruta bloqueada");
                return;
            }
        }
        else if (cell.type == Cell.CellType.End)
        {
            if (cell.HasColor() && cell.GetDotColor() != currentColor)
            {
                FailPath("Polaridad incorrecta en nodo final");
                return;
            }
        }
        else if (cell.type == Cell.CellType.Dot)
        {
            if (cell.HasColor() && cell.GetDotColor() != currentColor)
            {
                FailPath("Polaridad incorrecta en punto de paso");
                return;
            }
        }

        Cell lastCell = paths[currentColor].Last();
        if (!IsValidNeighbor(lastCell, cell)) return;

        if (cell.type == Cell.CellType.End)
        {
            paths[currentColor].Add(cell);
            cell.SetImage(start);
            EndDrawing(cell);
            return;
        }

        if (cell.type == Cell.CellType.Dot || cell.type == Cell.CellType.Empty)
        {
            if (!IsCellPartOfCompletedPath(cell) && (!cell.HasColor() || cell.GetDotColor() == currentColor))
            {
                cell.SetColor(currentColor);
                cell.SetImage(path);
            }
            else
            {
                FailPath("No puedes cruzar rutas ya completadas");
                return;
            }
        }

        paths[currentColor].Add(cell);
    }

    public void EndDrawing(Cell cell)
    {
        if (!isDrawing || !paths.ContainsKey(currentColor) || paths[currentColor].Count <= 1)
        {
            if (isDrawing) FailPath("Ruta demasiado corta");
            return;
        }

        if (paths[currentColor].Last().type == Cell.CellType.End)
        {
            completedPaths[currentColor] = true;
            isDrawing = false;

            CheckPuzzleProgress();
        }
        else
        {
            FailPath("Ruta inválida. Reiniciando...");
        }
    }

    private bool IsCellPartOfCompletedPath(Cell cell)
    {
        foreach (var completedPath in completedPaths)
        {
            if (completedPath.Value &&
                paths.ContainsKey(completedPath.Key) &&
                paths[completedPath.Key].Contains(cell))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsValidNeighbor(Cell a, Cell b)
    {
        RectTransform ra = a.GetComponent<RectTransform>();
        RectTransform rb = b.GetComponent<RectTransform>();
        float cellSize = Mathf.Max(ra.sizeDelta.x, ra.sizeDelta.y) * neighborThreshold;

        foreach (var dir in directions)
        {
            float dist = Vector2.Distance(rb.anchoredPosition, ra.anchoredPosition + dir * cellSize);
            if (dist < cellSize * 0.7f) return true;
        }
        return false;
    }

    #endregion

    #region // ===================== Estado del puzzle =====================

    private void CheckPuzzleProgress()
    {
        bool allSourcesConnected = cells
            .Where(c => c.type == Cell.CellType.Start)
            .All(start => completedPaths.ContainsKey(start.GetDotColor()) && completedPaths[start.GetDotColor()]);

        if (!allSourcesConnected)
        {
            ShowTemporaryMessage("¡Conexión parcial establecida!", 1.5f);
            OnPuzzleConectNode?.Invoke();
            return;
        }

        bool allDotsConnected = cells
            .Where(c => c.type == Cell.CellType.Dot)
            .All(dot =>
            {
                if (!dot.HasColor())
                    return paths.Values.Any(p => p.Contains(dot));
                else
                    return paths.ContainsKey(dot.GetDotColor()) && paths[dot.GetDotColor()].Contains(dot);
            });

        if (!allDotsConnected)
        {
            FailPath("Quedaron puntos sin energía");
            return;
        }

        ShowPuzzleCompleted();
    }

    private void ShowPuzzleCompleted()
    {
        ShowTemporaryMessage("¡Conexión reparada!", 2f);
        OnPuzzleConectNode?.Invoke();
        Invoke(nameof(OnPuzzleCompleted), 1f);
    }

    private void OnPuzzleCompleted()
    {
        OnPuzzleEnd?.Invoke();
        GamePlayerManager.Instance.ObjectRepaired();
        gameObject.SetActive(false);
    }

    private void FailPath(string msg)
    {
        if (isLocked) return;
        isLocked = true;
        isDrawing = false;
        ShowTemporaryMessage(msg, 1.5f);
        OnPuzzleFail?.Invoke();
        Invoke(nameof(ResetPuzzle), 0.5f);
    }

    private void ResetPuzzle()
    {
        cells.ForEach(c => c.ResetColor());
        ApplySpritesToCells();
        paths.Clear();
        completedPaths.Clear();
        isDrawing = false;
        isLocked = false;
    }

    #endregion

    #region // ===================== Mensajes temporales =====================

    private void ShowTemporaryMessage(string text, float duration)
    {
        if (messageText == null || !gameObject.activeInHierarchy) return;

        CancelInvoke(nameof(ClearTemporaryMessage));
        showingTemporaryMessage = true;
        typeo(text);
        Invoke(nameof(ClearTemporaryMessage), duration);
    }

    private void ClearTemporaryMessage()
    {
        showingTemporaryMessage = false;
        ShowDefaultMessage();
    }

    private void ShowDefaultMessage()
    {
        if (messageText == null || !gameObject.activeInHierarchy || showingTemporaryMessage) return;
        typeo(defaultMessage);
    }

    private void ClearMessage()
    {
        if (messageText == null || !gameObject.activeInHierarchy) return;
        ShowDefaultMessage();
    }

    #endregion

    #region // ===================== Visualización de Paths =====================

    private void RestorePathsVisuals()
    {
        foreach (var kvp in paths)
        {
            Color pathColor = kvp.Key;
            List<Cell> pathCells = kvp.Value;

            foreach (var cell in pathCells)
            {
                if (cell.type == Cell.CellType.Start) continue;

                if (cell.type == Cell.CellType.End && completedPaths.ContainsKey(pathColor) && completedPaths[pathColor])
                {
                    cell.SetImage(start);
                    cell.SetColor(pathColor);
                    continue;
                }

                cell.SetColor(pathColor);
                cell.SetImage(path);
            }
        }
    }

    #endregion

    #region // ===================== Utilidades =====================

    public void typeo(string text)
    {
        if (!gameObject.activeInHierarchy) return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTypingMessage = false;
        }

        typingCoroutine = StartCoroutine(TypewriterUtility.TypeText(
            text,
            messageText,
            messageTypingSpeed,
            (typing) => isTypingMessage = typing
        ));
    }

    public void RefreshCells() => InitializePuzzle();
    public Color GetInBlankColor() => InBlankColor;

    #endregion
}
