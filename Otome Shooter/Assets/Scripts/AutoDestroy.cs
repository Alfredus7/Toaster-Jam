using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    [SerializeField, Tooltip("Tiempo en segundos antes de que este objeto sea destruido.")]
    float lifetime = 1f;

    void Start()
    {
        // Destruye el objeto automáticamente al pasar el tiempo configurado
        Destroy(gameObject, lifetime);
    }
}