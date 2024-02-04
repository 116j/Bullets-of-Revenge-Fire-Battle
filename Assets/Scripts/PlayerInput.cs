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

    private bool m_upperAttack;
    private bool m_lowerAttack;
    private bool m_upperBlock;
    private bool m_middleBlock;
    private bool m_lowerBlock;

    public Vector2 Move => inputLocked ? Vector2.zero : m_move;
    public Vector2 Look => inputLocked ? Vector2.zero : m_look;
    public bool Run => !inputLocked && m_run;
    public bool Crouch => !inputLocked && m_crouch;
    public bool Fire => !inputLocked && m_fire;
    public bool Aim => !inputLocked && m_aim;

    public bool UpperAttack 
    { 
        get {
            if (m_upperAttack)
            {
                m_upperAttack = false;
                return !inputLocked;
            }
            return !inputLocked && m_upperAttack;
        } 
    }
    public bool LowerAttack
    {
        get
        {
            if (m_lowerAttack)
            {
                m_lowerAttack = false;
                return !inputLocked;
            }
            return !inputLocked && m_lowerAttack;
        }
    }
    public bool UpperBlock => !inputLocked && m_upperBlock;
    public bool MiddleBlock => !inputLocked && m_middleBlock;
    public bool LowerBlock => !inputLocked && m_lowerBlock;

    bool inputLocked = false;

    UnityEngine.InputSystem.PlayerInput m_input;

    private void Start()
    {
        Cursor.visible = false;
        m_input=GetComponent<UnityEngine.InputSystem.PlayerInput>();
    }

    public void LockInput()
    {
        inputLocked = !inputLocked;
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
    
    public void DisableCrouch()
    {
        m_crouch = false;
    }

    public void ChangeGanre()
    {
        m_input.SwitchCurrentActionMap("Fighting");
    }

    public void OnUpperAttack(InputValue inputValue)
    {
        m_upperAttack = inputValue.isPressed;
    }

    public void OnLowerAttack(InputValue inputValue)
    {
        m_lowerAttack = inputValue.isPressed;
    }

    public void OnUpperBlock(InputValue inputValue)
    {
        m_upperBlock = inputValue.isPressed;
    }

    public void OnMiddleBlock(InputValue inputValue)
    {
        m_middleBlock = inputValue.isPressed;
    }
    public void OnLowerBlock(InputValue inputValue)
    {
        m_lowerBlock = inputValue.isPressed;
    }
}
