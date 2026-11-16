using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class RandomEmissionToggle : MonoBehaviour
{
    Renderer rend;
    MaterialPropertyBlock mpb;

    Color baseEmissionColor;

    [Header("Emission Intensity")]
    public float onIntensity = 1.5f;
    public float offIntensity = 0f;

    [Header("Random Timing")]
    public float minDelay = 0.05f;
    public float maxDelay = 0.5f;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();

        // Read emission color from the material ONCE
        baseEmissionColor = rend.sharedMaterial.GetColor("_EmissionColor");

        StartCoroutine(Flicker());
    }

    IEnumerator Flicker()
    {
        while (true)
        {
            bool emissionOn = Random.value > 0.5f;

            rend.GetPropertyBlock(mpb);

            float intensity = emissionOn ? onIntensity : offIntensity;
            mpb.SetColor("_EmissionColor", baseEmissionColor * intensity);

            rend.SetPropertyBlock(mpb);

            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        }
    }
}
