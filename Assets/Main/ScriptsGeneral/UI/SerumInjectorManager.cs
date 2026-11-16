using UnityEngine;

public class SerumInjectorManager : MonoBehaviour
{
    public SerumSlot[] slots; // Assign Slot1, Slot2, Slot3

    public void InjectSelectedSerum()
    {
        if (UIHoverSelect.currentlySelected == null)
        {
            Debug.Log("No serum selected!");
            return;
        }

        UIHoverSelect.SerumType selectedType = UIHoverSelect.currentlySelected.serumType;
        Sprite selectedSprite = UIHoverSelect.currentlySelected.GetSerumSprite();

        foreach (SerumSlot slot in slots)
        {
            if (!slot.isFilled)
            {
                slot.FillSlot(selectedType, selectedSprite);
                return;
            }
        }

        Debug.Log("All slots full!");
    }

    public void RemoveSerum(int index)
    {
        if (!slots[index].isFilled)
            return;

        // Clear the clicked slot
        slots[index].ClearSlot();

        // Shift everything above down
        for (int i = index + 1; i < slots.Length; i++)
        {
            if (slots[i].isFilled)
            {
                // Move data from slot[i] to slot[i-1]
                Sprite spriteToMove = null;
                if (slots[i].serumDisplayImage != null)
                {
                    spriteToMove = slots[i].serumDisplayImage.sprite;
                }

                slots[i - 1].FillSlot(slots[i].currentSerum, spriteToMove);
                slots[i].ClearSlot();
            }
        }
    }
}
