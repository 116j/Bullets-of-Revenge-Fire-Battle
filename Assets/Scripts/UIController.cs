using Cinemachine;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Samples.RebindUI;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
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
    [Header("Buttons")]
    [SerializeField]
    GameObject m_startMenuButtons;
    [SerializeField]
    GameObject m_settingButtons;

    [Header("Menu Buttons")]
    [SerializeField]
    Button m_gameButton;
    [SerializeField]
    Button m_audioButton;
    [SerializeField]
    Button m_controlsButton;
    [SerializeField]
    Text m_header;

    [Header("Layouts")]
    [SerializeField]
    GameObject m_audioLayout;
    [SerializeField]
    GameObject m_gameLayout;
    [SerializeField]
    GameObject m_controlsLayout;
    [SerializeField]
    GameObject m_startLayout;

    [Header("Game Components")]
    [SerializeField]
    PlayerInput m_input;
    [SerializeField]
    AudioMixer m_mixer;
    [SerializeField]
    GameObject m_cutscenes;

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
    [SerializeField]
    Dropdown m_language;

    [Header("Controls Layer Components")]
    [SerializeField]
    Button[] m_buttons;
    [SerializeField]
    Text[] m_commands;

    [Header("Die Layout")]
    [SerializeField]
    GameObject m_dieLayout;

    [Header("Subtitles")]
    [SerializeField]
    Text[] m_subtitleText;

    [Header("Transcription")]
    [SerializeField]
    Image m_gameName;
    [SerializeField]
    Sprite m_gameNameRus;
    [SerializeField]
    Sprite m_gameNameEng;
    [SerializeField]
    PlayableAsset[] m_timelinesRus;
    [SerializeField]
    PlayableAsset[] m_timelinesEng;
    [SerializeField]
    AudioClip m_startAudioEng;
    [SerializeField]
    AudioClip m_startAudioRus;
    [SerializeField]
    AudioClip m_winAudioEng;
    [SerializeField]
    AudioClip m_winAudioRus;
    [SerializeField]
    AudioClip m_dieAudioEng;
    [SerializeField]
    AudioClip m_dieAudioRus;

    [Header("Fighting")]
    [SerializeField]
    CinemachineVirtualCamera m_fightingCamera;
    [SerializeField]
    Image m_blackScreen;
    [SerializeField]
    Text m_startText;
    [SerializeField]
    Text m_winText;
    [SerializeField]
    AudioClip m_fightingMusic;
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
    Text m_restartText;
    Text m_dieText;
    bool m_darkenScreen = false;
    bool m_startFighting = false;
    bool m_startShooter = false;
    readonly float m_fadeTime = 1.5f;
    bool m_restart = false;
    AudioSource m_gameVoice;
    CinemachineFramingTransposer m_transposer;

    readonly string[][] m_fightingCommands = {
    new string[]
    {
        "Upper Attack", "Lower Attack", "Upper Block", "Middle Block"
    },
    new string[]
    {
        "Верхняя атака", "Нижняя атака", "Верхний блок", "Нижний блок"
    }
    };
    readonly string[][] m_shooterCommands = {
    new string[]
    {
        "Aim", "Fire", "Run", "Crouch"
    },
    new string[]
    {
        "Целиться", " Стрелять", "Бежать", "Присесть"
    }
    };
    readonly string[][] m_startButtonsText =
{
        new string[]
        {
            "Strat","Settings","Quit"
        },
        new string[]
        {
            "Начать","Настройки","Выйти"
        }
    };
    readonly string[][] m_settingsButtonsText =
    {
        new string[]
        {
            "Game","Audio","Controls","Quit Game"
        },
        new string[]
        {
            "Игра","Звук","Управление","Выйти из игры"
        }
    };
    readonly string[][] m_gameplayElementsText =
    {
        new string[]
        {
            "Difficulty","Normal","Hard","Camera Turn Sensativity", "Quality","Low","Medium","High","Invert X","Invert Y"
        },
        new string[]
        {
            "Сложность", "Нормально","Сложно","Поворот камеры","Качество","Низкое","Среднее","Высокое","Инверсия X","Инверсия Y"
        }
    };
    readonly string[][] m_audioElementsText =
    {
        new string[]
        {
            "Game","Music","Effects","Language","Suntitles"
        },
        new string[]
        {
            "Игра","Музыка","Эффекты","Язык","Субтитры"
        }
    };
    readonly string[][] m_subtitles = {
        new string[]{
            "I will avenge all the pain that i had to experience!",
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
        m_transposer = m_fightingCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
        m_restartText = m_dieLayout.transform.GetChild(1).GetComponent<Text>();
        m_dieText = m_dieLayout.transform.GetChild(0).GetComponent<Text>();
        m_gameVoice = GetComponent<AudioSource>();
        m_screenFadeColor = m_blackScreen.color;
        m_screenFadeColor.a = 1f;
        m_textFadeColor = m_startText.color;
        m_textFadeColor.a = 0f;

        if (Application.systemLanguage == SystemLanguage.Russian)
        {
            m_language.value = 1;
        }
        else
        {
            m_language.value = 0;
        }
    }

    private void LateUpdate()
    {
        if (m_darkenScreen)
        {
            m_blackScreen.color = Color.Lerp(m_blackScreen.color, m_screenFadeColor, Time.deltaTime * m_fadeTime);
            if (m_blackScreen.color.a >= 0.99f)
            {
                m_blackScreen.color = m_screenFadeColor;

                m_darkenScreen = false;
                if (m_restart && CurrentGame == GameType.Fighting)
                {
                    StartFighting();
                }
                else if (m_restart && CurrentGame == GameType.Shooter)
                {
                    StartShooter();
                }
                else
                {
                    Camera.main.GetComponent<AudioSource>().Stop();
                    m_winText.gameObject.SetActive(false);
                    m_finalCutscene.Receive(transform);
                }
            }
        }

        if ((m_startFighting || m_startShooter) && m_blackScreen.isActiveAndEnabled)
        {
            m_blackScreen.color = Color.Lerp(m_blackScreen.color, m_screenFadeColor, Time.deltaTime * m_fadeTime);
            if (m_blackScreen.color.a < 0.1f)
            {
                m_blackScreen.gameObject.SetActive(false);
                m_screenFadeColor.a = 1f;
                if (m_startFighting)
                    m_gameVoice.PlayOneShot(m_langIndex == 0 ? m_startAudioEng : m_startAudioRus);            
                else if (m_startShooter)
                {
                    m_startShooter = false;
                    m_input.LockInput();
                }
            }
        }
        //if screen is not black anymore, fades away start text and unlock player input in th end
        if (m_startFighting && !m_blackScreen.isActiveAndEnabled && m_startText.isActiveAndEnabled)
        {
            m_startText.color = Color.Lerp(m_startText.color, m_textFadeColor, Time.deltaTime * m_fadeTime);
            if (m_startText.color.a < 0.1f)
            {
                if (m_restart)
                {
                    m_transposer.m_TrackedObjectOffset = new Vector3(0f, m_transposer.m_TrackedObjectOffset.y, m_transposer.m_TrackedObjectOffset.z - 2f);
                    m_restart = false;
                }
                m_startText.gameObject.SetActive(false);
                GameObject.FindGameObjectWithTag("Player").GetComponent<FightingPlayerController>().StartGame();
                m_startFighting = false;
            }
        }
    }

    public void StartFighting()
    {
        if (m_restart)
        {
            GameObject.FindGameObjectWithTag("Player").GetComponent<FightingPlayerController>().Reset();
            GameObject.FindGameObjectWithTag("Enemy").GetComponent<FightingSlenerAI>().Reset();
            m_transposer.m_TrackedObjectOffset = new Vector3(0f, m_transposer.m_TrackedObjectOffset.y, m_transposer.m_TrackedObjectOffset.z + 2f);
        }
        else
        {
            m_fightingCamera.gameObject.SetActive(true);
            EnemySpawner.Instance.DestroyAll();
        }
        m_startText.gameObject.SetActive(true);
        Color textColor = m_startText.color;
        textColor.a = 1;
        m_startText.color = textColor;
        m_screenFadeColor.a = 0f;
        m_startFighting = true;
        Camera.main.GetComponent<AudioSource>().clip = m_fightingMusic;
        Camera.main.GetComponent<AudioSource>().Play();
    }

    void StartShooter()
    {
        m_startShooter = true;
        m_screenFadeColor.a = 0f;
        m_restart = false;

        GameObject.FindGameObjectWithTag("Player").GetComponent<ShooterPlayerController>().Reset();

        foreach (OnTriggerSend trigger in GameObject.Find("TriggerZones").GetComponentsInChildren<OnTriggerSend>())
        {
            trigger.Reset();
        }

        EnemySpawner.Instance.DestroyAll();
    }

    public IEnumerator Win()
    {
        m_winText.gameObject.SetActive(true);
        m_gameVoice.PlayOneShot(m_langIndex == 0 ? m_winAudioEng : m_winAudioRus);
        yield return new WaitForSeconds(m_fadeTime);
        DarkenScreen();
    }

    public bool SetActive()
    {
        m_isActive = !m_isActive;
        Time.timeScale += m_isActive ? -1 : 1;
        m_settings.SetActive(m_isActive);
        m_startLayout.SetActive(!m_isActive && !m_input.GameStrted);
        if (m_isActive)
            SetGameLayout();
        return m_isActive;
    }

    public void Close()
    {
        m_input.ActivateSettings();
    }

    public void ChangeGameVolume(Single value)
    {
        m_mixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
    }

    public void ChangeMusicVolume(Single value)
    {
        m_mixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
    }
    public void ChangeEffectsVolume(Single value)
    {
        m_mixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
    }

    public void ChangeLanguage(int index)
    {
        m_langIndex = index;
        m_subtitlesOn = !m_subtitlesOn;
        TurnSubtitles();
        int i = 0;
        foreach (var button in m_settingButtons.GetComponentsInChildren<Text>())
        {
            button.text = m_settingsButtonsText[index][i];
            i++;
        }

        i = 0;
        foreach (var button in m_startMenuButtons.GetComponentsInChildren<Text>())
        {
            button.text = m_startButtonsText[index][i];
            i++;
        }
        int j = 0;

        for (i = 0; i < m_gameLayout.transform.childCount; i++)
        {
            foreach (var button in m_gameLayout.transform.GetChild(i).GetComponentsInChildren<Text>())
            {
                button.text = m_gameplayElementsText[index][j];
                j++;
            }
        }

        for (i = 0; i < m_audioLayout.transform.childCount; i++)
        {
            m_audioLayout.transform.GetChild(i).GetComponentInChildren<Text>().text = m_audioElementsText[index][i];
        }

        m_header.text = index == 0 ? "SETTINGS" : "НАСТРОЙКИ";
        m_gameName.sprite = index == 0 ? m_gameNameEng : m_gameNameRus;
        m_startText.text = index == 0 ? "Start\n\rfighting" : "Начинайте\n\rбой";
        m_winText.text = index == 0 ? "Player\n\rwins!" : "Игрок\n\rпобедил!";
        m_dieText.text = index == 0 ? "YOU\n\rDIED" : "ВЫ\n\rУМЕРЛИ";

        PlayableAsset[] cutscenes = index == 0 ? m_timelinesEng : m_timelinesRus;
        for (i = 0; i < cutscenes.Length; i++)
        {
            m_cutscenes.transform.GetChild(i).GetComponent<PlayableDirector>().playableAsset = cutscenes[i];
        }
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
        var normalColors = m_normal.colors;
        var hardColors = m_normal.colors;
        normalColors.normalColor = normalColors.selectedColor;
        hardColors.normalColor = hardColors.disabledColor;
        m_hard.colors = hardColors;
        m_normal.colors = normalColors;
    }

    public void SetHardDifficulty()
    {
        m_gameDifficulty = GameDifficulty.Hard;
        var normalColors = m_normal.colors;
        var hardColors = m_normal.colors;
        normalColors.normalColor = normalColors.disabledColor;
        hardColors.normalColor = hardColors.selectedColor;
        m_hard.colors = hardColors;
        m_normal.colors = normalColors;
    }

    public void SetLowQuality()
    {
        QualitySettings.SetQualityLevel(0, true);

        var lowColors = m_low.colors;
        var otherColors = m_high.colors;
        lowColors.normalColor = lowColors.selectedColor;
        otherColors.normalColor = otherColors.disabledColor;
        m_low.colors = lowColors;
        m_high.colors = otherColors;
        m_medium.colors = otherColors;
    }

    public void SetMediumQuality()
    {
        QualitySettings.SetQualityLevel(1, true);

        var mediumColors = m_medium.colors;
        var otherColors = m_high.colors;
        mediumColors.normalColor = mediumColors.selectedColor;
        otherColors.normalColor = otherColors.disabledColor;
        m_medium.colors = mediumColors;
        m_high.colors = otherColors;
        m_low.colors = otherColors;
    }

    public void SetHighQuality()
    {
        QualitySettings.SetQualityLevel(2, true);

        var highColors = m_high.colors;
        var otherColors = m_medium.colors;
        highColors.normalColor = highColors.selectedColor;
        otherColors.normalColor = otherColors.disabledColor;
        m_high.colors = highColors;
        m_low.colors = otherColors;
        m_medium.colors = otherColors;
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
            m_commands[i].text = CurrentGame == GameType.Fighting ? m_fightingCommands[m_langIndex][i] : m_shooterCommands[m_langIndex][i];
            string commandName = CurrentGame == GameType.Fighting ? m_fightingCommands[0][i] : m_shooterCommands[0][i];
            var rebind = m_buttons[i].GetComponentInParent<RebindActionUI>();
            rebind.actionReference = InputActionReference.Create(m_input.GetAction(commandName));
            rebind.bindingId = m_input.GetBindingId(commandName);
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
        else if (m_input.GameStrted)
        {
            m_input.ActivateSettings();
        }

        m_isGameLayout = m_isAudioLayout = m_isControlsLayout = false;
    }

    public void SetDieLayout(bool set)
    {
        m_dieLayout.SetActive(set);
        m_restart = true;
        if (set)
            m_gameVoice.PlayOneShot(m_langIndex == 0 ? m_dieAudioEng : m_dieAudioRus);

        if (set)
        {
            if (m_input.GetCurrentControlScheme() == "Gamepad")
            {
                m_restartText.text = m_langIndex == 0 ? "Press A to restart" : "Нажмите А, чтобы начать заново";
            }
            else
            {
                m_restartText.text = m_langIndex == 0 ? "Press ENTER to restart" : "Нажмите ENTER, чтобы начать заново";
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


    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
