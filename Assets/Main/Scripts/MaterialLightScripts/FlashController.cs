using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FlashController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool affectChildSpriteRenderers = true;
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashDuration = 0.1f;

    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

    private MaterialPropertyBlock propertyBlock;
    private Coroutine flashCoroutine;
    private readonly List<SpriteRenderer> targetRenderers = new();

    protected virtual void Awake()
    {
        propertyBlock = new MaterialPropertyBlock();
        ResolveSpriteRenderers();
        ApplyFlash(0f);
    }

    protected virtual void OnDisable()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        ApplyFlash(0f);
    }

    public void Flash()
    {
        PlayHitFlash();
    }

    public void PlayHitFlash()
    {
        ResolveSpriteRenderers();
        if (targetRenderers.Count == 0)
            return;

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        float duration = Mathf.Max(0.01f, flashDuration);
        float elapsed = 0f;

        ApplyFlash(1f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            ApplyFlash(1f - normalizedTime);
            yield return null;
        }

        ApplyFlash(0f);
        flashCoroutine = null;
    }

    private void ApplyFlash(float amount)
    {
        if (propertyBlock == null)
            return;

        float clampedAmount = Mathf.Clamp01(amount);

        for (int i = 0; i < targetRenderers.Count; i++)
        {
            SpriteRenderer targetRenderer = targetRenderers[i];
            if (targetRenderer == null)
                continue;

            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(FlashColorId, flashColor);
            propertyBlock.SetFloat(FlashAmountId, clampedAmount);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ResolveSpriteRenderers()
    {
        targetRenderers.Clear();

        if (spriteRenderer != null)
            AddRenderer(spriteRenderer);

        if (!affectChildSpriteRenderers)
        {
            if (targetRenderers.Count == 0)
                AddRenderer(GetComponent<SpriteRenderer>());
            return;
        }

        SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < childRenderers.Length; i++)
            AddRenderer(childRenderers[i]);

        if (targetRenderers.Count == 0)
            AddRenderer(GetComponentInParent<SpriteRenderer>());
    }

    private void AddRenderer(SpriteRenderer candidate)
    {
        if (candidate == null || targetRenderers.Contains(candidate))
            return;

        targetRenderers.Add(candidate);
    }
}
