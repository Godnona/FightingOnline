using UnityEngine;
using UnityEngine.EventSystems;
public class CharacterSelectionManager : MonoBehaviour
{
    public enum CharacterType
    {
        Seinna,
        RX9,
        Xandros,
        Kaitana, Ion
    }
    enum InputMode
{
    Keyboard,Mouse
}
[Header("UI Button Feedback")]
public UnityEngine.UI.Button selectionButton;  // Your button
public UnityEngine.Sprite yellowSprite;        // Default / unselected
public UnityEngine.Sprite greenSprite;         // Selected
InputMode currentInputMode = InputMode.Keyboard;
    public CharacterSlot[] slots;

    int currentIndex = 0;

    CharacterSlot hoveredSlot;
    CharacterSlot lockedSlot;
    [Header("Deactivate On First Interaction")]
public GameObject objectToDeactivate;
private bool hasInteracted = false;
private bool ignoreFirstHover = true;
[Header("Xandros TMP Glow")]
public TMPGlowController xandrosGlow;
[Header("Ready Button Control")]
public CanvasGroup readyCanvasGroup;
[Header("Scroll Settings")]
public UnityEngine.UI.ScrollRect scrollRect;
void Start()
{
    if (scrollRect != null) scrollRect.normalizedPosition = new Vector2(0, 1);
    if (slots != null && slots.Length > 0) LockSlot(slots[0]); // Default select first char
    ignoreFirstHover = false;

    // Ensure Ready button is off initially
    if (selectionButton != null && selectionButton.image != null)
    {
        selectionButton.interactable = false;
        selectionButton.image.sprite = yellowSprite;
    }
}
    void Update()
    {
        DetectInputMode();
        HandleKeyboardNavigation();
       // HandleMouseClick();
        HandleUnlockInput();
        UpdateReadyButton();
    }
void HandleKeyboardNavigation()
{
    // 🔥 Movement is only allowed if no slot is locked
    if (currentInputMode == InputMode.Keyboard && lockedSlot == null)
    {
        if (Input.GetKeyDown(KeyCode.D))
            Move(1);

        if (Input.GetKeyDown(KeyCode.A))
            Move(-1);

        if (Input.GetKeyDown(KeyCode.S))
            MoveRow(1);

        if (Input.GetKeyDown(KeyCode.W))
            MoveRow(-1);
    }

    // 🔥 Enter should ALWAYS work, to lock the current hovered slot
    if (Input.GetKeyDown(KeyCode.Return))
        FinalizeSelection();
}
void Move(int direction)
{
    int newIndex = currentIndex;

    do
    {
        newIndex += direction;

        if (newIndex < 0 || newIndex >= slots.Length)
            return;

    } while (!slots[newIndex].isUnlocked);

    HoverByIndex(newIndex);
}

void MoveRow(int direction)
{
    int columns = 5;
    int newIndex = currentIndex;

    do
    {
        newIndex += direction * columns;

        if (newIndex < 0 || newIndex >= slots.Length)
            return;

    } while (!slots[newIndex].isUnlocked);

    HoverByIndex(newIndex);
}

void HoverByIndex(int index)
{
    if (hoveredSlot != null)
        hoveredSlot.SetNormal();

    currentIndex = index;
    hoveredSlot = slots[currentIndex];
    hoveredSlot.SetHover();

    if (!ignoreFirstHover)
        DeactivateIfNeeded();
}

public void HoverSlot(CharacterSlot slot)
{
    if (lockedSlot != null) return;
    if (!slot.isUnlocked) return;
    if (hoveredSlot == slot) return;

    if (hoveredSlot != null)
        hoveredSlot.SetNormal();

    hoveredSlot = slot;
    currentIndex = System.Array.IndexOf(slots, slot);
    hoveredSlot.SetHover();

    DeactivateIfNeeded();   // 🔥 ADD THIS
}
/*void HandleMouseClick()
{
    if (Input.GetMouseButtonDown(0))
    {
        // Only allow click if pointer is over UI
        if (!EventSystem.current.IsPointerOverGameObject())
            return;

        if (hoveredSlot != null)
            LockSlot(hoveredSlot);
    }
}*/
public void SelectSlotFromClick(CharacterSlot slot)
{
    if (lockedSlot != null)
        lockedSlot.Unlock();

    lockedSlot = slot;
    lockedSlot.Lock();

  //  DeactivateIfNeeded();   // 🔥 ADD THIS
}
void LockSlot(CharacterSlot slot)
{
    if (!slot.isUnlocked) return;   // 👈 ADD THIS

    if (lockedSlot != null)
        lockedSlot.Unlock();

    lockedSlot = slot;
    lockedSlot.Lock();
}
void DetectInputMode()
{
    // If any keyboard movement key pressed
    if (Input.GetKeyDown(KeyCode.W) ||
        Input.GetKeyDown(KeyCode.A) ||
        Input.GetKeyDown(KeyCode.S) ||
        Input.GetKeyDown(KeyCode.D))
    {
        if (currentInputMode != InputMode.Keyboard)
        {
            SwitchToKeyboardMode();
        }

        currentInputMode = InputMode.Keyboard;
    }
// Mouse movement
if (Input.GetAxis("Mouse X") != 0 ||
    Input.GetAxis("Mouse Y") != 0)
{
    if (currentInputMode != InputMode.Mouse)
    {
        currentInputMode = InputMode.Mouse;

        // DO NOT clear hovered slot here
        // Let HoverSlot() handle actual changes
    }
}
}
void SwitchToKeyboardMode()
{
    if (lockedSlot != null)
    {
        lockedSlot.Unlock();
        lockedSlot = null;
    }
}
void HandleUnlockInput()
{
    // Backspace (Keyboard)
    if (Input.GetKeyDown(KeyCode.Backspace))
    {
        UnlockCurrentSelection();
    }

    // Right Mouse Button
    if (Input.GetMouseButtonDown(1))
    {
        UnlockCurrentSelection();
    }
}
void UnlockCurrentSelection()
{
    if (lockedSlot != null)
    {
        lockedSlot.Unlock();
        lockedSlot = null;

        // Restore hover highlight to current index
        if (hoveredSlot != null)
            hoveredSlot.SetHover();
    }
}
void FinalizeSelection()
{
    if (lockedSlot == null && hoveredSlot != null)
    {
        LockSlot(hoveredSlot);
    }

    if (lockedSlot != null)
    {
        Debug.Log("FINAL SELECTED: " + lockedSlot.characterType);

        // Update Ready button whenever a character is finalized
        UpdateReadyButton();
    }
}
void UpdateReadyButton()
{
    bool isXandrosActive =
        (hoveredSlot != null && hoveredSlot.characterType == CharacterType.Xandros) ||
        (lockedSlot != null && lockedSlot.characterType == CharacterType.Xandros);

    // TMP glow
    if (xandrosGlow != null)
        xandrosGlow.SetGlow(isXandrosActive);

    // Sprite swap
    if (selectionButton != null && selectionButton.image != null)
        selectionButton.image.sprite = isXandrosActive ? greenSprite : yellowSprite;

    // 🔥 REAL interactable control
    if (readyCanvasGroup != null)
    {
        readyCanvasGroup.interactable = isXandrosActive;
        readyCanvasGroup.blocksRaycasts = isXandrosActive;
        readyCanvasGroup.alpha = isXandrosActive ? 1f : 0.6f;
    }
}
void DeactivateIfNeeded()
{
    if (hasInteracted) return;

    if (objectToDeactivate != null)
        objectToDeactivate.SetActive(false);

    hasInteracted = true;
}
}