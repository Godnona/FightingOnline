using TMPro;
using UnityEngine;

public class TMPGlowController : MonoBehaviour
{
    [Header("TMP Reference")]
    public TMP_Text tmpText;

    [Header("Glow Values")]
    public float normalGlow = 0f;
    public float activeGlow = 3.5f;

    Material runtimeMat;

    void EnsureInitialized()
    {
        if (runtimeMat != null) return;
        if (tmpText == null) tmpText = GetComponent<TMP_Text>();
        if (tmpText == null) return;

        // Create runtime material instance (VERY IMPORTANT)
        runtimeMat = Instantiate(tmpText.fontMaterial);
        tmpText.fontMaterial = runtimeMat;
    }

    void Awake()
    {
        // SetGlow(false); // Remove this to avoid early instantiation
    }

    public void SetGlow(bool active)
    {
        EnsureInitialized();
        if (runtimeMat == null) return;
        runtimeMat.SetFloat("_GlowIntensity", active ? activeGlow : normalGlow);
    }
}