using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Создать UI игры (HUD и экраны)».
    // Пересоздаёт в Canvas «UI» панели HUD, ResultPopup, GameOverScreen, VictoryScreen, TitleScreen,
    // сам заполняет все поля скриптов через SerializedObject, проверяет EventSystem (InputSystemUIInputModule)
    // и добавляет SoundEventsBridge в [Systems]. TutorialPanel и остальную сцену НЕ трогает.
    // Повторный запуск безопасен: удаляются только пять своих панелей.
    //
    // Раскладка рассчитана на Canvas Scaler 1920×1080, Match 0.5: всё привязано к углам/краям,
    // по высоте помещается от ~900 ед. Canvas (широкое окно Game) до ~1250 (4:3), по ширине — от ~1650 (4:3).
    public static class BuildGameUI
    {
        private const string Root = "Assets/_Project";
        private const string SpriteDir = Root + "/Art/Sprites/UI/";
        private const string FontPath = Root + "/Art/Fonts/Montserrat-Bold SDF.asset";
        private const string TextsPath = Root + "/Data/UiTexts.asset";
        private const string SettingsPath = Root + "/Data/GameSettings.asset";

        private static readonly string[] OwnPanels = { "HUD", "ResultPopup", "GameOverScreen", "VictoryScreen", "TitleScreen" };

        private static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
        private static readonly Color Red = new Color(0.95f, 0.27f, 0.24f);
        private static readonly Color Soft = new Color(1f, 1f, 1f, 0.9f);

        private static TMP_FontAsset font;

        [MenuItem("CardThrow/Создать UI игры (HUD и экраны)")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            GameManager gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gm == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "В открытой сцене нет GameManager. Открой Scenes/Main.unity.", "OK");
                return;
            }

            UiTexts texts = LoadOrCreateTexts();
            GameSettings settings = LoadSettings(gm);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) Debug.LogWarning($"[CT] Не найден шрифт {FontPath} — будет шрифт TMP по умолчанию (кириллицы может не быть)");

            Canvas canvas = FindOrCreateCanvas();
            EnsureEventSystem();

            foreach (string panelName in OwnPanels)
            {
                Transform old = canvas.transform.Find(panelName);
                if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
            }

            // Порядок детей = порядок отрисовки (нижние в списке рисуются поверх)
            GameObject hud = BuildHud(canvas.transform, texts, settings);
            BuildResultPopup(canvas.transform, texts, settings);
            BuildEndScreen(canvas.transform, "GameOverScreen", GameState.GameOver, Red, texts);
            BuildEndScreen(canvas.transform, "VictoryScreen", GameState.Victory, Gold, texts);
            BuildTitleScreen(canvas.transform, texts);

            Transform tutorial = canvas.transform.Find("TutorialPanel");
            if (tutorial != null) tutorial.SetAsLastSibling(); // подсказки обучения — поверх всего
            else Debug.LogWarning("[CT] Нет TutorialPanel — запусти CardThrow → «Создать UI обучения»");

            EnsureSoundBridge(gm, settings);

            EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
            Selection.activeGameObject = hud;
            Debug.Log("[CT] UI игры собран: HUD, ResultPopup, GameOverScreen, VictoryScreen, TitleScreen + SoundEventsBridge. Сохрани сцену (Ctrl+S).");
        }

        // =====================================================================
        // HUD
        // =====================================================================

        private static GameObject BuildHud(Transform canvas, UiTexts texts, GameSettings settings)
        {
            RectTransform root = NewRect("HUD", canvas);
            Stretch(root);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

            // --- Верх слева: уровень ---
            TextMeshProUGUI level = NewText("LevelLabel", root, "Уровень 1/3", 34f, TextAlignmentOptions.Left, Color.white);
            Place(level.rectTransform, new Vector2(0f, 1f), new Vector2(28f, -22f), new Vector2(400f, 56f)); // не заходит под таймер даже при 4:3
            AutoSize(level, 22f, 34f, false);

            // --- Верх по центру: таймер ---
            RectTransform timer = NewRect("Timer", root);
            Place(timer, new Vector2(0.5f, 1f), new Vector2(-90f, -28f), new Vector2(600f, 40f));
            Image frame = NewImage("Frame", timer, "ui_timer_frame");
            frame.preserveAspect = false;
            Stretch(frame.rectTransform);
            Image fill = NewImage("Fill", timer, "ui_timer_fill");
            fill.preserveAspect = false;
            Stretch(fill.rectTransform);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;
            TextMeshProUGUI timeText = NewText("Seconds", timer, "20", 26f, TextAlignmentOptions.Center, Color.white);
            Stretch(timeText.rectTransform);

            // --- Справа от таймера: 3 глаза подозрения ---
            RectTransform suspicion = NewRect("Suspicion", root);
            suspicion.anchorMin = suspicion.anchorMax = new Vector2(0.5f, 1f);
            suspicion.pivot = new Vector2(0f, 1f);
            suspicion.anchoredPosition = new Vector2(234f, -20f);
            suspicion.sizeDelta = new Vector2(188f, 56f);
            HorizontalLayoutGroup eyesRow = suspicion.gameObject.AddComponent<HorizontalLayoutGroup>();
            eyesRow.spacing = 10f;
            eyesRow.childAlignment = TextAnchor.MiddleLeft;
            eyesRow.childControlWidth = eyesRow.childControlHeight = false;
            eyesRow.childForceExpandWidth = eyesRow.childForceExpandHeight = false;
            Image[] eyes = new Image[3];
            for (int i = 0; i < eyes.Length; i++)
            {
                eyes[i] = NewImage("Eye_" + (i + 1), suspicion, "ui_eye_open");
                eyes[i].rectTransform.sizeDelta = new Vector2(56f, 56f);
            }

            // --- Верх справа: очки (в обучении скрыты — там же стоит «Tab — пропустить») ---
            TextMeshProUGUI score = NewText("Score", root, "Выигрыш: 0", 36f, TextAlignmentOptions.Right, Gold);
            Place(score.rectTransform, new Vector2(1f, 1f), new Vector2(-28f, -22f), new Vector2(360f, 56f));
            AutoSize(score, 22f, 36f, false);

            // --- Подсказка над столом ---
            RectTransform hint = NewRect("Hint", root);
            Place(hint, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(1280f, 120f));
            CanvasGroup hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            Image hintBg = NewImage("Background", hint, "ui_panel");
            SetupPanel(hintBg, new Color(1f, 1f, 1f, 0.85f));
            TextMeshProUGUI hintText = NewText("Text", hint, "Подсказка", 42f, TextAlignmentOptions.Center, Color.white);
            Stretch(hintText.rectTransform, 28f, 10f);
            AutoSize(hintText, 24f, 42f, true);

            // --- Баннер начала раздачи (по центру) ---
            RectTransform banner = NewRect("Banner", root);
            Place(banner, new Vector2(0.5f, 0.5f), new Vector2(0f, 60f), new Vector2(980f, 230f));
            CanvasGroup bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            SetupPanel(NewImage("Background", banner, "ui_panel"), Color.white);
            TextMeshProUGUI bannerTitle = NewText("Title", banner, "Уровень 1/3", 84f, TextAlignmentOptions.Center, Gold);
            Band(bannerTitle.rectTransform, 0.42f, 1f, 24f, 10f);
            AutoSize(bannerTitle, 40f, 84f, false);
            TextMeshProUGUI bannerSub = NewText("Subtitle", banner, "Название раздачи", 44f, TextAlignmentOptions.Center, Color.white);
            Band(bannerSub.rectTransform, 0f, 0.45f, 24f, 14f);
            AutoSize(bannerSub, 24f, 44f, false);

            // --- Низ по центру: строка управления ---
            RectTransform controls = NewRect("Controls", root);
            controls.anchorMin = new Vector2(0f, 0f);   // растянута по ширине экрана с полями 24:
            controls.anchorMax = new Vector2(1f, 0f);   // на узком окне текст ужмётся автоподбором,
            controls.pivot = new Vector2(0.5f, 0f);     // а не вылезет за края
            controls.offsetMin = new Vector2(24f, 10f);
            controls.offsetMax = new Vector2(-24f, 56f);
            SetupPanel(NewImage("Background", controls, "ui_panel"), new Color(1f, 1f, 1f, 0.7f));
            TextMeshProUGUI controlsText = NewText("Text", controls, texts.controlsLine, 26f, TextAlignmentOptions.Center, Soft);
            Stretch(controlsText.rectTransform, 16f, 4f);
            AutoSize(controlsText, 14f, 26f, false);

            // --- Низ слева: карта и закрут ---
            RectTransform cardPanel = NewRect("CardPanel", root);
            Place(cardPanel, new Vector2(0f, 0f), new Vector2(24f, 66f), new Vector2(470f, 64f));
            SetupPanel(NewImage("Background", cardPanel, "ui_panel"), Color.white);
            TextMeshProUGUI card = NewText("CardLabel", cardPanel, "Карта: 7", 32f, TextAlignmentOptions.Left, Gold);
            card.rectTransform.anchorMin = new Vector2(0f, 0f);
            card.rectTransform.anchorMax = new Vector2(0f, 1f);
            card.rectTransform.pivot = new Vector2(0f, 0.5f);
            card.rectTransform.anchoredPosition = new Vector2(18f, 0f);
            card.rectTransform.sizeDelta = new Vector2(190f, 0f);
            AutoSize(card, 18f, 32f, false);

            RectTransform spin = NewRect("Spin", cardPanel);
            spin.anchorMin = spin.anchorMax = spin.pivot = new Vector2(0f, 0.5f);
            spin.anchoredPosition = new Vector2(215f, 0f);
            spin.sizeDelta = new Vector2(240f, 56f);
            Image spinArrow = NewImage("Arrow", spin, "ui_spin_arrow");
            spinArrow.rectTransform.anchorMin = spinArrow.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            spinArrow.rectTransform.anchoredPosition = new Vector2(28f, 0f);
            spinArrow.rectTransform.sizeDelta = new Vector2(44f, 44f);
            TextMeshProUGUI spinText = NewText("SpinLabel", spin, "Закрут 0", 28f, TextAlignmentOptions.Left, Color.white);
            Stretch(spinText.rectTransform);
            spinText.rectTransform.offsetMin = new Vector2(60f, 0f);
            AutoSize(spinText, 16f, 28f, false);

            // --- Низ справа: кнопка «Заново (R)» ---
            Button restart = NewButton("RestartButton", root, texts.restartButton, 28f, out TextMeshProUGUI restartLabel);
            Place((RectTransform)restart.transform, new Vector2(1f, 0f), new Vector2(-24f, 66f), new Vector2(260f, 64f));

            // В режиме редактирования окно Game показывает титул, а не всё сразу (в Play видимостью управляют скрипты)
            HideInEditor(group);
            HideInEditor(hintGroup);
            HideInEditor(bannerGroup);

            // --- Поля HUD ---
            HUD hudComp = root.gameObject.AddComponent<HUD>();
            SerializedObject so = new SerializedObject(hudComp);
            Set(so, "texts", texts);
            Set(so, "settings", settings);
            Set(so, "group", group);
            Set(so, "levelLabel", level);
            Set(so, "bannerGroup", bannerGroup);
            Set(so, "bannerTitle", bannerTitle);
            Set(so, "bannerSubtitle", bannerSub);
            Set(so, "timerRoot", timer.gameObject);
            Set(so, "timerFill", fill);
            Set(so, "timerLabel", timeText);
            Set(so, "suspicionRoot", suspicion.gameObject);
            SetArray(so, "eyes", eyes);
            Set(so, "eyeOpenSprite", LoadSprite("ui_eye_open"));
            Set(so, "eyeClosedSprite", LoadSprite("ui_eye_closed"));
            Set(so, "scoreRoot", score.gameObject);
            Set(so, "scoreLabel", score);
            Set(so, "cardLabel", card);
            Set(so, "spinGroup", spin);
            Set(so, "spinArrow", spinArrow);
            Set(so, "spinLabel", spinText);
            Set(so, "hintGroup", hintGroup);
            Set(so, "hintLabel", hintText);
            Set(so, "controlsLabel", controlsText);
            Set(so, "restartButton", restart);
            Set(so, "restartLabel", restartLabel);
            so.ApplyModifiedPropertiesWithoutUndo();

            return root.gameObject;
        }

        // =====================================================================
        // ResultPopup
        // =====================================================================

        private static void BuildResultPopup(Transform canvas, UiTexts texts, GameSettings settings)
        {
            RectTransform root = NewRect("ResultPopup", canvas);
            Place(root, new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1100f, 330f));
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            SetupPanel(NewImage("Background", root, "ui_panel"), new Color(1f, 1f, 1f, 0.9f));

            TextMeshProUGUI title = NewText("Title", root, "BLACKJACK!", 110f, TextAlignmentOptions.Center, Gold);
            Band(title.rectTransform, 0.44f, 1f, 30f, 8f);
            AutoSize(title, 56f, 110f, false);
            TextMeshProUGUI subtitle = NewText("Subtitle", root, "Напарник даже не моргнул", 40f, TextAlignmentOptions.Center, Color.white);
            Band(subtitle.rectTransform, 0.2f, 0.46f, 30f, 0f);
            AutoSize(subtitle, 22f, 40f, true);
            TextMeshProUGUI gained = NewText("Gained", root, "+1 850", 52f, TextAlignmentOptions.Center, Gold);
            Band(gained.rectTransform, 0f, 0.22f, 30f, 6f);
            AutoSize(gained, 28f, 52f, false);

            HideInEditor(group);

            ResultPopup popup = root.gameObject.AddComponent<ResultPopup>();
            SerializedObject so = new SerializedObject(popup);
            Set(so, "texts", texts);
            Set(so, "settings", settings);
            Set(so, "group", group);
            Set(so, "titleLabel", title);
            Set(so, "subtitleLabel", subtitle);
            Set(so, "gainedLabel", gained);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // TitleScreen
        // =====================================================================

        private static void BuildTitleScreen(Transform canvas, UiTexts texts)
        {
            RectTransform root = NewRect("TitleScreen", canvas);
            Stretch(root);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            Image dim = NewPlainImage("Dim", root, new Color(0f, 0f, 0f, 0.55f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true; // клики по столу под титулом не проходят

            Image logo = NewImage("Logo", root, "logo");
            Place(logo.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(840f, 280f));

            RectTransform rules = NewRect("Rules", root);
            Place(rules, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1240f, 230f));
            SetupPanel(NewImage("Background", rules, "ui_panel"), Color.white);
            TextMeshProUGUI[] lines = new TextMeshProUGUI[3];
            for (int i = 0; i < lines.Length; i++)
            {
                string line = texts.rulesLines != null && i < texts.rulesLines.Length ? texts.rulesLines[i] : "";
                lines[i] = NewText("Rule_" + (i + 1), rules, line, 32f, TextAlignmentOptions.Center, i == 0 ? Gold : Color.white);
                float top = 1f - i / 3f, bottom = 1f - (i + 1) / 3f;
                Band(lines[i].rectTransform, bottom, top, 28f, 6f);
                AutoSize(lines[i], 20f, 32f, true);
            }

            Button play = NewButton("PlayButton", root, texts.playButton, 52f, out TextMeshProUGUI playLabel);
            Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(600f, 120f));

            TextMeshProUGUI high = NewText("HighScore", root, "Рекорд: 0", 34f, TextAlignmentOptions.Center, Gold);
            Place(high.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -300f), new Vector2(800f, 50f));

            TitleScreen title = root.gameObject.AddComponent<TitleScreen>();
            SerializedObject so = new SerializedObject(title);
            Set(so, "texts", texts);
            Set(so, "group", group);
            SetArray(so, "ruleLabels", lines);
            Set(so, "playButton", play);
            Set(so, "playLabel", playLabel);
            Set(so, "highScoreLabel", high);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // EndScreen (GameOver / Victory)
        // =====================================================================

        private static void BuildEndScreen(Transform canvas, string name, GameState mode, Color titleColor, UiTexts texts)
        {
            bool victory = mode == GameState.Victory;

            RectTransform root = NewRect(name, canvas);
            Stretch(root);
            CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
            Image dim = NewPlainImage("Dim", root, new Color(0f, 0f, 0f, 0.65f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            RectTransform panel = NewRect("Panel", root);
            Place(panel, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 620f));
            SetupPanel(NewImage("Background", panel, "ui_panel"), Color.white);

            TextMeshProUGUI title = NewText("Title", panel, victory ? texts.victoryTitle : texts.gameOverTitle, 84f, TextAlignmentOptions.Center, titleColor);
            FromTop(title.rectTransform, 40f, 100f);
            AutoSize(title, 40f, 84f, false);
            TextMeshProUGUI subtitle = NewText("Subtitle", panel, victory ? texts.victorySubtitle : texts.gameOverSuspicionReason, 36f, TextAlignmentOptions.Center, Color.white);
            FromTop(subtitle.rectTransform, 150f, 80f);
            AutoSize(subtitle, 22f, 36f, true);
            TextMeshProUGUI final = NewText("FinalScore", panel, "Выигрыш: 0", 56f, TextAlignmentOptions.Center, Color.white);
            FromTop(final.rectTransform, 250f, 70f);
            AutoSize(final, 30f, 56f, false);
            TextMeshProUGUI high = NewText("HighScore", panel, "Рекорд: 0", 34f, TextAlignmentOptions.Center, Gold);
            FromTop(high.rectTransform, 325f, 50f);
            TextMeshProUGUI record = NewText("NewRecord", panel, texts.newRecord, 44f, TextAlignmentOptions.Center, Gold);
            FromTop(record.rectTransform, 380f, 60f);

            Button again = NewButton("PlayAgainButton", panel, texts.playAgainButton, 44f, out TextMeshProUGUI againLabel);
            RectTransform againRect = (RectTransform)again.transform;
            againRect.anchorMin = againRect.anchorMax = againRect.pivot = new Vector2(0.5f, 0f);
            againRect.anchoredPosition = new Vector2(0f, 40f);
            againRect.sizeDelta = new Vector2(520f, 110f);

            HideInEditor(group);

            EndScreen screen = root.gameObject.AddComponent<EndScreen>();
            SerializedObject so = new SerializedObject(screen);
            so.FindProperty("mode").enumValueIndex = (int)mode; // GameState без явных чисел → индекс = значение
            Set(so, "texts", texts);
            Set(so, "group", group);
            Set(so, "panel", panel);
            Set(so, "titleLabel", title);
            Set(so, "subtitleLabel", subtitle);
            Set(so, "finalScoreLabel", final);
            Set(so, "highScoreLabel", high);
            Set(so, "newRecordLabel", record);
            Set(so, "playAgainButton", again);
            Set(so, "playAgainLabel", againLabel);
            so.FindProperty("appearDelay").floatValue = victory ? 0.2f : 0.6f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // =====================================================================
        // Сцена: Canvas, EventSystem, SoundEventsBridge
        // =====================================================================

        private static Canvas FindOrCreateCanvas()
        {
            // Сначала ищем именно «UI» (там уже TutorialPanel), чтобы не схватить чужой world-space Canvas
            GameObject named = GameObject.Find("UI");
            Canvas canvas = named != null ? named.GetComponent<Canvas>() : null;
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null) canvas = canvas.rootCanvas;
            else
            {
                GameObject go = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(go, "Create UI Canvas");
                go.layer = LayerMask.NameToLayer("UI");
                canvas = go.GetComponent<Canvas>();
                Debug.Log("[CT] Создан Canvas «UI»");
            }

            // Настройки по CONTEXT §4 — выставляем всегда (идемпотентно)
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = Undo.AddComponent<CanvasScaler>(canvas.gameObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            if (canvas.GetComponent<GraphicRaycaster>() == null) Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            return canvas;
        }

        // Кнопки без EventSystem не нажимаются; со старым StandaloneInputModule при новом Input System — ошибки в Console
        private static void EnsureEventSystem()
        {
            EventSystem es = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
            if (es == null)
            {
                GameObject go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                Debug.Log("[CT] Создан EventSystem с InputSystemUIInputModule");
                return;
            }

            StandaloneInputModule legacy = es.GetComponent<StandaloneInputModule>();
            if (legacy != null)
            {
                Undo.DestroyObjectImmediate(legacy);
                Debug.Log("[CT] EventSystem: StandaloneInputModule заменён на InputSystemUIInputModule");
            }
            if (es.GetComponent<InputSystemUIInputModule>() == null) Undo.AddComponent<InputSystemUIInputModule>(es.gameObject);
        }

        private static void EnsureSoundBridge(GameManager gm, GameSettings settings)
        {
            SoundEventsBridge bridge = Object.FindAnyObjectByType<SoundEventsBridge>(FindObjectsInactive.Include);
            if (bridge == null)
            {
                AudioManager audio = Object.FindAnyObjectByType<AudioManager>(FindObjectsInactive.Include);
                GameObject host = audio != null ? audio.gameObject : gm.gameObject;
                bridge = Undo.AddComponent<SoundEventsBridge>(host);
                Debug.Log($"[CT] SoundEventsBridge добавлен на {host.name}");
                if (audio == null) Debug.LogWarning("[CT] В сцене нет AudioManager — звуков не будет");
            }
            SerializedObject so = new SerializedObject(bridge);
            Set(so, "settings", settings);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static UiTexts LoadOrCreateTexts()
        {
            UiTexts texts = AssetDatabase.LoadAssetAtPath<UiTexts>(TextsPath);
            if (texts != null) return texts;
            texts = ScriptableObject.CreateInstance<UiTexts>();
            AssetDatabase.CreateAsset(texts, TextsPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CT] Создан {TextsPath}");
            return texts;
        }

        private static GameSettings LoadSettings(GameManager gm)
        {
            GameSettings settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (settings != null) return settings;
            settings = new SerializedObject(gm).FindProperty("settings").objectReferenceValue as GameSettings;
            if (settings == null) Debug.LogWarning($"[CT] Не найден {SettingsPath} и GameManager.settings пуст — HUD возьмёт тайминги по умолчанию");
            return settings;
        }

        // =====================================================================
        // Фабрики объектов UI
        // =====================================================================

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Sprite LoadSprite(string spriteName)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + spriteName + ".png");
            if (sprite == null) Debug.LogWarning($"[CT] Не найден спрайт {SpriteDir}{spriteName}.png (Texture Type должен быть Sprite)");
            return sprite;
        }

        private static Image NewImage(string name, Transform parent, string spriteName)
        {
            Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = LoadSprite(spriteName);
            img.preserveAspect = true;
            img.raycastTarget = false; // декор не перехватывает ЛКМ, которой игрок бросает карту
            return img;
        }

        private static Image NewPlainImage(string name, Transform parent, Color color)
        {
            Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        // Фон-плашка: растянута на родителя, 9-slice, если у спрайта заданы границы
        private static void SetupPanel(Image img, Color tint)
        {
            img.preserveAspect = false;
            img.type = img.sprite != null && img.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.color = tint;
            Stretch(img.rectTransform);
            img.transform.SetAsFirstSibling(); // фон под текстом
        }

        private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, TextAlignmentOptions align, Color color)
        {
            TextMeshProUGUI t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        // Кнопка = ui_button (9-slice) + надпись. Единственные элементы с raycastTarget = true.
        private static Button NewButton(string name, Transform parent, string label, float fontSize, out TextMeshProUGUI text)
        {
            Image bg = NewImage(name, parent, "ui_button");
            bg.preserveAspect = false;
            bg.type = bg.sprite != null && bg.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            bg.raycastTarget = true;
            Button button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.highlightedColor = new Color(1f, 0.92f, 0.75f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            button.colors = colors;

            text = NewText("Label", bg.transform, label, fontSize, TextAlignmentOptions.Center, Color.white);
            Stretch(text.rectTransform, 16f, 6f);
            AutoSize(text, fontSize * 0.5f, fontSize, false);
            return button;
        }

        // Чтобы поправить скрытую панель руками — поставь Alpha = 1 в её Canvas Group; в Play скрипт всё равно выставит сам
        private static void HideInEditor(CanvasGroup group)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }

        // ---------- Раскладка ----------

        // Якорь и pivot в одной точке (угол/край/центр), позиция от неё, фиксированный размер
        private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float padX = 0f, float padY = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        // Горизонтальная полоса на всю ширину родителя между долями высоты bottom..top
        private static void Band(RectTransform rt, float bottom, float top, float padX, float padY)
        {
            rt.anchorMin = new Vector2(0f, bottom);
            rt.anchorMax = new Vector2(1f, top);
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        // Полоса на всю ширину родителя, отсчёт от верхнего края
        private static void FromTop(RectTransform rt, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -top);
            rt.sizeDelta = new Vector2(-48f, height); // по 24 отступа слева и справа
        }

        // Автоподбор размера шрифта: длинный текст уменьшается, а не вылезает за рамку
        private static void AutoSize(TextMeshProUGUI t, float min, float max, bool wrap)
        {
            t.enableAutoSizing = true;
            t.fontSizeMin = min;
            t.fontSizeMax = max;
            if (!wrap) DisableWrapping(t);
        }

        // Через SerializedObject, чтобы не зависеть от версии API TextMeshPro
        // (Unity 6: m_TextWrappingMode, 0 = NoWrap; раньше — m_enableWordWrapping). Как в BuildTutorialUI.
        private static void DisableWrapping(TextMeshProUGUI text)
        {
            SerializedObject so = new SerializedObject(text);
            SerializedProperty mode = so.FindProperty("m_TextWrappingMode");
            if (mode != null) mode.intValue = 0;
            else
            {
                SerializedProperty legacy = so.FindProperty("m_enableWordWrapping");
                if (legacy != null) legacy.boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- SerializedObject ----------

        // Громко сообщает об опечатке в имени поля, вместо того чтобы молча оставить его пустым
        private static void Set(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[CT] {so.targetObject.GetType().Name}: нет поля «{field}»"); return; }
            p.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject so, string field, Object[] items)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[CT] {so.targetObject.GetType().Name}: нет поля «{field}»"); return; }
            p.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
