using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «UI: оформить стартовый экран».  Плейтест 12.10: «не нравится таблица, кнопка и рекорд».
    // Правит ТОЛЬКО существующие объекты TitleScreen в открытой сцене Main (ссылки берёт из полей TitleScreen),
    // ничего не пересоздаёт — остальной UI и правки команды не трогаются. Повторный запуск безопасен.
    //   • Правила: тёмная плашка + золотая рамка (как панели HUD), заголовок — золотой градиент,
    //     под ним золотой разделитель, ключевые слова подсвечены (<color> в UiTexts).
    //   • Кнопки (ui_button): белый тинт, 9-slice, кремовая надпись — новый ui_button.png с золотым ободком
    //     подхватывается во всех кнопках игры (Раздавать / Пересдать / Новая смена).
    //   • Рекорд: табличка с рамкой; TitleScreen прячет её целиком, пока рекорда нет.
    public static class StyleTitleScreen
    {
        private const string SpriteDir = "Assets/_Project/Art/Sprites/UI/";
        private const float FrameScale = 2f; // как в BuildGameUI: рамка на экране вдвое тоньше

        private static readonly Color Cream = new Color(1f, 0.95f, 0.85f);
        private static readonly Color GoldLight = new Color(1f, 0.93f, 0.6f);
        private static readonly Color GoldDeep = new Color(0.95f, 0.66f, 0.2f);
        private static readonly Color PanelTint = new Color(0.82f, 0.6f, 0.66f, 1f); // ui_panel (25,25,35) → тёмно-сливовый

        [MenuItem("CardThrow/UI: оформить стартовый экран")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            TitleScreen title = Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
            if (title == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "В открытой сцене нет TitleScreen. Открой Scenes/Main.unity.", "OK");
                return;
            }

            SerializedObject so = new SerializedObject(title);
            UiTexts texts = so.FindProperty("texts").objectReferenceValue as UiTexts;
            SerializedProperty rulesProp = so.FindProperty("ruleLabels");
            Button play = so.FindProperty("playButton").objectReferenceValue as Button;
            TMP_Text high = so.FindProperty("highScoreLabel").objectReferenceValue as TMP_Text;

            Undo.RegisterFullObjectHierarchyUndo(title.gameObject, "Оформить стартовый экран");

            // Затемнение стола чуть сильнее — текст читается лучше
            Image dim = Find<Image>(title.transform, "Dim");
            if (dim != null) dim.color = new Color(0f, 0f, 0f, 0.65f);

            StyleRules(rulesProp);
            StyleTexts(texts);
            foreach (Button b in title.transform.root.GetComponentsInChildren<Button>(true)) StyleButton(b);
            if (play != null)
            {
                RectTransform rt = (RectTransform)play.transform;
                rt.sizeDelta = new Vector2(540f, 104f);
                rt.anchoredPosition = new Vector2(0f, -200f);
            }
            GameObject plaque = StyleHighScore(title.transform, high);
            SerializedProperty rootProp = so.FindProperty("highScoreRoot");
            if (rootProp != null) rootProp.objectReferenceValue = plaque;
            else Debug.LogWarning("[CT] У TitleScreen нет поля highScoreRoot — TitleScreen.cs не обновлён? Табличка будет видна и без рекорда.");
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(title.gameObject.scene);
            EditorSceneManager.SaveScene(title.gameObject.scene);
            Debug.Log("[CT] Стартовый экран оформлен: рамка правил, градиент, кнопки, табличка рекорда. Сцена сохранена.");
        }

        // ---------- Правила ----------

        private static void StyleRules(SerializedProperty rulesProp)
        {
            if (rulesProp == null || rulesProp.arraySize == 0) return;
            TMP_Text first = rulesProp.GetArrayElementAtIndex(0).objectReferenceValue as TMP_Text;
            if (first == null) return;

            RectTransform rules = (RectTransform)first.transform.parent;
            rules.sizeDelta = new Vector2(1180f, 250f);
            rules.anchoredPosition = new Vector2(0f, 10f);

            Image bg = Find<Image>(rules, "Background");
            if (bg != null) bg.color = PanelTint;
            EnsureFrame(rules, 1);

            int n = rulesProp.arraySize;
            for (int i = 0; i < n; i++)
            {
                TMP_Text t = rulesProp.GetArrayElementAtIndex(i).objectReferenceValue as TMP_Text;
                if (t == null) continue;
                // Заголовок — верхние 38%, две строки правил делят остальное
                float top = i == 0 ? 1f : 0.62f - (i - 1) * 0.31f;
                float bottom = i == 0 ? 0.62f : top - 0.31f;
                RectTransform r = t.rectTransform;
                r.anchorMin = new Vector2(0f, bottom);
                r.anchorMax = new Vector2(1f, top);
                r.offsetMin = new Vector2(48f, 4f);
                r.offsetMax = new Vector2(-48f, -4f);
                t.richText = true;
                t.enableAutoSizing = true;
                t.fontSizeMin = 16f;
                t.characterSpacing = 1f;
                if (i == 0)
                {
                    t.fontSizeMax = 30f;
                    t.color = Color.white; // градиент умножается на color
                    t.enableVertexGradient = true;
                    t.colorGradient = new VertexGradient(GoldLight, GoldLight, GoldDeep, GoldDeep);
                }
                else
                {
                    t.fontSizeMax = 26f;
                    t.enableVertexGradient = false;
                    t.color = Cream;
                }
                EditorUtility.SetDirty(t);
            }

            // Золотой разделитель между заголовком и правилами
            Image divider = Find<Image>(rules, "Divider");
            if (divider == null)
            {
                GameObject go = new GameObject("Divider", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(rules, false);
                divider = go.GetComponent<Image>();
            }
            divider.raycastTarget = false;
            divider.color = new Color(GoldDeep.r, GoldDeep.g, GoldDeep.b, 0.55f);
            RectTransform d = divider.rectTransform;
            d.anchorMin = new Vector2(0.2f, 0.62f);
            d.anchorMax = new Vector2(0.8f, 0.62f);
            d.sizeDelta = new Vector2(0f, 2f);
            d.anchoredPosition = Vector2.zero;
        }

        // Ключевые слова — цветом (rich text TMP). Только если строку ещё не размечали.
        private static void StyleTexts(UiTexts texts)
        {
            if (texts == null || texts.rulesLines == null) return;
            Undo.RecordObject(texts, "Оформить тексты титула");
            for (int i = 1; i < texts.rulesLines.Length; i++)
            {
                string s = texts.rulesLines[i];
                if (string.IsNullOrEmpty(s) || s.Contains("<color")) continue;
                s = s.Replace("ровно 21", "<color=#FFD54A>ровно 21</color>");
                s = s.Replace("Три промаха", "<color=#FF7A66>Три промаха</color>");
                texts.rulesLines[i] = s;
            }
            if (!string.IsNullOrEmpty(texts.highScoreFormat))
            {
                // «Рекорд заведения: {0}» → мелкая кремовая подпись + крупное золотое число.
                // Подпись приподнята (voffset) до середины цифр: иначе мелкий текст «сидит» на строке и всё смотрится ниже центра.
                // Подпись берём из текущей строки без тегов — повторный запуск даёт тот же результат.
                string caption = Regex.Replace(texts.highScoreFormat, "<[^>]+>", "").Replace("{0}", "").Trim().TrimEnd(':').Trim().ToUpperInvariant();
                texts.highScoreFormat = $"<voffset=0.14em><size=60%><color=#EADFC4>{caption}</color></size></voffset>   {{0}}";
            }
            EditorUtility.SetDirty(texts);
        }

        // ---------- Кнопки ----------

        private static void StyleButton(Button b)
        {
            Image img = b.targetGraphic as Image;
            if (img == null || img.sprite == null || img.sprite.name != "ui_button") return;
            img.color = Color.white;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f;
            TMP_Text label = b.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.enableVertexGradient = false;
                label.color = Cream;
                label.characterSpacing = 2f;
                EditorUtility.SetDirty(label);
            }
            EditorUtility.SetDirty(img);
        }

        // ---------- Рекорд ----------

        private static GameObject StyleHighScore(Transform titleRoot, TMP_Text high)
        {
            if (high == null) return null;
            RectTransform plaque = titleRoot.Find("HighScorePlaque") as RectTransform;
            if (plaque == null)
            {
                GameObject go = new GameObject("HighScorePlaque", typeof(RectTransform));
                go.transform.SetParent(titleRoot, false);
                plaque = (RectTransform)go.transform;
                plaque.SetSiblingIndex(high.transform.GetSiblingIndex());

                Image bg = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                bg.transform.SetParent(plaque, false);
                bg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_panel.png");
                bg.type = Image.Type.Sliced;
                bg.color = PanelTint;
                bg.raycastTarget = false;
                Stretch(bg.rectTransform);
            }
            plaque.anchorMin = plaque.anchorMax = new Vector2(0.5f, 0.5f);
            plaque.pivot = new Vector2(0.5f, 0.5f);
            // Ниже кнопки с зазором ~60 px: кнопка — главный акцент, табличка — второй
            plaque.sizeDelta = new Vector2(560f, 76f);
            plaque.anchoredPosition = new Vector2(0f, -350f);
            Image frame = EnsureFrame(plaque, 1);
            if (frame != null) frame.pixelsPerUnitMultiplier = 1f; // рамка вдвое толще, чем у панели правил

            high.transform.SetParent(plaque, false);
            high.transform.SetAsLastSibling();
            high.gameObject.SetActive(true); // прятать теперь будет табличка целиком
            Stretch(high.rectTransform);
            high.rectTransform.offsetMin = new Vector2(24f, 4f);
            high.rectTransform.offsetMax = new Vector2(-24f, -4f);
            high.alignment = TextAlignmentOptions.CenterGeoAligned; // центр по геометрии букв, а не по строке
            high.richText = true;
            high.enableAutoSizing = true;
            high.fontSizeMin = 18f;
            high.fontSizeMax = 36f;
            high.color = Color.white;
            high.enableVertexGradient = true;
            high.colorGradient = new VertexGradient(GoldLight, GoldLight, GoldDeep, GoldDeep);
            EditorUtility.SetDirty(high);
            return plaque.gameObject;
        }

        // ---------- Общее ----------

        // Золотая рамка 9-slice поверх фона и под текстом (как у панелей HUD)
        private static Image EnsureFrame(RectTransform parent, int siblingIndex)
        {
            Image frame = Find<Image>(parent, "Frame");
            if (frame == null)
            {
                frame = new GameObject("Frame", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                frame.transform.SetParent(parent, false);
            }
            frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + "ui_frame_gold.png");
            if (frame.sprite == null) Debug.LogWarning("[CT] Не найден UI/ui_frame_gold.png");
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = FrameScale;
            frame.raycastTarget = false;
            frame.color = Color.white;
            Stretch(frame.rectTransform);
            frame.transform.SetSiblingIndex(Mathf.Min(siblingIndex, parent.childCount - 1));
            return frame;
        }

        private static T Find<T>(Transform parent, string name) where T : Component
        {
            Transform t = parent.Find(name);
            return t != null ? t.GetComponent<T>() : null;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
