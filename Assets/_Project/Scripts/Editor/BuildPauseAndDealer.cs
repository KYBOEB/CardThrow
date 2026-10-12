using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «UI: пауза и выход + крупье».  12.10, 22:40.
    //   1. В Canvas сцены Main — экран PauseScreen (затемнение, золотая рамка, «Пауза», три кнопки) + компонент PauseMenu.
    //      Кнопка «Пауза (Esc)» в HUD справа под «Выигрышем», кнопка «Выйти» на титульном экране под рекордом.
    //   2. Крупье — фигура как у игроков: dealer_body.png (320×240, тот же размер, что player_body) × 2, повёрнут к столу.
    //      Свои руки фигуры кладутся на стол перед веером карт; нарисованные отдельно руки крупье (Hand) скрываются —
    //      они не стыковались с телом. DealerHand меняет у Hand только sprite, enabled не трогает.
    // Повторный запуск безопасен (объекты ищутся по имени). Dealer.prefab — Алексей: «замок» в #locks.
    public static class BuildPauseAndDealer
    {
        private const string SpriteDir = "Assets/_Project/Art/Sprites/UI/";
        private const string DealerPath = "Assets/_Project/Prefabs/Gameplay/Dealer.prefab";
        private const string BodySprite = "Assets/_Project/Art/Sprites/Characters/dealer_body.png";
        private static readonly Color Cream = new Color(1f, 0.95f, 0.85f);
        private static readonly Color Gold = new Color(1f, 0.82f, 0.3f);
        private static readonly Color PanelTint = new Color(0.82f, 0.6f, 0.66f, 1f);

        private static TMP_FontAsset font;

        [MenuItem("CardThrow/UI: пауза и выход + крупье")]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("CardThrow", "Это меню уже применено, повторный запуск откатит более поздние правки (вернёт «Заново» и кнопку паузы, старого крупье). Запускать?", "Всё равно запустить", "Отмена"))
                return;
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }
            BuildPause();
            SetupDealer();
        }

        // ---------- Пауза ----------

        private static void BuildPause()
        {
            TitleScreen title = Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
            HUD hud = Object.FindAnyObjectByType<HUD>(FindObjectsInactive.Include);
            if (title == null || hud == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "Открой Scenes/Main.unity (нет TitleScreen или HUD).", "OK");
                return;
            }
            Canvas canvas = title.GetComponentInParent<Canvas>();
            TMP_Text sample = title.GetComponentInChildren<TMP_Text>(true);
            font = sample != null ? sample.font : null;

            // Экран паузы
            RectTransform root = FindOrCreate("PauseScreen", canvas.transform);
            Stretch(root);
            root.SetAsLastSibling(); // поверх всего UI
            CanvasGroup group = GetOrAdd<CanvasGroup>(root);
            group.alpha = 0f; group.interactable = false; group.blocksRaycasts = false;

            Image dim = Img(FindOrCreate("Dim", root), null, new Color(0f, 0f, 0f, 0.7f));
            Stretch(dim.rectTransform);
            dim.raycastTarget = true; // клики не проходят на стол

            RectTransform panel = FindOrCreate("Panel", root);
            Place(panel, Vector2.zero, new Vector2(620f, 500f));
            Image bg = Img(FindOrCreate("Background", panel), Load("ui_panel"), PanelTint);
            bg.type = Image.Type.Sliced; Stretch(bg.rectTransform); bg.transform.SetSiblingIndex(0);
            Image frame = Img(FindOrCreate("Frame", panel), Load("ui_frame_gold"), Color.white);
            frame.type = Image.Type.Sliced; frame.pixelsPerUnitMultiplier = 1f; Stretch(frame.rectTransform); frame.transform.SetSiblingIndex(1);

            TMP_Text head = Text(FindOrCreate("Title", panel), "Пауза", 64f, Gold);
            Place(head.rectTransform, new Vector2(0f, 170f), new Vector2(560f, 90f));
            Button resume = MakeButton(panel, "ResumeButton", "Продолжить (Esc)", new Vector2(0f, 55f));
            Button restart = MakeButton(panel, "RestartButton", "Заново (R)", new Vector2(0f, -45f));
            Button quit = MakeButton(panel, "QuitButton", "Выйти из игры", new Vector2(0f, -145f));
            TMP_Text hint = Text(FindOrCreate("Hint", panel), "P или Esc — пауза", 22f, new Color(1f, 1f, 1f, 0.6f));
            Place(hint.rectTransform, new Vector2(0f, -218f), new Vector2(560f, 34f));

            // Кнопка «Пауза» в HUD — справа под «Выигрышем»
            RectTransform hudBtn = FindOrCreate("PauseButton", hud.transform);
            Button hudPause = ButtonOn(hudBtn, "Пауза (Esc)", 24f);
            hudBtn.anchorMin = hudBtn.anchorMax = new Vector2(1f, 1f);
            hudBtn.pivot = new Vector2(1f, 1f);
            hudBtn.anchoredPosition = new Vector2(-24f, -132f);
            hudBtn.sizeDelta = new Vector2(230f, 58f);

            // Кнопка «Выйти» на титуле — под табличкой рекорда
            RectTransform titleBtn = FindOrCreate("QuitButton", title.transform);
            Button titleQuit = ButtonOn(titleBtn, "Выйти", 26f);
            Place(titleBtn, new Vector2(0f, -455f), new Vector2(260f, 64f));

            PauseMenu menu = GetOrAdd<PauseMenu>(root);
            SerializedObject so = new SerializedObject(menu);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("resumeButton").objectReferenceValue = resume;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("titleQuitButton").objectReferenceValue = titleQuit;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            Debug.Log("[CT] Пауза: экран PauseScreen, кнопка в HUD, «Выйти» на титуле. Сцена сохранена.");
        }

        private static Button MakeButton(RectTransform parent, string name, string label, Vector2 pos)
        {
            RectTransform rt = FindOrCreate(name, parent);
            Button b = ButtonOn(rt, label, 32f);
            Place(rt, pos, new Vector2(440f, 84f));
            return b;
        }

        private static Button ButtonOn(RectTransform rt, string label, float size)
        {
            Image img = GetOrAdd<Image>(rt);
            img.sprite = Load("ui_button");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.raycastTarget = true;
            Button b = GetOrAdd<Button>(rt);
            b.targetGraphic = img;
            TMP_Text t = Text(FindOrCreate("Label", rt), label, size, Cream);
            Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(12f, 4f);
            t.rectTransform.offsetMax = new Vector2(-12f, -4f);
            t.characterSpacing = 2f;
            return b;
        }

        // ---------- Крупье ----------

        private static void SetupDealer()
        {
            TextureImporter ti = AssetImporter.GetAtPath(BodySprite) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[CT] Нет {BodySprite}"); return; }
            if (ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 200f || ti.mipmapEnabled)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 200f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }

            GameObject root = PrefabUtility.LoadPrefabContents(DealerPath);
            try
            {
                Transform hand = root.transform.Find("Hand");
                SpriteRenderer handSr = hand != null ? hand.GetComponent<SpriteRenderer>() : null;
                Transform body = root.transform.Find("DealerBody");
                if (body == null)
                {
                    body = new GameObject("DealerBody").transform;
                    body.SetParent(root.transform, false);
                }
                // Как у игроков (Seat): масштаб 2, центр фигуры за краем стола, ладони ложатся на сукно перед веером карт
                body.localPosition = new Vector3(-0.95f, 0f, 0f);
                body.localRotation = Quaternion.Euler(0f, 0f, -90f);
                body.localScale = Vector3.one * 2f;
                SpriteRenderer sr = GetOrAdd<SpriteRenderer>(body);
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BodySprite);
                if (handSr != null)
                {
                    sr.sortingLayerID = handSr.sortingLayerID;
                    sr.sortingOrder = handSr.sortingOrder;
                    handSr.enabled = false; // отдельные руки с рукавами больше не нужны — у фигуры свои
                }
                PrefabUtility.SaveAsPrefabAsset(root, DealerPath);
                Debug.Log("[CT] Dealer.prefab: фигура крупье как у игроков (×2), отдельные руки скрыты");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Общее ----------

        private static RectTransform FindOrCreate(string name, Transform parent)
        {
            Transform t = parent.Find(name);
            if (t != null) return (RectTransform)t;
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image Img(RectTransform rt, Sprite sprite, Color color)
        {
            Image img = GetOrAdd<Image>(rt);
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text Text(RectTransform rt, string text, float size, Color color)
        {
            TextMeshProUGUI t = GetOrAdd<TextMeshProUGUI>(rt);
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.Center;
            t.raycastTarget = false;
            return t;
        }

        // Не «GetComponent() ?? AddComponent()»: в редакторе отсутствующий компонент — «фальшивый null», ?? его не ловит
        private static T GetOrAdd<T>(Component c) where T : Component
        {
            T t = c.GetComponent<T>();
            return t != null ? t : c.gameObject.AddComponent<T>();
        }

        private static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
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
