using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.UI;

public enum GameDifficulty
{
    Normal,
    Hard
}

public enum GameType
{
    Shooter, 
    Fighting
}

public class UIController : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField]
    Button m_gameButton;
    [SerializeField]
    Button m_audioButton;
    [SerializeField]
    Button m_controlsButton;

    [Header("Layouts")]
    [SerializeField]
    GameObject m_audioLayout;
    [SerializeField]
    GameObject m_gameLayout;
    [SerializeField]
    GameObject m_controlsLayout;

    [Header("Game Components")]
    [SerializeField]
    PlayerInput m_input;
    [SerializeField]
    AudioMixer m_mixer;

    [Header("Game Layer Components")]
    [SerializeField]
    Button m_normal;
    [SerializeField]
    Button m_hard;
    [SerializeField]
    Button m_low;
    [SerializeField]
    Button m_medium;
    [SerializeField]
    Button m_high;

    [Header("Audio Layer Components")]
    [SerializeField]
    Slider m_gameVolumeSlider;
    [SerializeField]
    Text m_subtitlesText;

    [Header("Controls Layer Components")]
    [SerializeField]
    Button[] m_buttons;
    [SerializeField]
    Text[] m_commands;

    [Header("Die Layout")]
    [SerializeField]
    GameObject m_dieLayout;
    [SerializeField]
    Text m_restartPressText;

    static UIController m_instance;
    public static UIController Instance => m_instance;
    public bool Subtitles => m_subtitles;
    public GameDifficulty GameDifficulty => m_gameDifficulty;
    public GameType CurrentGame { get; set; } = GameType.Shooter;

    bool m_isActive = false;
    bool m_subtitles = true;
    GameObject m_settings;
    GameDifficulty m_gameDifficulty = GameDifficulty.Normal;
    bool m_isControlsLayout = false;
    bool m_isGameLayout = false;
    bool m_isAudioLayout = false;

    readonly string[] m_fightingCommands = { "Upper Attack", "Lower Attack", "Upper Block", "Middle Block" };
    readonly string[] m_shooterCommands = { "Aim", "Fire", "Run", "Crouch" };

    private void Awake()
    {
        if (m_instance == null)
        {
            m_instance = this;
        }
    }

    private void Start()
    {
        m_settings = transform.GetChild(2).gameObject;
    }

    public bool SetActive()
    {
        m_isActive = !m_isActive;
        Time.timeScale += m_isActive ? -1 : 1;
        m_settings.SetActive(m_isActive);
        SetGameLayout();
        return m_isActive;
    }

    public void Close()
    {
        SetActive();
    }

    public void ChangeGameVolume(Single value)
    {
        m_mixer.SetFloat("MasterVolume", Mathf.Log10(value) * 10);
    }

    public void ChangeMusicVolume(Single value)
    {
        m_mixer.SetFloat("MusicVolume", Mathf.Log10(value) * 10);
    }
    public void ChangeEffectsVolume(Single value)
    {
        m_mixer.SetFloat("SFXVolume", Mathf.Log10(value) * 10);
    }

    public void TurnSubtitles()
    {
        if (m_subtitles)
        {
            m_subtitlesText.text = "Off";
        }
        else
        {
            m_subtitlesText.text = "On";
        }
        m_subtitles = !m_subtitles;
    }

    public void SetNormalDifficulty()
    {
        m_gameDifficulty = GameDifficulty.Normal;
        m_normal.gameObject.GetComponent<Image>().color = m_normal.colors.pressedColor;
        m_hard.gameObject.GetComponent<Image>().color = m_hard.colors.disabledColor;
    }

    public void SetHardDifficulty()
    {
        m_gameDifficulty = GameDifficulty.Hard;
        m_hard.gameObject.GetComponent<Image>().color = m_hard.colors.pressedColor;
        m_normal.gameObject.GetComponent<Image>().color = m_normal.colors.disabledColor;
    }

    public void SetLowQuality()
    {
        QualitySettings.SetQualityLevel(0, true);

        m_low.gameObject.GetComponent<Image>().color = m_low.colors.pressedColor;
        m_medium.gameObject.GetComponent<Image>().color = m_medium.colors.disabledColor;
        m_hard.gameObject.GetComponent<Image>().color = m_high.colors.disabledColor;
    }

    public void SetMediumQuality()
    {
        QualitySettings.SetQualityLevel(1, true);

        m_low.gameObject.GetComponent<Image>().color = m_low.colors.disabledColor;
        m_medium.gameObject.GetComponent<Image>().color = m_medium.colors.pressedColor;
        m_hard.gameObject.GetComponent<Image>().color = m_high.colors.disabledColor;
    }

    public void SetHighQuality()
    {
        QualitySettings.SetQualityLevel(2, true);

        m_low.gameObject.GetComponent<Image>().color = m_low.colors.disabledColor;
        m_medium.gameObject.GetComponent<Image>().color = m_medium.colors.disabledColor;
        m_hard.gameObject.GetComponent<Image>().color = m_high.colors.pressedColor;
    }

    public void SetGameLayout()
    {
        if (!m_gameLayout.activeInHierarchy)
        {

            m_isGameLayout = true;
            m_isControlsLayout = m_isAudioLayout = false;

            SetLayouts();

            if (m_gameDifficulty == GameDifficulty.Normal)
            {
                SetNormalDifficulty();
            }
            else
            {
                SetHardDifficulty();
            }
            m_normal.Select();

            switch (QualitySettings.GetQualityLevel())
            {
                case 0:
                    SetLowQuality();
                    break;
                case 1:
                    SetMediumQuality();
                    break;
                case 2:
                    SetHighQuality();
                    break;
            }
        }
    }

    public void SetAudioLayout()
    {
        if (!m_audioLayout.activeInHierarchy)
        {
            m_isAudioLayout = true;
            m_isGameLayout = m_isControlsLayout = false;

            SetLayouts();

            m_gameVolumeSlider.Select();
        }
    }

    public void SetControlsLayout()
    {
        if (!m_controlsLayout.activeInHierarchy)
        {
            m_isControlsLayout = true;
            m_isGameLayout = m_isAudioLayout = false;

            SetLayouts();

            SetControls();

            m_buttons[0].Select();
        }
    }

    void SetControls()
    {
        for (int i = 0; i < m_commands.Length; i++)
        {
            m_commands[i].text = CurrentGame == GameType.Fighting ? m_fightingCommands[i] : m_shooterCommands[i];
            var rebind = m_buttons[i].GetComponentInParent<RebindActionUI>();
            var actionReferece = new InputActionReference();
            actionReferece.Set(m_input.GetAction(m_commands[i].text));
            rebind.actionReference = actionReferece;
            rebind.bindingId = m_input.GetBindingId(m_commands[i].text);
        }
    }

    void SetLayouts()
    {
        m_gameButton.gameObject.GetComponent<Image>().sprite = m_isGameLayout ? m_gameButton.spriteState.selectedSprite : m_gameButton.spriteState.disabledSprite;
        m_audioButton.gameObject.GetComponent<Image>().sprite = m_isAudioLayout ? m_audioButton.spriteState.selectedSprite : m_audioButton.spriteState.disabledSprite;
        m_controlsButton.gameObject.GetComponent<Image>().sprite = m_isControlsLayout ? m_controlsButton.spriteState.selectedSprite : m_controlsButton.spriteState.disabledSprite;
        m_gameLayout.SetActive(m_isGameLayout);
        m_audioLayout.SetActive(m_isAudioLayout);
        m_controlsLayout.SetActive(m_isControlsLayout);
    }

    public void CancelLayout(InputAction.CallbackContext context)
    {
        if (m_isGameLayout)
        {
            m_gameButton.gameObject.GetComponent<Image>().sprite = m_gameButton.spriteState.disabledSprite;
            m_gameButton.Select();
        }
        else if (m_isAudioLayout)
        {
            m_audioButton.gameObject.GetComponent<Image>().sprite = m_audioButton.spriteState.disabledSprite;
            m_audioButton.Select();
        }
        else if (m_controlsLayout)
        {
            m_controlsButton.gameObject.GetComponent<Image>().sprite = m_controlsButton.spriteState.disabledSprite;
            m_controlsButton.Select();
        }

        m_isGameLayout = m_isAudioLayout = m_isControlsLayout = false;
    }

    public void SetDieLayout(bool set)
    {
        m_dieLayout.SetActive(set);

        if (set)
        {
            if (m_input.GetCurrentControlScheme() == "Gamepad")
            {
                m_restartPressText.text = "Press A to restart";
            }
            else
            {
                m_restartPressText.text = "Press ENTER to restart";
            }
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
				Application.Quit();
#endif
    }

}
