using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private Rigidbody rb;

    [Header("Thresholds")]
    [SerializeField] private float runSpeedThreshold = 0.1f;
    [SerializeField] private float riseSpeedThreshold = 0.05f;
    [SerializeField] private float fallSpeedThreshold = -0.05f;

    private readonly HashSet<string> boolParameters = new HashSet<string>();
    private readonly HashSet<string> triggerParameters = new HashSet<string>();
    private bool wasDashing;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        movement = GetComponent<PlayerMovement>();
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (rb == null) rb = GetComponent<Rigidbody>();

        CacheParameters();
    }

    private void LateUpdate()
    {
        ApplyLocomotionState();
    }

    public void FinishAnimator()
    {
        ResetTriggerIfExists("Dash");
        wasDashing = movement != null && movement.IsDashing;
        ApplyLocomotionState();
    }

    public bool IsStatePlaying(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(stateName);
    }

    public bool IsStateFinished(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return true;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(stateName))
            return false;

        if (animator.IsInTransition(0))
            return false;

        return stateInfo.normalizedTime >= 1f;
    }

    public float GetStateDuration(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return 0f;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(stateName) ? stateInfo.length : 0f;
    }

    private void ApplyLocomotionState()
    {
        if (animator == null || movement == null)
            return;

        float horizontalSpeed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
        float verticalSpeed = rb != null ? rb.linearVelocity.y : 0f;

        bool isGrounded = movement.IsGrounded;
        bool isDashing = movement.IsDashing;
        bool isRunning = isGrounded && !isDashing && horizontalSpeed > runSpeedThreshold;
        bool isJumping = !isGrounded && !isDashing && verticalSpeed > riseSpeedThreshold;
        bool isFalling = !isGrounded && !isDashing && verticalSpeed < fallSpeedThreshold;
        bool isIdle = isGrounded && !isRunning && !isDashing;

        SetBoolIfExists("IsGrounded", isGrounded);
        SetBoolIfExists("IsRunning", isRunning);
        SetBoolIfExists("IsIdle", isIdle);
        SetBoolIfExists("IsJumping", isJumping);
        SetBoolIfExists("MidAirFall", isFalling);

        if (isDashing && !wasDashing)
            SetTriggerIfExists("Dash");

        wasDashing = isDashing;
    }

    private void CacheParameters()
    {
        boolParameters.Clear();
        triggerParameters.Clear();

        if (animator == null)
            return;

        for (int i = 0; i < animator.parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = animator.parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Bool)
                boolParameters.Add(parameter.name);
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
                triggerParameters.Add(parameter.name);
        }
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (animator == null || !boolParameters.Contains(parameterName))
            return;

        animator.SetBool(parameterName, value);
    }

    private void SetTriggerIfExists(string parameterName)
    {
        if (animator == null || !triggerParameters.Contains(parameterName))
            return;

        animator.SetTrigger(parameterName);
    }

    private void ResetTriggerIfExists(string parameterName)
    {
        if (animator == null || !triggerParameters.Contains(parameterName))
            return;

        animator.ResetTrigger(parameterName);
    }
}
