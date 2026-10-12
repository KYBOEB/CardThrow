using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Создать UI обучения».
    // Создаёт (или пересоздаёт) Canvas «UI» → TutorialPanel с иконками, стрелкой и подсказкой «Tab — пропустить»
    // и сам заполняет все поля TutorialController в [Systems]. Остальную сцену НЕ трогает.
    // Повторный запуск безопасен: удаляет только старый TutorialPanel и строит его заново.
    public static class BuildTutorialUI
    {
        private const string Root = "Assets/_Project";
        private const string SpriteDir = Root + "/Art/Sprites/UI/";
        private const string FontPath = Root + "/Art/Fonts/Montserrat-Bold SDF.asset";
        private const string TextsPath = Root + "/Data/UiTexts.asset";

        // Иконки управления: компактнее и в «золоте» стола (белые клавиши × золотой тинт = золотые клавиши с тёмными буквами)
        private const float KeySize = 64f;
        private const float MouseSize = 84f;
        private static readonly Color IconTint = new Color(1f, 0.84f, 0.45f);

        [MenuItem("CardThrow/Создать UI обучения")]
        public static void Build()
        {
            TutorialController controller = Object.FindAnyObjectByType<TutorialController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "В открытой сцене нет TutorialController. Открой Scenes/Main.unity.", "OK");
                return;
            }

            UiTexts texts = AssetDatabase.LoadAssetAtPath<UiTexts>(TextsPath);
            if (texts == null)
            {
                texts = ScriptableObject.CreateInstance<UiTexts>();
                AssetDatabase.CreateAsset(texts, TextsPath);
                AssetDatabase.SaveAssets();
                Debug.Log($"[CT] Создан {TextsPath}");
            }

            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) Debug.LogWarning($"[CT] Не найден шрифт {FontPath} — будет шрифт TMP по умолчанию (без кириллицы может быть пусто)");

            Canvas canvas = FindOrCreateCanvas();

            // Пересоздаём только свою панель
            Transform old = canvas.transform.Find("TutorialPanel");
            if (old != null) Undo.DestroyObjectImmediate(old.gameObject);

            // --- TutorialPanel: растянут на весь экран, сам ничего не рисует ---
            RectTransform panel = NewRect("TutorialPanel", canvas.transform);
            Stretch(panel);
            panel.SetAsLastSibling(); // поверх остального UI

            // --- Стрелка (спрайт смотрит вниз → arrowSpriteAngle = −90) ---
            Image arrow = NewImage("Arrow", panel, "tut_arrow", new Vector2(110, 110));
            arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

            // --- Ряд иконок над рукой крупье ---
            RectTransform icons = NewRect("Icons", panel);
            // Позицию в Play каждый кадр задаёт TutorialController (рядом с крупье); здесь — запасная
            icons.anchorMin = icons.anchorMax = new Vector2(0f, 0.5f);
            icons.pivot = new Vector2(0f, 0.5f);
            icons.anchoredPosition = new Vector2(120f, 150f);
            icons.sizeDelta = new Vector2(480f, MouseSize);
            HorizontalLayoutGroup row = icons.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 10f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = false;
            row.childForceExpandWidth = row.childForceExpandHeight = false;

            // Клавиши и мышь — цветные бордово-золотые спрайты, их не тонируем (IconTint был для белых заглушек)
            GameObject keyA = NewImage("Key_A", icons, "icon_key_a", new Vector2(KeySize, KeySize)).gameObject;
            GameObject keyD = NewImage("Key_D", icons, "icon_key_d", new Vector2(KeySize, KeySize)).gameObject;
            GameObject wheel = NewImage("MouseWheel", icons, "icon_mouse_wheel", new Vector2(MouseSize, MouseSize)).gameObject;
            GameObject keyQ = NewImage("Key_Q", icons, "icon_key_q", new Vector2(KeySize, KeySize)).gameObject;
            GameObject keyE = NewImage("Key_E", icons, "icon_key_e", new Vector2(KeySize, KeySize)).gameObject;

            // Слот нужен, чтобы Layout Group двигал слот, а код анимировал иконку внутри
            RectTransform dragSlot = NewRect("MouseDragSlot", icons);
            dragSlot.sizeDelta = new Vector2(MouseSize, MouseSize);
            Image drag = NewImage("MouseDrag", dragSlot, "icon_mouse_drag", new Vector2(MouseSize, MouseSize)); // цветной спрайт — без тинта
            drag.rectTransform.anchorMin = drag.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            drag.rectTransform.anchoredPosition = Vector2.zero;

            // --- «Tab — пропустить» в правом верхнем углу (кликабельно) ---
            Image skipBg = NewImage("SkipHint", panel, "ui_panel", new Vector2(400, 64)); // ширина с запасом: текст в одну строку
            RectTransform skipRect = skipBg.rectTransform;
            skipRect.anchorMin = skipRect.anchorMax = skipRect.pivot = new Vector2(1f, 1f);
            skipRect.anchoredPosition = new Vector2(-24f, -24f);
            skipBg.type = skipBg.sprite != null && skipBg.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            skipBg.raycastTarget = true; // единственный кликабельный элемент
            Button skipButton = skipBg.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipBg;

            Image tabIcon = NewImage("Icon", skipRect, "icon_key_tab", new Vector2(48, 48));
            tabIcon.rectTransform.anchorMin = tabIcon.rectTransform.anchorMax = tabIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
            tabIcon.rectTransform.anchoredPosition = new Vector2(12f, 0f);

            RectTransform labelRect = NewRect("Label", skipRect);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(68f, 6f);  // место под иконку Tab
            labelRect.offsetMax = new Vector2(-14f, -6f);
            TextMeshProUGUI label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = texts.tutorialSkipHint;
            label.fontSize = 26f;
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.color = Color.white;
            label.raycastTarget = false;
            if (font != null) label.font = font;
            DisableWrapping(label);

            // --- Связи в TutorialController ---
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("texts").objectReferenceValue = texts;
            so.FindProperty("tutorialRoot").objectReferenceValue = panel.gameObject;
            SetArray(so.FindProperty("step1Icons"), keyA, keyD);
            SetArray(so.FindProperty("step2Icons"), wheel, keyQ, keyE);
            SetArray(so.FindProperty("step3Icons"), dragSlot.gameObject);
            so.FindProperty("arrow").objectReferenceValue = arrow.rectTransform;
            so.FindProperty("skipHintLabel").objectReferenceValue = label;
            so.FindProperty("skipButton").objectReferenceValue = skipButton;
            so.FindProperty("dragIcon").objectReferenceValue = drag.rectTransform;
            so.FindProperty("iconsRow").objectReferenceValue = icons;
            DealerHand dealer = Object.FindAnyObjectByType<DealerHand>(FindObjectsInactive.Include);
            so.FindProperty("iconsAnchor").objectReferenceValue = dealer != null ? dealer.transform : null;
            if (dealer == null) Debug.LogWarning("[CT] В сцене нет Dealer — иконки останутся в левой части экрана");
            so.FindProperty("arrowSpriteAngle").floatValue = -90f;
            so.ApplyModifiedProperties();

            // В редакторе без Play панель скрыта, чтобы не наслаиваться на титул.
            // В Play её включает TutorialController на уровне 0 (ссылки на неактивные объекты работают).
            panel.gameObject.SetActive(false);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("[CT] UI обучения собран и подключён к TutorialController. Сохрани сцену (Ctrl+S).");
        }

        // ---------- Вспомогательное ----------

        private static Image Tint(Image img)
        {
            img.color = IconTint;
            return img;
        }

        private static Canvas FindOrCreateCanvas()
        {
            Canvas existing = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (existing != null) return existing.rootCanvas;

            GameObject go = new GameObject("UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(go, "Create UI Canvas");
            go.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Debug.Log("[CT] Создан Canvas «UI» (Overlay, 1920×1080, Match 0.5)");
            return canvas;
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image NewImage(string name, Transform parent, string spriteName, Vector2 size)
        {
            RectTransform rt = NewRect(name, parent);
            rt.sizeDelta = size;
            Image img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + spriteName + ".png");
            img.preserveAspect = true;
            img.raycastTarget = false; // не перехватывать клик ЛКМ, которым игрок бросает карту
            if (img.sprite == null) Debug.LogWarning($"[CT] Не найден спрайт {SpriteDir}{spriteName}.png (Texture Type должен быть Sprite)");
            return img;
        }

        // «Tab — пропустить» всегда в одну строку. Через SerializedObject, чтобы не зависеть от версии API TextMeshPro:
        // в Unity 6 поле называется m_TextWrappingMode (0 = NoWrap), в старых версиях — m_enableWordWrapping.
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

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static void SetArray(SerializedProperty prop, params GameObject[] items)
        {
            prop.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
