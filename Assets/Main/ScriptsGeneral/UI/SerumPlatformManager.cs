using UnityEngine;
using UnityEngine.UI;

public class SerumPlatformManager : MonoBehaviour
{
    public Image platformImage;

    [Header("Default Sprite")]
    public Sprite defaultSprite;

    [Header("Platform Sprites")]
    public Sprite pinkSprite;
    public Sprite blueSprite;
    public Sprite redSprite;
    public Sprite greenSprite;
    public Sprite purpleSprite;

    [Header("Glow Colors (Editable)")]
    public Color defaultGlowColor;
    public Color pinkGlowColor;
    public Color blueGlowColor;
    public Color redGlowColor;
    public Color greenGlowColor;
    public Color purpleGlowColor;

    private Material platformMaterial;

    void Awake()
    {
        // Create unique material instance
        platformMaterial = new Material(platformImage.material);
        platformImage.material = platformMaterial;
    }

    public void UpdatePlatform(UIHoverSelect.SerumType type)
    {
        switch (type)
        {
            case UIHoverSelect.SerumType.Pink:
                platformImage.sprite = pinkSprite;
                SetGlowColor(pinkGlowColor);
                break;

            case UIHoverSelect.SerumType.Blue:
                platformImage.sprite = blueSprite;
                SetGlowColor(blueGlowColor);
                break;

            case UIHoverSelect.SerumType.Red:
                platformImage.sprite = redSprite;
                SetGlowColor(redGlowColor);
                break;

            case UIHoverSelect.SerumType.Green:
                platformImage.sprite = greenSprite;
                SetGlowColor(greenGlowColor);
                break;

            case UIHoverSelect.SerumType.Purple:
                platformImage.sprite = purpleSprite;
                SetGlowColor(purpleGlowColor);
                break;
        }
    }

    public void ResetPlatform()
    {
        platformImage.sprite = defaultSprite;
        SetGlowColor(defaultGlowColor);
    }

    void SetGlowColor(Color color)
    {
        if (platformMaterial != null && platformMaterial.HasProperty("_GlowColor"))
        {
            platformMaterial.SetColor("_GlowColor", color);
        }
    }
}