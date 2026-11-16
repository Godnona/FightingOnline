using UnityEngine;

public class SerumSlot : MonoBehaviour
{
    public UIHoverSelect.SerumType currentSerum;
    public bool isFilled = false;

    [Header("UI References")]
    public GameObject emptyObject;      // Icon dấu cộng (+)
    public UnityEngine.UI.Image serumDisplayImage; // Image hiển thị serum

    [System.Serializable]
    public class SerumEntry
    {
        public UIHoverSelect.SerumType type;
        public Sprite serumSprite; 
    }

    public SerumEntry[] serums;   

    public void FillSlot(UIHoverSelect.SerumType type, Sprite serumSprite = null)
    {
        currentSerum = type;
        isFilled = true;

        // Ưu tiên dùng sprite truyền vào, nếu không có thì tìm trong mảng serums
        Sprite spriteToUse = serumSprite;
        
        if (spriteToUse == null)
        {
            foreach (var entry in serums)
            {
                if (entry.type == type)
                {
                    spriteToUse = entry.serumSprite;
                    break;
                }
            }
        }

        // Cập nhật hình ảnh hiển thị
        if (serumDisplayImage != null && spriteToUse != null)
        {
            serumDisplayImage.sprite = spriteToUse;
            serumDisplayImage.gameObject.SetActive(true);
        }

        // Ẩn icon dấu cộng (+)
        if (emptyObject != null)
        {
            emptyObject.SetActive(false);
        }
    }

    public void ClearSlot()
    {
        // Hide serum image
        if (serumDisplayImage != null)
        {
            serumDisplayImage.gameObject.SetActive(false);
        }

        // Show the empty (+) icon
        if (emptyObject != null)
        {
            emptyObject.SetActive(true);
        }

        isFilled = false;
    }
}