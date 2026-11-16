using UnityEngine;

public class PlayerController1 : MonoBehaviour
{
    [SerializeField] private PlayerInputController inputController;
    [SerializeField] private PlayerMovement movementController;

    public PlayerInputController InputController => inputController;
    public PlayerMovement MovementController => movementController;

    // Auto-fill the local controller references from the same player object.
    private void Reset()
    {
        inputController = GetComponent<PlayerInputController>();
        movementController = GetComponent<PlayerMovement>();
    }

    // Cache the minimal local references from the same player object.
    private void Awake()
    {
        if (inputController == null) inputController = GetComponent<PlayerInputController>();
        if (movementController == null) movementController = GetComponent<PlayerMovement>();
    }
}
