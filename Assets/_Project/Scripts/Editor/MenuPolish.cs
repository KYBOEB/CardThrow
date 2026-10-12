using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «UI: титул, пауза с громкостью, Ур.2 зона».  12.10, 23:30, после плейтеста.
    //   1. Титул: «Раздавать» — зелёная (ui_button_green), «Выйти» — справа от неё в один ряд.
    //      Статичный текст правил в сцене: «Бутылки не задевай» → «Стаканы не задевай».
    //   2. Пауза: убраны «Заново (R)» и подсказка «P или Esc — пауза»; добавлен ползунок «Громкость музыки».
    //   3. HUD: кнопка «Пауза» удалена (хватает Esc / P). «Пересдать (R)» → «Заново (R)» (UiTexts + сцена).
    //   4. Ур.2 (Hand_02): зона напарника стандартного размера. Напарник сидит у верхнего края, а зона в мире
    //      не поворачивается (Seat.ClipZoneToTable), поэтому 1.6×1.4 «вглубь × вдоль» = 1.4 по X × 1.6 по Y.
    // Повторный запуск безопасен. Hand_02 — файл Егора: «замок» в #locks.
    public static class MenuPolish
    {
        private const string UiDir = "Assets/_Project/Art/Sprites/UI/";
        private const string GreenButton = UiDir + "ui_button_green.png";
        private const string TextsPath = "Assets/_Project/Data/UiTexts.asset";
        private const string Hand02 = "Assets/_Project/Prefabs/Levels/Hand_02.prefab";
        private static readonly Color Cream = new Color(1f, 0.95f, 0.85f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.3f);

        private static readonly (string from, string to)[] TextPairs =
        {
            ("Бутылки не задевай", "Стаканы не задевай"),
            ("Пересдать (R)", "Заново (R)"),
        };

        [MenuItem("CardThrow/UI: титул, пауза с громкостью, Ур.2 зона")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }
            TitleScreen title = Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
            PauseMenu pause = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
            HUD hud = Object.FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
            if (title == null || pause == null || hud == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "Открой Scenes/Main.unity (нужны TitleScreen, PauseMenu, HUD).", "OK");
                return;
            }

            FixTexts(title.GetComponentInParent<Canvas>());
            StyleTitle(title);
            StylePause(pause);
            RemoveHudPause(hud);
            SetStandardZone();

            Scene scene = title.gameObject.scene;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Титул, пауза с громкостью, HUD, зона Ур.2 — готово. Сцена сохранена.");
        }

        // ---------- Тексты ----------

        private static void FixTexts(Canvas canvas)
        {
            int changed = 0;
            UiTexts texts = AssetDatabase.LoadAssetAtPath<UiTexts>(TextsPath);
            if (texts != null)
            {
                SerializedObject so = new SerializedObject(texts);
                SerializedProperty p = so.GetIterator();
                while (p.Next(true))
                {
                    if (p.propertyType != SerializedPropertyType.String) continue;
                    string r = Replace(p.stringValue);
                    if (r != p.stringValue) { p.stringValue = r; changed++; }
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(texts);
            }
            // Часть надписей лежит в сцене как есть (например, правила на титуле)
            foreach (TMP_Text t in canvas.GetComponentsInChildren<TMP_Text>(true))
            {
                string r = Replace(t.text);
                if (r == t.text) continue;
                Undo.RecordObject(t, "CT texts");
                t.text = r;
                EditorUtility.SetDirty(t);
                changed++;
            }
            Debug.Log($"[CT] Тексты: исправлено строк — {changed}");
        }

        private static string Replace(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            foreach (var pair in TextPairs) s = s.Replace(pair.from, pair.to);
            return s;
        }

        // ---------- Титул ----------

        private static void StyleTitle(TitleScreen title)
        {
            SerializedObject so = new SerializedObject(title);
            Button play = so.FindProperty("playButton").objectReferenceValue as Button;
            Transform quitT = title.transform.Find("QuitButton");
            if (play == null || quitT == null) { Debug.LogWarning("[CT] Титул: нет PlayButton или QuitButton"); return; }

            Sprite green = ImportButtonSprite(GreenButton);
            Image playImg = play.GetComponent<Image>();
            if (playImg != null && green != null)
            {
                playImg.sprite = green;
                playImg.type = Image.Type.Sliced;
                playImg.color = Color.white;
            }

            // Ряд кнопок: [ Раздавать (Space) 540 ]  40  [ Выйти 280 ], ряд по центру экрана
            RectTransform playRt = (RectTransform)play.transform;
            RectTransform quitRt = (RectTransform)quitT;
            float y = playRt.anchoredPosition.y;
            playRt.sizeDelta = new Vector2(540f, 104f);
            playRt.anchoredPosition = new Vector2(-160f, y);
            quitRt.anchorMin = playRt.anchorMin;
            quitRt.anchorMax = playRt.anchorMax;
            quitRt.pivot = playRt.pivot;
            quitRt.sizeDelta = new Vector2(280f, 104f);
            quitRt.anchoredPosition = new Vector2(290f, y);
            TMP_Text quitLabel = quitRt.GetComponentInChildren<TMP_Text>(true);
            TMP_Text playLabel = so.FindProperty("playLabel").objectReferenceValue as TMP_Text;
            if (quitLabel != null)
            {
                quitLabel.fontSize = playLabel != null ? playLabel.fontSize : 36f;
                quitLabel.enableAutoSizing = false;
            }
            EditorUtility.SetDirty(playRt);
            EditorUtility.SetDirty(quitRt);
            Debug.Log("[CT] Титул: «Раздавать» зелёная, «Выйти» справа от неё");
        }

        private static Sprite ImportButtonSprite(string path)
        {
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[CT] Нет {path}"); return null; }
            Vector4 border = new Vector4(32f, 32f, 32f, 32f); // как у ui_button
            if (ti.textureType != TextureImporterType.Sprite || ti.spriteBorder != border || ti.mipmapEnabled)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 100f;
                ti.spriteBorder = border;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- Пауза ----------

        private static void StylePause(PauseMenu pause)
        {
            Transform panel = pause.transform.Find("Panel");
            if (panel == null) { Debug.LogWarning("[CT] Пауза: нет PauseScreen/Panel"); return; }
            Destroy(panel, "RestartButton");
            Destroy(panel, "Hint");

            SetPos(panel, "Title", new Vector2(0f, 165f));
            SetPos(panel, "ResumeButton", new Vector2(0f, 60f));
            SetPos(panel, "QuitButton", new Vector2(0f, -40f));

            TMP_Text sample = panel.GetComponentInChildren<TMP_Text>(true);
            RectTransform caption = FindOrCreate("MusicLabel", panel);
            TextMeshProUGUI capText = GetOrAdd<TextMeshProUGUI>(caption);
            if (sample != null) capText.font = sample.font;
            capText.text = "Громкость музыки";
            capText.fontSize = 28f;
            capText.color = Cream;
            capText.alignment = TextAlignmentOptions.Center;
            capText.raycastTarget = false;
            Place(caption, new Vector2(0f, -128f), new Vector2(520f, 40f));

            Slider slider = BuildSlider(panel);

            SerializedObject so = new SerializedObject(pause);
            so.FindProperty("musicSlider").objectReferenceValue = slider;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[CT] Пауза: без «Заново» и подсказки, ползунок громкости музыки");
        }

        private static Slider BuildSlider(Transform panel)
        {
            Transform old = panel.Find("MusicSlider");
            if (old != null) Object.DestroyImmediate(old.gameObject); // пересобираем начисто — так проще, чем чинить

            // Стандартный слайдер uGUI (как из меню GameObject → UI → Slider), дальше — свои размеры и цвета
            DefaultControls.Resources res = new DefaultControls.Resources
            {
                standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
                background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
                knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            };
            GameObject go = DefaultControls.CreateSlider(res);
            go.name = "MusicSlider";
            go.transform.SetParent(panel, false);
            RectTransform rt = (RectTransform)go.transform;
            Place(rt, new Vector2(0f, -185f), new Vector2(420f, 34f));

            Slider slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.value = 1f;
            slider.navigation = new Navigation { mode = Navigation.Mode.None }; // стрелки ← → — выбор карты, не громкость

            Tint(rt, "Background", new Color(0.08f, 0.05f, 0.07f, 0.95f));
            Tint(rt, "Fill Area/Fill", Gold);
            Image knob = Tint(rt, "Handle Slide Area/Handle", Cream);
            if (knob != null) ((RectTransform)knob.transform).sizeDelta = new Vector2(34f, 0f);
            return slider;
        }

        private static Image Tint(Transform root, string path, Color c)
        {
            Transform t = root.Find(path);
            Image img = t != null ? t.GetComponent<Image>() : null;
            if (img != null) img.color = c;
            return img;
        }

        // ---------- HUD ----------

        private static void RemoveHudPause(HUD hud)
        {
            Destroy(hud.transform, "PauseButton");
        }

        // ---------- Ур.2 ----------

        private static void SetStandardZone()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(Hand02);
            try
            {
                LevelConfig level = root.GetComponent<LevelConfig>();
                Zone zone = level != null && level.PartnerSeat != null ? level.PartnerSeat.Zone : null;
                BoxCollider2D box = zone != null ? zone.GetComponent<BoxCollider2D>() : null;
                if (box == null) { Debug.LogWarning("[CT] Hand_02: не нашёл зону напарника"); return; }
                // Напарник у верхнего края: «вглубь стола» — это −Y. Сдвиг 0.1 к столу, как у Ур.3–5.
                zone.transform.localPosition = new Vector3(0f, -0.1f, zone.transform.localPosition.z);
                box.offset = Vector2.zero;
                box.size = new Vector2(1.4f, 1.6f);
                PrefabUtility.SaveAsPrefabAsset(root, Hand02);
                Debug.Log("[CT] Hand_02: зона напарника 1.4×1.6 (стандарт, повёрнут к верхнему краю)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Общее ----------

        private static void Destroy(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static void SetPos(Transform parent, string name, Vector2 pos)
        {
            if (parent.Find(name) is RectTransform rt) rt.anchoredPosition = pos;
        }

        private static RectTransform FindOrCreate(string name, Transform parent)
        {
            Transform t = parent.Find(name);
            if (t != null) return (RectTransform)t;
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        // Не «GetComponent() ?? AddComponent()»: в редакторе отсутствующий компонент — «фальшивый null», ?? его не ловит
        private static T GetOrAdd<T>(Component c) where T : Component
        {
            T t = c.GetComponent<T>();
            return t != null ? t : c.gameObject.AddComponent<T>();
        }

        private static void Place(RectTransform rt, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
