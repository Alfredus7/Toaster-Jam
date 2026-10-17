using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class Cell : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
{
    public enum CellType { Empty, Start, End, Fails, Dot }
    public CellType type = CellType.Empty;

    private Color dotColor;
    private Color baseColor;
    private Color neutralColor;
    private Image image;
    private PuzzleConnectDots puzzle;

    public void Init(PuzzleConnectDots puzzleRef, Color neutral)
    {
        puzzle = puzzleRef;
        image = GetComponent<Image>();
        baseColor = image.color;
        neutralColor = neutral;
        dotColor = image.color;
    }

    public void OnPointerDown(PointerEventData e) => puzzle.StartDrawing(this);
    public void OnPointerEnter(PointerEventData e) => puzzle.ContinueDrawing(this);
    public void OnPointerUp(PointerEventData e) => puzzle.EndDrawing(this);

    public void SetColor(Color c) => image.color = c;
    public void SetImage(Sprite s) => image.sprite = s;
    public void ResetColor() => image.color = baseColor;

    public Color GetDotColor() => dotColor;

    public bool HasColor()
    {
        // Si el color es igual (o casi igual) al neutral definido, se considera “sin color”
        return Vector4.Distance(dotColor, neutralColor) > 0.05f;
    }
}
