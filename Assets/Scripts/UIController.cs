using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
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
    Text m_subtitlesButtonText;

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
    [SerializeField]
    AudioClip m_dieAudio;

    [Header("")]
    [SerializeField]
    Text[] m_subtitleText;

    [Header("Fighting")]
    [SerializeField]
    Image m_blackScreen;
    [SerializeField]
    Text m_startText;
    [SerializeField]
    Text m_winText;
    [SerializeField]
    AudioClip m_fightingMusic;
    [SerializeField]
    AudioClip m_startAudio;
    [SerializeField]
    AudioClip m_winAudio;
    [SerializeField]
    CommandReceiver m_finalCutscene;

    static UIController m_instance;
    public static UIController Instance => m_instance;
    public bool Subtitles => m_subtitlesOn;
    public GameDifficulty GameDifficulty => m_gameDifficulty;
    public GameType CurrentGame { get; set; } = GameType.Shooter;

    bool m_isActive = false;
    bool m_subtitlesOn = true;
    GameObject m_settings;
    GameDifficulty m_gameDifficulty = GameDifficulty.Normal;
    int m_langIndex = 0;
    bool m_isControlsLayout = false;
    bool m_isGameLayout = false;
    bool m_isAudioLayout = false;

    Color m_screenFadeColor;
    Color m_textFadeColor;
    bool m_darkenScreen = false;
    bool m_startFighting = false;
    readonly float m_fadeTime = 3f;
    bool m_restart = true;
    AudioSource m_gameVoice;

    readonly string[] m_fightingCommands = { "Upper Attack", "Lower Attack", "Upper Block", "Middle Block" };
    readonly string[] m_shooterCommands = { "Aim", "Fire", "Run", "Crouch" };
    readonly string[][] m_subtitles = new string[2][]
    {
        new string[]{
            "I will avnge all the pain that i had to experience!",
            "Long time no see, Pudge!",
            "You killed my famaly and ate my children, bastard!\n\r I will avenge them and cleanse the world of such evil as you, Slenderman!",
        "Let's see who wins!",
        "I have avenged my family and rid the world of evil! Now I can go to them!"},
        new string[]{
        "Я отомщу за всю боль, которую мне пришлось испытать!",
        "Давно не виделись, жирдяй!",
        "Ты убил мою семью и съел моих детей, ублюдок!\n\r Я отомщу за них и очищу мир от такого зла, как ты, Слендермен!",
        "Посмотрим кто кого!",
        "Я отомстил за свою семью и избавил мир ото зла! Теперь я могу отправится к ним!"}
    };

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
        m_gameVoice = GetComponent<AudioSource>();
        m_screenFadeColor = m_blackScreen.color;
        m_screenFadeColor.a = 1f;
        m_textFadeColor = m_startText.color;
        m_textFadeColor.a = 0f;
    }

    private void Update()
    {
        if (m_darkenScreen)
        {
            m_blackScreen.color = Color.Lerp(m_blackScreen.color, m_screenFadeColor, Time.deltaTime * m_fadeTime);
            if (m_blackScreen.color.a >= 0.99f)
            {
                m_darkenScreen = false;
                if (m_restart)
                {
                    StartFighting();
                }
                else
                {
                    m_winText.gameObject.SetActive(false);
                    m_finalCutscene.Receive(Vector3.zero);
                }
            }
        }

        if (m_startFighting && m_blackScreen.isActiveAndEnabled)
        {
            m_blackScreen.color = Color.Lerp(m_blackScreen.color, m_screenFadeColor, Time.deltaTime * m_fadeTime);
            if (m_blackScreen.color.a < 0.01f)
            {
                m_blackScreen.gameObject.SetActive(false);
                m_screenFadeColor.a = 1f;
                m_gameVoice.PlayOneShot(m_startAudio);
            }
        }
        //if screen is not black anymore, fades away start text and unlock player input in th end
        if (m_startFighting && !m_blackScreen.isActiveAndEnabled && m_startText.isActiveAndEnabled)
        {
            m_startText.color = Color.Lerp(m_startText.color, m_textFadeColor, Time.deltaTime * m_fadeTime);
            if (m_startText.color.a < 0.01f)
            {
                m_startText.gameObject.SetActive(false);
                GameObject.FindGameObjectWithTag("Player").GetComponent<FightingPlayerController>().StartGame();
                m_startFighting = false;
            }
        }
    }

    public void StartFighting()
    {
        m_startFighting = true;
        m_startText.gameObject.SetActive(true);
        Color textColor = m_startText.color;
        textColor.a = 1;
        m_startText.color = textColor;
        m_screenFadeColor.a = 0f;
        m_restart = false;
        Camera.main.GetComponent<AudioSource>().clip = m_fightingMusic;
    }

    public IEnumerator Win()
    {
        m_winText.gameObject.SetActive(true);
        m_gameVoice.PlayOneShot(m_winAudio);
        yield return new WaitForSeconds(m_fadeTime);
        DarkenScreen();
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

    public void ChangeLanguage(int index)
    {
        m_langIndex = index;
        TurnSubtitles();
        m_subtitlesOn = !m_subtitlesOn;
    }

    public void TurnSubtitles()
    {
        if (m_subtitlesOn)
        {
            m_subtitlesButtonText.text = "Off";
            foreach (Text subtitle in m_subtitleText)
            {
                subtitle.text = "";
            }
        }
        else
        {
            m_subtitlesButtonText.text = "On";
            for (int i = 0; i < m_subtitleText.Length; i++)
            {
                m_subtitleText[i].text = m_subtitles[m_langIndex][i];
            }
        }
        m_subtitlesOn = !m_subtitlesOn;
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
            rebind.actionReference = InputActionReference.Create(m_input.GetAction(m_commands[i].text));
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
        m_restart = true;
        m_gameVoice.PlayOneShot(m_dieAudio);

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

    public void DarkenScreen()
    {
        m_darkenScreen = true;
        m_blackScreen.gameObject.SetActive(true);
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
