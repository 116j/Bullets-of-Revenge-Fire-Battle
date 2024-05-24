using System;
using System.Linq;
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

    public Vector2 Move => m_inputLocked ? Vector2.zero : m_move;
    public Vector2 Look => m_inputLocked ? Vector2.zero : m_look * m_cameraSensativity * m_invert;
    public bool Run => !m_inputLocked && m_run;
    public bool Crouch => !m_inputLocked && m_crouch;
    public bool Fire => !m_inputLocked && m_fire;
    public bool Aim => !m_inputLocked && m_aim;

    public bool UpperAttack
    {
        get
        {
            if (m_upperAttack)
            {
                m_upperAttack = false;
                return !m_inputLocked;
            }
            return !m_inputLocked && m_upperAttack;
        }
    }
    public bool LowerAttack
    {
        get
        {
            if (m_lowerAttack)
            {
                m_lowerAttack = false;
                return !m_inputLocked;
            }
            return !m_inputLocked && m_lowerAttack;
        }
    }
    public bool UpperBlock => !m_inputLocked && m_upperBlock;
    public bool MiddleBlock => !m_inputLocked && m_middleBlock;

    bool m_inputLocked = true;
    bool m_pause = false;
    float m_cameraSensativity = 1f;
    Vector2 m_invert = new(1, 1);
    bool m_playerDied = false;
    bool m_gameStarted = false;

    UnityEngine.InputSystem.PlayerInput m_input;

    public bool GameStrted => m_gameStarted;

    private void Start()
    {
        Cursor.visible = true;
        m_input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        m_input.uiInputModule.cancel.action.performed += UIController.Instance.CancelLayout;
    }

    private void OnEnable()
    {
        if (m_input != null)
            m_input.enabled = true;
    }

    private void OnDisable()
    {
        if (m_input != null)
            m_input.enabled = false;
    }

    public void StartGame()
    {
        m_gameStarted = true;
        Cursor.visible = false;
    }

    public void Die()
    {
        m_playerDied = true;
        m_inputLocked = true;
        UIController.Instance.SetDieLayout(m_playerDied);
    }

    public void LockInput()
    {
        m_inputLocked = !m_inputLocked;
    }

    public void SetSensativity(Single value)
    {
        m_cameraSensativity = value;
    }

    public void SetInvertX(bool invertX)
    {
        m_invert.x = invertX ? 1 : -1;
    }

    public void SetInvertY(bool invertY)
    {
        m_invert.y = invertY ? 1 : -1;
    }

    public void OnMove(InputValue inputValue)
    {
        m_move = inputValue.Get<Vector2>();
    }

    public void OnLook(InputValue inputValue)
    {
        m_look = inputValue.Get<Vector2>();
    }

    public void OnCrouch()
    {
        m_crouch = !m_crouch&&!m_inputLocked;
    }

    public void OnFire(InputValue inputValue)
    {
        m_fire = inputValue.isPressed && !m_inputLocked;
    }

    public void OnRun(InputValue inputValue)
    {
        m_run = inputValue.isPressed && !m_inputLocked;
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
        if (m_input == null)
        {
            m_input = GetComponent<UnityEngine.InputSystem.PlayerInput>();
        }
        m_input.SwitchCurrentActionMap("Fighting");
        UIController.Instance.CurrentGame = GameType.Fighting;
        m_inputLocked = true;
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

    public void OnMenu()
    {
        if (!m_playerDied && !m_inputLocked || m_pause || !m_gameStarted && m_pause)
        {
            ActivateSettings();
        }
    }

    public void ActivateSettings()
    {
        m_pause = UIController.Instance.SetActive();
        Cursor.visible = m_pause||!m_gameStarted;
    }
    /// <summary>
    /// Reset player and enemies,disables die layout
    /// </summary>
    public void OnRestart()
    {
        if (m_playerDied)
        {
            UIController.Instance.DarkenScreen();
            m_playerDied = false;
            UIController.Instance.SetDieLayout(m_playerDied);
        }
    }

    public string GetCurrentControlScheme() => m_input.currentControlScheme;

    public InputAction GetAction(string command) => m_input.actions.FindAction(new string(command.Where(c => !char.IsWhiteSpace(c)).ToArray()));

    public string GetBindingId(string command)
    {
        var action = m_input.actions.FindAction(new string(command.Where(c => !char.IsWhiteSpace(c)).ToArray()));
        int index = action.GetBindingIndex(group: m_input.currentControlScheme);
        return action.bindings[index].id.ToString();
    }
}
