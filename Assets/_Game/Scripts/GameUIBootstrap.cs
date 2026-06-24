using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameUIBootstrap : MonoBehaviour
{
    private const string RootName = "Runtime Game UI";
    private static GameUIBootstrap Instance { get; set; }

    public static bool BlocksPauseToggle => Instance != null && Instance.settingsOpen;
    public static bool AllowsGameplayPause => Instance == null || Instance.missionStarted;

    private static readonly Color DeepSpace = new Color(0.01f, 0.015f, 0.025f, 0.84f);
    private static readonly Color PanelGlass = new Color(0.035f, 0.055f, 0.075f, 0.78f);
    private static readonly Color Cyan = new Color(0.1f, 0.92f, 1f, 1f);
    private static readonly Color SoftCyan = new Color(0.35f, 0.9f, 1f, 0.92f);
    private static readonly Color Amber = new Color(1f, 0.66f, 0.18f, 1f);
    private static readonly Color MutedText = new Color(0.64f, 0.78f, 0.84f, 0.95f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateUI()
    {
        if (FindFirstObjectByType<GameUIBootstrap>() != null)
        {
            return;
        }

        GameObject root = new GameObject(RootName);
        DontDestroyOnLoad(root);
        root.AddComponent<GameUIBootstrap>();
    }

    private PlayerRigidbodyMovement player;
    private Texture2D whiteTexture;
    private Texture2D menuBackground;
    private GUIStyle titleStyle;
    private GUIStyle largeStyle;
    private GUIStyle labelStyle;
    private GUIStyle smallStyle;
    private GUIStyle missionTitleStyle;
    private GUIStyle hintStyle;
    private GUIStyle controlsStyle;
    private GUIStyle commandTitleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle buttonStyle;
    private AudioSource ambientMusic;
    private bool missionStarted;
    private bool settingsOpen;
    private bool musicEnabled = true;
    private bool specialEffectsEnabled = true;
    private float musicVolume = 0.35f;
    private float effectsIntensity = 0.8f;

    private void Awake()
    {
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded;
        whiteTexture = Texture2D.whiteTexture;
        LoadMenuBackground();
        CreateAmbientMusic();
        FindPlayer();
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (settingsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            settingsOpen = false;
        }

        if (player == null)
        {
            FindPlayer();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayer();
        ShowMainMenu();
    }

    private void FindPlayer()
    {
        player = FindFirstObjectByType<PlayerRigidbodyMovement>();
    }

    private void BuildStyles()
    {
        titleStyle = CreateStyle(15, FontStyle.Bold, Cyan, TextAnchor.UpperLeft);
        largeStyle = CreateStyle(34, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        labelStyle = CreateStyle(18, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        smallStyle = CreateStyle(14, FontStyle.Normal, MutedText, TextAnchor.UpperLeft);
        missionTitleStyle = CreateStyle(15, FontStyle.Bold, Amber, TextAnchor.UpperLeft);
        hintStyle = CreateStyle(13, FontStyle.Bold, SoftCyan, TextAnchor.MiddleRight);
        controlsStyle = CreateStyle(15, FontStyle.Bold, MutedText, TextAnchor.MiddleCenter);
        commandTitleStyle = CreateStyle(34, FontStyle.Bold, Cyan, TextAnchor.MiddleCenter);
        subtitleStyle = CreateStyle(14, FontStyle.Bold, Amber, TextAnchor.MiddleCenter);

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 18;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = Cyan;
        buttonStyle.active.textColor = Amber;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.padding = new RectOffset(12, 12, 8, 8);
    }

    private GUIStyle CreateStyle(int size, FontStyle style, Color color, TextAnchor alignment)
    {
        GUIStyle guiStyle = new GUIStyle(GUI.skin.label);
        guiStyle.fontSize = size;
        guiStyle.fontStyle = style;
        guiStyle.normal.textColor = color;
        guiStyle.alignment = alignment;
        guiStyle.wordWrap = false;
        return guiStyle;
    }

    private void OnGUI()
    {
        if (titleStyle == null)
        {
            BuildStyles();
        }

        if (!missionStarted)
        {
            if (settingsOpen)
            {
                DrawSettingsMenu(false);
            }
            else
            {
                DrawMainMenu();
            }

            return;
        }

        DrawGameplayStatusPanel();

        if (IsGamePaused())
        {
            if (settingsOpen)
            {
                DrawSettingsMenu(true);
            }
            else
            {
                DrawPauseMenu();
            }
        }
    }

    private void DrawMainMenu()
    {
        DrawMenuBackdrop(0.52f);

        Rect panel = new Rect((Screen.width - 560f) * 0.5f, (Screen.height - 430f) * 0.5f, 560f, 430f);
        DrawPanel(panel, new Color(0.004f, 0.015f, 0.02f, 0.78f), Cyan);

        GUIStyle mainTitle = CreateStyle(38, FontStyle.Bold, Cyan, TextAnchor.MiddleCenter);
        GUIStyle mainSubtitle = CreateStyle(15, FontStyle.Bold, Amber, TextAnchor.MiddleCenter);

        GUI.Label(new Rect(panel.x + 44f, panel.y + 46f, 472f, 52f), "ORBITAL TRAINING", mainTitle);
        GUI.Label(new Rect(panel.x + 44f, panel.y + 96f, 472f, 28f), "EVA MOBILITY SIMULATION", mainSubtitle);

        if (DrawHologramButton(new Rect(panel.x + 110f, panel.y + 188f, 340f, 50f), "START MISSION"))
        {
            StartMission();
        }

        if (DrawHologramButton(new Rect(panel.x + 110f, panel.y + 268f, 340f, 50f), "SETTINGS"))
        {
            settingsOpen = true;
        }

        if (DrawHologramButton(new Rect(panel.x + 110f, panel.y + 348f, 340f, 50f), "EXIT"))
        {
            ExitPlayMode();
        }
    }

    private void DrawSettingsMenu(bool fromPause)
    {
        Matrix4x4 previousMatrix = GUI.matrix;
        const float virtualWidth = 1366f;
        const float virtualHeight = 768f;
        float scale = Mathf.Min(Screen.width / virtualWidth, Screen.height / virtualHeight);
        Vector3 offset = new Vector3((Screen.width - virtualWidth * scale) * 0.5f, (Screen.height - virtualHeight * scale) * 0.5f, 0f);
        GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, new Vector3(scale, scale, 1f));

        DrawMenuBackdropVirtual(0.18f);

        Rect panel = new Rect(258f, 126f, 858f, 538f);
        Rect titlePlate = new Rect(474f, 30f, 418f, 82f);

        DrawPanel(panel, new Color(0.01f, 0.055f, 0.075f, 0.62f), Cyan);
        DrawPanel(titlePlate, new Color(0.02f, 0.09f, 0.12f, 0.72f), Cyan);

        GUIStyle title = CreateStyle(42, FontStyle.Bold, SoftCyan, TextAnchor.MiddleCenter);
        GUIStyle section = CreateStyle(30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        GUIStyle rowTitle = CreateStyle(25, FontStyle.Bold, SoftCyan, TextAnchor.UpperLeft);
        GUIStyle checkboxStyle = CreateStyle(23, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);

        GUI.Label(titlePlate, "SETTINGS", title);
        GUI.Label(new Rect(panel.x + 42f, panel.y + 20f, panel.width - 84f, 44f), "SOUND", section);
        DrawRect(new Rect(panel.x + 42f, panel.y + 66f, panel.width - 84f, 2f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.74f));

        float rowTop = 236f;
        float effectsTop = 462f;
        float left = 324f;
        GUI.Label(new Rect(left, rowTop - 50f, 390f, 36f), "BACKGROUND MUSIC", rowTitle);
        DrawIconBox(new Rect(left, rowTop, 142f, 142f), true);

        musicVolume = DrawHologramSlider(new Rect(574f, 258f, 482f, 74f), musicVolume);
        bool musicMuted = !musicEnabled;
        musicMuted = DrawHologramCheckbox(new Rect(579f, 357f, 34f, 34f), new Rect(579f, 354f, 420f, 42f), musicMuted);
        musicEnabled = !musicMuted;
        GUI.Label(new Rect(626f, 354f, 370f, 42f), "MUTE MUSIC", checkboxStyle);

        GUI.Label(new Rect(left, effectsTop - 50f, 390f, 36f), "SPECIAL EFFECTS", rowTitle);
        DrawIconBox(new Rect(left, effectsTop, 142f, 142f), false);
        effectsIntensity = DrawHologramSlider(new Rect(574f, 482f, 482f, 74f), effectsIntensity);
        bool effectsMuted = !specialEffectsEnabled;
        effectsMuted = DrawHologramCheckbox(new Rect(579f, 582f, 34f, 34f), new Rect(579f, 579f, 420f, 42f), effectsMuted);
        specialEffectsEnabled = !effectsMuted;
        GUI.Label(new Rect(626f, 579f, 370f, 42f), "MUTE EFFECTS", checkboxStyle);

        ApplyAudioSettings();

        if (DrawHologramButton(new Rect(960f, 683f, 154f, 58f), "[BACK]"))
        {
            settingsOpen = false;
        }

        GUI.matrix = previousMatrix;
    }

    private void DrawGameplayStatusPanel()
    {
        Rect panel = new Rect(28f, 28f, 320f, 138f);
        DrawPanel(panel, PanelGlass, Cyan);

        GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, 280f, 22f), "MOVEMENT STATUS", titleStyle);

        string speed = player == null ? "--" : player.CurrentSpeed.ToString("0.00");
        GUI.Label(new Rect(panel.x + 18f, panel.y + 36f, 286f, 36f), "SPD " + speed + " m/s", CreateStyle(28, FontStyle.Bold, Color.white, TextAnchor.UpperLeft));

        Color modifierColor = GetModifierColor();
        DrawRect(new Rect(panel.x + 18f, panel.y + 78f, 8f, 24f), modifierColor);
        GUI.Label(new Rect(panel.x + 34f, panel.y + 75f, 270f, 28f), "MOD: " + GetModifierLabel(), labelStyle);

        GUI.Label(new Rect(panel.x + 18f, panel.y + 106f, 286f, 20f), "TIMER: " + GetModifierTimerText(), smallStyle);
    }

    private bool DrawHologramButton(Rect rect, string label)
    {
        Vector2 mousePosition = Event.current.mousePosition;
        bool hover = rect.Contains(mousePosition);

        Color fill = hover
            ? new Color(0.05f, 0.32f, 0.4f, 0.42f)
            : new Color(0.01f, 0.08f, 0.11f, 0.32f);
        Color outline = hover ? new Color(0.72f, 1f, 1f, 1f) : new Color(Cyan.r, Cyan.g, Cyan.b, 0.88f);

        DrawRect(rect, fill);
        DrawBorder(rect, outline, 2f);
        DrawCornerBrackets(rect, outline);

        bool clicked = GUI.Button(rect, GUIContent.none, GUIStyle.none);

        GUIStyle labelButtonStyle = CreateStyle(18, FontStyle.Bold, hover ? Color.white : SoftCyan, TextAnchor.MiddleCenter);
        GUI.Label(rect, label, labelButtonStyle);

        return clicked;
    }

    private float DrawHologramSlider(Rect rect, float value)
    {
        Rect hitRect = new Rect(rect.x, rect.y + 8f, rect.width, 38f);
        Event current = Event.current;
        Vector2 mousePosition = current.mousePosition;

        if ((current.type == EventType.MouseDown || current.type == EventType.MouseDrag) && hitRect.Contains(mousePosition))
        {
            value = Mathf.Clamp01((mousePosition.x - rect.x) / rect.width);
            current.Use();
        }

        Rect track = new Rect(rect.x, rect.y + 19f, rect.width, 18f);
        DrawRect(track, new Color(0f, 0.13f, 0.17f, 0.96f));
        DrawBorder(track, new Color(Cyan.r, Cyan.g, Cyan.b, 0.75f), 2f);

        float fillWidth = track.width * value;
        DrawRect(new Rect(track.x + 3f, track.y + 3f, Mathf.Max(0f, fillWidth - 6f), track.height - 6f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.72f));
        DrawRect(new Rect(track.x + 3f, track.y + 6f, Mathf.Max(0f, fillWidth - 6f), 3f), new Color(0.78f, 1f, 1f, 0.95f));

        for (int i = 0; i <= 40; i++)
        {
            float x = track.x + (track.width / 40f) * i;
            float height = i % 5 == 0 ? 12f : 7f;
            DrawRect(new Rect(x, track.yMax + 8f, 1f, height), new Color(Cyan.r, Cyan.g, Cyan.b, 0.55f));
        }

        float knobX = track.x + track.width * value;
        Rect knob = new Rect(knobX - 13f, track.y - 8f, 26f, 40f);
        DrawRect(knob, new Color(0.75f, 1f, 1f, 0.88f));
        DrawBorder(knob, new Color(Cyan.r, Cyan.g, Cyan.b, 1f), 2f);

        GUIStyle percentStyle = CreateStyle(12, FontStyle.Normal, MutedText, TextAnchor.UpperLeft);
        GUI.Label(new Rect(rect.x, track.yMax + 22f, 60f, 18f), "0%", percentStyle);
        GUI.Label(new Rect(rect.x + rect.width * 0.5f - 22f, track.yMax + 22f, 70f, 18f), Mathf.RoundToInt(value * 100f) + "%", percentStyle);
        GUI.Label(new Rect(rect.xMax - 42f, track.yMax + 22f, 60f, 18f), "100%", percentStyle);

        value = GUI.HorizontalSlider(hitRect, value, 0f, 1f, GUIStyle.none, GUIStyle.none);

        return value;
    }

    private bool DrawHologramCheckbox(Rect rect, Rect hitRect, bool value)
    {
        if (GUI.Button(hitRect, GUIContent.none, GUIStyle.none))
        {
            value = !value;
        }

        DrawRect(rect, new Color(0f, 0.11f, 0.14f, 0.88f));
        DrawBorder(rect, new Color(Cyan.r, Cyan.g, Cyan.b, 0.92f), 3f);

        if (value)
        {
            DrawRect(new Rect(rect.x + 8f, rect.y + 18f, 6f, 12f), SoftCyan);
            DrawRect(new Rect(rect.x + 13f, rect.y + 24f, 7f, 6f), SoftCyan);
            DrawRect(new Rect(rect.x + 19f, rect.y + 10f, 6f, 20f), SoftCyan);
        }

        return value;
    }

    private void DrawIconBox(Rect rect, bool musicIcon)
    {
        DrawPanel(rect, new Color(0.02f, 0.09f, 0.12f, 0.58f), Cyan);

        if (musicIcon)
        {
            DrawMusicIcon(rect);
        }
        else
        {
            DrawEffectsIcon(rect);
        }
    }

    private void DrawMusicIcon(Rect rect)
    {
        Color icon = new Color(SoftCyan.r, SoftCyan.g, SoftCyan.b, 0.95f);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 10f, rect.width - 36f, 72f), "MUSIC", CreateStyle(22, FontStyle.Bold, icon, TextAnchor.MiddleCenter));

        float baseY = rect.y + 110f;
        for (int i = 0; i < 5; i++)
        {
            float height = 18f + i * 9f;
            DrawRect(new Rect(rect.x + 36f + i * 14f, baseY - height, 8f, height), icon);
        }
    }

    private void DrawEffectsIcon(Rect rect)
    {
        Color icon = new Color(SoftCyan.r, SoftCyan.g, SoftCyan.b, 0.95f);
        GUIStyle fxText = CreateStyle(44, FontStyle.Bold, icon, TextAnchor.MiddleCenter);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 16f, rect.width - 36f, 58f), "FX", fxText);

        float cx = rect.center.x;
        float cy = rect.y + 96f;
        DrawRect(new Rect(cx - 42f, cy - 3f, 84f, 6f), icon);
        DrawRect(new Rect(cx - 3f, cy - 42f, 6f, 84f), icon);
        DrawRect(new Rect(cx - 28f, cy - 28f, 56f, 6f), icon);
        DrawRect(new Rect(cx - 28f, cy + 22f, 56f, 6f), icon);
        DrawRect(new Rect(cx - 28f, cy - 28f, 6f, 56f), icon);
        DrawRect(new Rect(cx + 22f, cy - 28f, 6f, 56f), icon);
    }

    private void DrawPauseMenu()
    {
        DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0.005f, 0.015f, 0.82f));

        Rect panel = new Rect((Screen.width - 500f) * 0.5f, (Screen.height - 390f) * 0.5f, 500f, 390f);
        DrawPanel(panel, new Color(0.025f, 0.045f, 0.065f, 0.96f), Cyan);

        GUI.Label(new Rect(panel.x + 40f, panel.y + 30f, 420f, 48f), "COMMAND DECK", commandTitleStyle);

        GUI.Label(new Rect(panel.x + 40f, panel.y + 78f, 420f, 26f), "SIMULATION PAUSED", subtitleStyle);

        if (DrawHologramButton(new Rect(panel.x + 80f, panel.y + 128f, 340f, 48f), "RESUME MISSION"))
        {
            ResumeGameplay();
        }

        if (DrawHologramButton(new Rect(panel.x + 80f, panel.y + 188f, 340f, 48f), "SETTINGS"))
        {
            settingsOpen = true;
        }

        if (DrawHologramButton(new Rect(panel.x + 80f, panel.y + 248f, 340f, 48f), "RESTART MISSION"))
        {
            RestartScene();
        }

        if (DrawHologramButton(new Rect(panel.x + 80f, panel.y + 308f, 340f, 48f), "EXIT SIMULATION"))
        {
            ExitPlayMode();
        }
    }

    private string GetMovementState()
    {
        if (player == null)
        {
            return "NO PLAYER SIGNAL";
        }

        if (!player.IsGrounded)
        {
            return "AIRBORNE VECTOR  //  GROUND LOST";
        }

        if (player.IsRunning)
        {
            return "BOOST RUN  //  GROUND LOCK";
        }

        if (player.HasMovementInput)
        {
            return "MOTION ACTIVE  //  GROUND LOCK";
        }

        return "IDLE STANDBY  //  GROUND LOCK";
    }

    private string GetModifierLabel()
    {
        if (player == null) return "NO SIGNAL";

        float m = player.EffectiveSpeedMultiplier;
        if (m > 1.05f) return "BOOSTED x" + m.ToString("0.0");
        if (m < 0.95f) return "SLOWED x" + m.ToString("0.0");
        return "NORMAL x1.0";
    }

    private string GetModifierTimerText()
    {
        if (player == null) return "--";

        float remaining = player.TimedSpeedRemaining;
        return remaining > 0f ? remaining.ToString("0.0") + "s" : "--";
    }

    private Color GetModifierColor()
    {
        if (player == null) return MutedText;

        float m = player.EffectiveSpeedMultiplier;
        if (m > 1.05f) return Cyan;
        if (m < 0.95f) return Amber;
        return SoftCyan;
    }

    private Color GetStateColor()
    {
        if (player == null || !player.IsGrounded)
        {
            return Amber;
        }

        if (player.IsRunning)
        {
            return Cyan;
        }

        return SoftCyan;
    }

    private void DrawPanel(Rect rect, Color fillColor, Color accentColor)
    {
        DrawRect(rect, fillColor);
        DrawBorder(rect, new Color(accentColor.r, accentColor.g, accentColor.b, 0.45f), 1f);
        DrawCornerBrackets(rect, accentColor);
    }

    private void DrawBorder(Rect rect, Color color, float thickness)
    {
        DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
        DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
        DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
        DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
    }

    private void DrawCornerBrackets(Rect rect, Color color)
    {
        float length = 42f;
        float inset = 10f;
        float thickness = 3f;
        Color bracket = new Color(color.r, color.g, color.b, 0.9f);

        DrawRect(new Rect(rect.x + inset, rect.y + inset, length, thickness), bracket);
        DrawRect(new Rect(rect.x + inset, rect.y + inset, thickness, length), bracket);
        DrawRect(new Rect(rect.xMax - inset - length, rect.y + inset, length, thickness), bracket);
        DrawRect(new Rect(rect.xMax - inset - thickness, rect.y + inset, thickness, length), bracket);
        DrawRect(new Rect(rect.x + inset, rect.yMax - inset - thickness, length, thickness), bracket);
        DrawRect(new Rect(rect.x + inset, rect.yMax - inset - length, thickness, length), bracket);
        DrawRect(new Rect(rect.xMax - inset - length, rect.yMax - inset - thickness, length, thickness), bracket);
        DrawRect(new Rect(rect.xMax - inset - thickness, rect.yMax - inset - length, thickness, length), bracket);
    }

    private void DrawRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = previousColor;
    }

    private void DrawMenuBackdrop(float overlayAlpha)
    {
        if (menuBackground != null)
        {
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), menuBackground, ScaleMode.ScaleAndCrop);
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0.006f, 0.018f, overlayAlpha));
        }
        else
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0.006f, 0.018f, overlayAlpha + 0.24f));
        }

        if (!specialEffectsEnabled)
        {
            return;
        }

        Color lineColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.05f * effectsIntensity);
        for (float y = 0f; y < Screen.height; y += 18f)
        {
            DrawRect(new Rect(0f, y, Screen.width, 1f), lineColor);
        }
    }

    private void DrawMenuBackdropVirtual(float overlayAlpha)
    {
        Rect full = new Rect(0f, 0f, 1366f, 768f);

        if (menuBackground != null)
        {
            GUI.DrawTexture(full, menuBackground, ScaleMode.StretchToFill);
        }
        else
        {
            DrawRect(full, new Color(0f, 0.006f, 0.018f, 1f));
        }

        DrawRect(full, new Color(0f, 0.006f, 0.018f, overlayAlpha));
    }

    private void LoadMenuBackground()
    {
        string path = Path.Combine(Application.dataPath, "_Game", "UI", "MenuBackground.png");
        if (!File.Exists(path))
        {
            path = Path.Combine(Application.dataPath, "UI", "MenuBackground.png");
        }

        if (!File.Exists(path))
        {
            return;
        }

        byte[] imageBytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (texture.LoadImage(imageBytes))
        {
            menuBackground = texture;
        }
    }

    private void CreateAmbientMusic()
    {
        ambientMusic = gameObject.AddComponent<AudioSource>();
        ambientMusic.loop = true;
        ambientMusic.playOnAwake = false;
        ambientMusic.spatialBlend = 0f;
        ambientMusic.clip = Resources.Load<AudioClip>("Audio/CaCO3");

        if (ambientMusic.clip == null)
        {
            ambientMusic.clip = CreateAmbientClip();
        }

        ApplyAudioSettings();
        ambientMusic.Play();
    }

    private AudioClip CreateAmbientClip()
    {
        int sampleRate = 44100;
        int sampleCount = sampleRate * 2;
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(t * 42f) * 0.18f + Mathf.Sin(t * 73f) * 0.08f;
        }

        AudioClip clip = AudioClip.Create("Procedural Space Ambience", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void ApplyAudioSettings()
    {
        if (ambientMusic == null)
        {
            return;
        }

        ambientMusic.mute = !musicEnabled;
        ambientMusic.volume = musicVolume;
    }

    private void ShowMainMenu()
    {
        missionStarted = false;
        settingsOpen = false;
        SetGameplayActive(false);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void StartMission()
    {
        missionStarted = true;
        settingsOpen = false;
        SetGameplayActive(true);
        ResumeGameplay();
    }

    private void SetGameplayActive(bool active)
    {
        foreach (PlayerRigidbodyMovement movement in FindObjectsByType<PlayerRigidbodyMovement>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            movement.enabled = active;
        }

        foreach (CameraOrbitTarget cameraTarget in FindObjectsByType<CameraOrbitTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            cameraTarget.enabled = active;
        }
    }

    private void SetPaused(bool paused)
    {
        if (LevelManager.Instance != null)
        {
            if (paused)
            {
                LevelManager.Instance.PauseGame();
            }
            else
            {
                ResumeGameplay();
            }

            return;
        }

        Time.timeScale = paused ? 0f : 1f;
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    private bool IsGamePaused()
    {
        if (LevelManager.Instance != null)
        {
            return LevelManager.Instance.IsPaused;
        }

        return Time.timeScale == 0f && missionStarted;
    }

    private void ResumeGameplay()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.IsPaused)
        {
            LevelManager.Instance.ResumeGame();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetState(GameState.Playing);
        }

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void RestartScene()
    {
        if (LevelManager.Instance != null)
        {
            LevelManager.Instance.ResetLevel();
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ExitPlayMode()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
