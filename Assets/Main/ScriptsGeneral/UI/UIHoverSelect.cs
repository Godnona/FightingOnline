using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class UIHoverSelect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
    
{
    public enum SerumType
    {
    Pink,
    Blue,
    Red,
    Green,
    Purple
}
    public RectTransform targetToScale;
    public GameObject infoPanel;
    public SerumPlatformManager platformManager;
    [Header("Normal Scale")]
    public float normalScaleX = 1f;
    public float normalScaleY = 1f;

    [Header("Hover / Selected Scale")]
    public float hoverScaleX = 1.15f;
    public float hoverScaleY = 1.15f;

    public float scaleSpeed = 10f;

    private bool isHovered = false;
    private bool isSelected = false;

    public static UIHoverSelect currentlySelected;
    [Header("Serum Data")]
    public Image serumImage;
    [Header("Serum Data")]
    public SerumType serumType;
    void Update()
    {
        float targetX = normalScaleX;
        float targetY = normalScaleY;

        if (isSelected || isHovered)
        {
            targetX = hoverScaleX;
            targetY = hoverScaleY;
        }

        Vector3 targetScale = new Vector3(targetX, targetY, 1f);

        targetToScale.localScale = Vector3.Lerp(
            targetToScale.localScale,
            targetScale,
            Time.deltaTime * scaleSpeed
        );

        // RMB to deselect
        if (isSelected && Input.GetMouseButtonDown(1))
        {
            Deselect();
        }
    }

public void OnPointerEnter(PointerEventData eventData)
{
    isHovered = true;

    // Hiển thị thông tin kể cả khi cái khác đang được chọn
    ShowInfo();

    // Chỉ cập nhật platform nếu chưa có cái nào được CHỌN (isSelected)
    if (currentlySelected == null && platformManager != null)
    {
        platformManager.UpdatePlatform(serumType);
    }
}

public void OnPointerExit(PointerEventData eventData)
{
    if (!isSelected)
    {
        isHovered = false;

        // Only reset platform if nothing is selected
        if (currentlySelected == null && platformManager != null)
        {
            platformManager.ResetPlatform();
        }
    }
}
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (currentlySelected == this)
            {
                Deselect();
            }
            else
            {
                Select();
            }
        }
    }

    void Select()
    {
        // Bỏ chọn cái cũ nếu có
        if (currentlySelected != null && currentlySelected != this)
        {
            currentlySelected.isSelected = false;
            currentlySelected.isHovered = false;
        }

        isSelected = true;
        currentlySelected = this;

        if (platformManager != null)
        {
            platformManager.UpdatePlatform(serumType);
        }

        ShowInfo();
    }

void Deselect()
{
    isSelected = false;
    isHovered = false;
    currentlySelected = null;

    if (platformManager != null)
        platformManager.ResetPlatform();

    HideInfo();
}

    void ShowInfo()
    {
        foreach (UIHoverSelect item in FindObjectsByType<UIHoverSelect>(FindObjectsSortMode.None))
        {
            if (item != this)
                item.HideInfo();
        }

        infoPanel.SetActive(true);
    }

    void HideInfo()
    {
        infoPanel.SetActive(false);
    }
    public Sprite GetSerumSprite()
    {
        return serumImage.sprite;
    }
}
