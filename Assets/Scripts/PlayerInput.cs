using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private Vector2 m_move;
    private Vector2 m_look;
    private bool m_fire;
    private bool m_crouch;
    private bool m_run;
    private bool m_aim;

    private Vector2 m_mousePosition;

    public Vector2 Move => inputLocked ? Vector2.zero : m_move;
    public Vector2 Look => inputLocked ? Vector2.zero : m_look;
    public bool Run => !inputLocked && m_run;
    public bool Crouch => !inputLocked && m_crouch;
    public bool Fire => !inputLocked && m_fire;
    public bool Aim => !inputLocked && m_aim;

    public Vector2 MousePos => m_mousePosition;

    bool inputLocked = false;

    private void Start()
    {
        Cursor.visible = false;
    }

    public void OnMove(InputValue inputValue)
    {
        m_move = inputValue.Get<Vector2>();
    }

    public void OnLook(InputValue inputValue)
    {
        m_look = inputValue.Get<Vector2>();
    }

    public void OnCrouch(InputValue inputValue)
    {
        m_crouch = !m_crouch;
    }

    public void OnFire(InputValue inputValue)
    {
        m_fire = inputValue.isPressed;
    }

    public void OnRun(InputValue inputValue)
    {
        m_run = inputValue.isPressed;
    }

    public void OnAim(InputValue inputValue)
    {
        m_aim = inputValue.isPressed;
    }

    public void SetFireDone()
    {
        m_fire = false;
    }

    public void OnMousePosition(InputValue inputValue)
    {
        m_look = inputValue.Get<Vector2>();
    }
}
