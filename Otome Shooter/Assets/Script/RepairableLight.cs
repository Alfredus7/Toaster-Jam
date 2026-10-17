using UnityEngine;
using System.Collections;

public class RepairableLight : MonoBehaviour
{
    [Header("Light Component")]
    public Light lightComponent;

    [Header("Normal State Config")]
    public float normalIntensity = 1f;
    public float normalRange = 10f;

    [Header("Fail State Config")]
    public float failIntensity = 0.3f;
    public float failRange = 5f;
    public float flickerInterval = 0.5f;

    private bool isFlickering = false;
    private Coroutine flickerCoroutine;

    void Start()
    {
        // Estado inicial: luz apagada
        SetLightOff();
    }

    // Apagar la luz completamente
    public void SetLightOff()
    {
        if (lightComponent != null)
        {
            lightComponent.enabled = false;
            StopFlickering();
        }
    }

    // Encender la luz en modo falla (intermitente)
    public void SetLightFail()
    {
        if (lightComponent != null)
        {
            lightComponent.enabled = true;
            lightComponent.intensity = failIntensity;
            lightComponent.range = failRange;
            StartFlickering();
        }
    }

    // Reparar la luz (normal, constante)
    public void SetLightRepaired()
    {
        if (lightComponent != null)
        {
            lightComponent.enabled = true;
            lightComponent.intensity = normalIntensity;
            lightComponent.range = normalRange;
            StopFlickering();
        }
    }

    void StartFlickering()
    {
        if (!isFlickering)
        {
            isFlickering = true;
            flickerCoroutine = StartCoroutine(FlickerRoutine());
        }
    }

    void StopFlickering()
    {
        if (isFlickering && flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            isFlickering = false;
        }

        // Asegurarse de que la luz esté visible si está encendida
        if (lightComponent != null && lightComponent.enabled)
        {
            lightComponent.enabled = true;
        }
    }

    IEnumerator FlickerRoutine()
    {
        while (isFlickering)
        {
            // Alternar entre visible e invisible
            if (lightComponent != null)
            {
                lightComponent.enabled = !lightComponent.enabled;
            }

            yield return new WaitForSeconds(flickerInterval);
        }
    }
}