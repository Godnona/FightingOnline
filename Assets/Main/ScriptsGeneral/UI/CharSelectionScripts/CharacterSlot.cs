using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

[RequireComponent(typeof(Image))]
public class CharacterSlot : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    public CharacterSelectionManager.CharacterType characterType;

    public List<GameObject> previewObjects; // 🔥 Add multiple objects here
    public GameObject characterIcon;       // 🆕 Reference to the character image object
    public GameObject characterNameText;   // 🆕 Reference to the character name text object
    public Image lockIcon;

    [HideInInspector] public bool isLocked = false;
    public bool isUnlocked = true;

    private Image mainImage;
    CharacterSelectionManager manager;

    Color normalColor;
    Color hoverColor;
    public static System.Action OnAnySlotHovered;
    void Awake()
    {
        mainImage = GetComponent<Image>();
    }

    void Start()
    {
        manager = FindFirstObjectByType<CharacterSelectionManager>();

        ColorUtility.TryParseHtmlString("#ADADAD", out normalColor);
        ColorUtility.TryParseHtmlString("#6193FF", out hoverColor);

        SetNormal();
        DeactivatePreviewObjects();
        if (characterNameText != null) characterNameText.SetActive(false);
        UpdateLockedStateVisuals(); // 🆕 Initial setup
    }

    // 🆕 Allows real-time updates in the Inspector
    private void OnValidate()
    {
        UpdateLockedStateVisuals();
    }

    // 🆕 Handles visibility of icons based on unlocked status
    public void UpdateLockedStateVisuals()
    {
        if (characterIcon != null)
            characterIcon.SetActive(isUnlocked);

        if (lockIcon != null)
            lockIcon.enabled = !isUnlocked;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        manager.HoverSlot(this);
    }

    public void SetHover()
    {
        if (!isUnlocked) return;

        mainImage.color = hoverColor;
        ActivatePreviewObjects();

        // 🔥 Notify globally that a slot was hovered
        OnAnySlotHovered?.Invoke();
    }

    public void SetNormal()
    {
        if (!isLocked)
            mainImage.color = normalColor;

        DeactivatePreviewObjects();
    }

    public void Lock()
    {
        if (!isUnlocked) return;

        isLocked = true;
        mainImage.color = hoverColor;
        ActivatePreviewObjects();
        if (characterNameText != null) characterNameText.SetActive(true);
        
        // Remove padlock toggle from selection logic
    }

    public void Unlock()
    {
        isLocked = false;
        mainImage.color = normalColor;
        DeactivatePreviewObjects();
        if (characterNameText != null) characterNameText.SetActive(false);

        // Remove padlock toggle from selection logic
    }

    void ActivatePreviewObjects()
    {
        foreach (var obj in previewObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }
    }

    void DeactivatePreviewObjects()
    {
        foreach (var obj in previewObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
     public void OnPointerClick(PointerEventData eventData)
{
    if (!isUnlocked) return;

    if (manager != null)
        manager.SelectSlotFromClick(this);
}
}