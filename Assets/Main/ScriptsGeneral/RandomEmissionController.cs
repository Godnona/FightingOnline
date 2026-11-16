using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomEmissionController : MonoBehaviour
{
    [Header("Renderer")]
    public Renderer targetRenderer;

    [Header("Select Materials (by index)")]
    public List<int> materialIndexes = new List<int>();

    [Header("Timing Settings")]
    public float minInterval = 0.2f;
    public float maxInterval = 1f;

    private Material[] materials;
    private Coroutine toggleRoutine;
    private bool isActive;

    void Awake()
    {
        if (!targetRenderer)
            targetRenderer = GetComponent<Renderer>();

        materials = targetRenderer.materials;
        DisableEmission();
    }

    // Call to start random blinking
    public void Activate()
    {
        if (isActive) return;
        isActive = true;
        toggleRoutine = StartCoroutine(RandomToggle());
    }

    // Call to stop
    public void Deactivate()
    {
        if (!isActive) return;
        isActive = false;

        if (toggleRoutine != null)
            StopCoroutine(toggleRoutine);

        DisableEmission();
    }

    IEnumerator RandomToggle()
    {
        while (isActive)
        {
            ToggleEmission();
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
        }
    }

    void ToggleEmission()
    {
        foreach (int index in materialIndexes)
        {
            if (index < 0 || index >= materials.Length) continue;

            if (materials[index].IsKeywordEnabled("_EMISSION"))
                materials[index].DisableKeyword("_EMISSION");
            else
                materials[index].EnableKeyword("_EMISSION");
        }
    }

    void DisableEmission()
    {
        foreach (int index in materialIndexes)
        {
            if (index < 0 || index >= materials.Length) continue;
            materials[index].DisableKeyword("_EMISSION");
        }
    }
}
