using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public enum AttackType { None, Light, Heavy, AirLight, AirHeavy, DownLight, Punch, Special, SpecialQuake, Cyclone, Kick, Parry }

    [System.Serializable]
    public class AttackBinding
    {
        public AttackType attackType;
        public List<KeyCode> keys = new List<KeyCode>();
    }

    [Header("Move Keys")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode altDashKey = KeyCode.RightShift;
    [SerializeField] private List<KeyCode> leftMoveKeys = new List<KeyCode> { KeyCode.A, KeyCode.LeftArrow };
    [SerializeField] private List<KeyCode> rightMoveKeys = new List<KeyCode> { KeyCode.D, KeyCode.RightArrow };
    [SerializeField] private float doubleTapDashWindow = 0.25f;

    [Header("Attack Keys")]
    [SerializeField] private float attackBufferTime = 0.15f;
    [SerializeField] private List<AttackBinding> attackBindings = new List<AttackBinding>();

    private float attackBufferTimer;
    private float lastLeftTapTime = float.NegativeInfinity;
    private float lastRightTapTime = float.NegativeInfinity;

    public float MoveX { get; private set; }
    public bool JumpPressed { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool DashPressed { get; private set; }
    public float DashDirection { get; private set; }
    public AttackType BufferedAttack { get; private set; }

    // Fill the inspector with a simple default key layout.
    private void Reset()
    {
        EnsureDefaultMoveKeys();
        attackBindings = new List<AttackBinding>
        {
            NewBinding(AttackType.SpecialQuake, KeyCode.S, KeyCode.D, KeyCode.B),
            NewBinding(AttackType.SpecialQuake, KeyCode.S, KeyCode.A, KeyCode.B),
            NewBinding(AttackType.DownLight, KeyCode.S, KeyCode.M),
            NewBinding(AttackType.Light, KeyCode.M),
            NewBinding(AttackType.Heavy, KeyCode.Comma),
            NewBinding(AttackType.Punch, KeyCode.J),
            NewBinding(AttackType.Kick, KeyCode.L),
            NewBinding(AttackType.Parry, KeyCode.P),
            NewBinding(AttackType.Special, KeyCode.B),
            NewBinding(AttackType.Cyclone, KeyCode.K)
        };
    }

    private void Awake()
    {
        EnsureDefaultMoveKeys();
    }

    // Read move input every frame and keep the latest buffered attack.
    private void Update()
    {
        MoveX = Input.GetAxisRaw("Horizontal");
        JumpPressed = Input.GetKeyDown(jumpKey);
        //JumpHeld = Input.GetKey(jumpKey);
        DashPressed = false;
        DashDirection = 0f;
        ReadDashInput();
        TickAttackBuffer();
        if (TryReadAttack(out AttackType attack))
            BufferAttack(attack);
    }

    private void ReadDashInput()
    {
        if (Input.GetKeyDown(dashKey) || Input.GetKeyDown(altDashKey))
        {
            DashPressed = true;
            DashDirection = MoveX > 0.01f ? 1f : MoveX < -0.01f ? -1f : 0f;
            return;
        }

        if (WasDoubleTapped(rightMoveKeys, ref lastRightTapTime))
        {
            DashPressed = true;
            DashDirection = 1f;
            return;
        }

        if (WasDoubleTapped(leftMoveKeys, ref lastLeftTapTime))
        {
            DashPressed = true;
            DashDirection = -1f;
        }
    }

    // Remove expired buffered attacks so the fighter only consumes fresh input.
    private void TickAttackBuffer()
    {
        if (BufferedAttack == AttackType.None)
            return;
        attackBufferTimer -= Time.deltaTime;
        if (attackBufferTimer <= 0f)
            BufferedAttack = AttackType.None;
    }

    // Resolve the highest priority attack that matches the current key press.
    private bool TryReadAttack(out AttackType attack)
    {
        attack = AttackType.None;
        for (int i = 0; i < attackBindings.Count; i++)
        {
            AttackBinding binding = attackBindings[i];
            if (binding == null || binding.keys == null || binding.keys.Count == 0)
                continue;
            if (AllKeysHeld(binding.keys) && AnyKeyPressed(binding.keys))
            {
                attack = binding.attackType;
                return true;
            }
        }
        return false;
    }

    // Store one attack for a short buffer window.
    private void BufferAttack(AttackType attack)
    {
        BufferedAttack = attack;
        attackBufferTimer = attackBufferTime;
    }

    // Let another controller consume the buffered attack once it starts.
    public void ConsumeAttack()
    {
        BufferedAttack = AttackType.None;
        attackBufferTimer = 0f;
    }

    // Make sure combination inputs keep their modifier keys held.
    private bool AllKeysHeld(List<KeyCode> keys)
    {
        for (int i = 0; i < keys.Count; i++)
            if (!Input.GetKey(keys[i]))
                return false;
        return true;
    }

    // Accept the attack on the frame where any key in the combo is pressed.
    private bool AnyKeyPressed(List<KeyCode> keys)
    {
        for (int i = 0; i < keys.Count; i++)
            if (Input.GetKeyDown(keys[i]))
                return true;
        return false;
    }

    // Directional dash fires on the second tap inside the configured input window.
    private bool WasDoubleTapped(List<KeyCode> keys, ref float lastTapTime)
    {
        if (!AnyKeyPressed(keys))
            return false;

        float now = Time.time;
        bool didDoubleTap = now - lastTapTime <= doubleTapDashWindow;
        lastTapTime = now;
        return didDoubleTap;
    }

    // Build one attack binding with any number of keys.
    private AttackBinding NewBinding(AttackType attackType, params KeyCode[] keys)
    {
        AttackBinding binding = new AttackBinding { attackType = attackType };
        binding.keys.AddRange(keys);
        return binding;
    }

    private void EnsureDefaultMoveKeys()
    {
        if (leftMoveKeys == null)
            leftMoveKeys = new List<KeyCode>();

        if (rightMoveKeys == null)
            rightMoveKeys = new List<KeyCode>();

        if (leftMoveKeys.Count == 0)
        {
            leftMoveKeys.Add(KeyCode.A);
            leftMoveKeys.Add(KeyCode.LeftArrow);
        }

        if (rightMoveKeys.Count == 0)
        {
            rightMoveKeys.Add(KeyCode.D);
            rightMoveKeys.Add(KeyCode.RightArrow);
        }
    }
}
