using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : StaticInstanceWithLogger<InputSystem>
{
    [Header("Character Input Values")]
    public static Vector2 move;
    public static Vector2 look;
    public static bool jump;
    public static bool sprint;

    [Header("Movement Settings")]
    public static bool analogMovement;
    public static bool isEnabled = false;

    [Header("Mouse Cursor Settings")]
    public static bool cursorLocked = false;
    public static bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnLook(InputValue value)
    {
        if (cursorInputForLook)
        {
            LookInput(value.Get<Vector2>());
        }
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }
#endif

    public void MoveInput(Vector2 newMoveDirection)
    {
        if (!isEnabled) { return; }
        move = newMoveDirection;
    }

    public void LookInput(Vector2 newLookDirection)
    {
        if (!isEnabled) { return; }
        look = newLookDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        if (!isEnabled) { return; }
        jump = newJumpState;
    }

    public void SprintInput(bool newSprintState)
    {
        if (!isEnabled) { return; }
        sprint = newSprintState;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
