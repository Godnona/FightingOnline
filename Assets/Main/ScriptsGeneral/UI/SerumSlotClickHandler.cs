using UnityEngine;
using UnityEngine.EventSystems;

public class SerumSlotClickHandler : MonoBehaviour, IPointerClickHandler
{
    public int slotIndex;
    public SerumInjectorManager manager;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            manager.RemoveSerum(slotIndex);
        }
    }
}