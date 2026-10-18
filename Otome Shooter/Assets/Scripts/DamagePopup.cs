using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] TMP_Text textMesh;

    [Header("Movimiento")]
    [SerializeField] float lifetime = 1f;
    [SerializeField] float riseSpeed = 1.5f;
    [SerializeField] float horizontalJitter = 0.35f;

    [Header("Animación (curvas 0→1)")]
    [SerializeField] AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0.4f, 0.2f, 1f);
    [SerializeField] AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("Colores")]
    [SerializeField] Color normalColor = Color.white;
    [SerializeField] Color critColor = new Color(1f, 0.35f, 0.1f);

    float timer;
    Vector3 moveDir;
    Transform cam;

    public void Setup(float amount, bool isCrit = false)
    {
        timer = 0f;
        cam = Camera.main != null ? Camera.main.transform : null;

        float jitter = Random.Range(-horizontalJitter, horizontalJitter);
        moveDir = new Vector3(jitter, riseSpeed, 0f);

        if (textMesh != null)
        {
            textMesh.text = Mathf.RoundToInt(amount).ToString();
            textMesh.color = isCrit ? critColor : normalColor;
        }
    }

    void LateUpdate()
    {
        timer += Time.deltaTime;
        float t = timer / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }

        // Subir con jitter
        transform.position += moveDir * Time.deltaTime;

        // Billboard hacia la cámara (siempre mira al jugador)
        if (cam != null)
            transform.rotation = Quaternion.LookRotation(transform.position - cam.position);

        // Escala
        if (scaleCurve != null && scaleCurve.length > 0)
            transform.localScale = Vector3.one * scaleCurve.Evaluate(t);

        // Fade alpha
        if (textMesh != null && fadeCurve != null && fadeCurve.length > 0)
        {
            var c = textMesh.color;
            c.a = fadeCurve.Evaluate(t);
            textMesh.color = c;
        }
    }
}