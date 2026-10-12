using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Финальная полировка (победа, крупье, стакан)».  12.10, 23:00.
    //   1. Экран победы: орнаментальная золотая рамка, крупная надпись «67 000 000 $» (набегает), фейерверки
    //      по бокам с хлопками (VictoryCelebration). Существующие тексты EndScreen перекладываются, ничего не удаляется.
    //   2. Крупье: фигура-игрок с тремя положениями рук (dealer_idle / dealer_pinch / dealer_pull) в Hand —
    //      DealerHand сам меняет их по силе оттяга (0 → idle, >0 → pinch, >0.6 → pull). Старый DealerBody удаляется.
    //   3. Стакан (Ур.5): картинка под диаметр коллайдера r = 0.45 (то, что делало меню «Алексей → 5»).
    // Повторный запуск безопасен. Dealer.prefab и Glass.prefab — Алексея (правки только визуала).
    public static class FinalPolish
    {
        private const string Root = "Assets/_Project/";
        private const string FramePath = Root + "Art/Sprites/UI/ui_frame_ornate.png";
        private const string PopPath = Root + "Audio/SFX/sfx_party_popper_01.ogg";
        private const string VfxDir = Root + "Art/Sprites/VFX/";
        private const string CharDir = Root + "Art/Sprites/Characters/";
        private const string DealerPath = Root + "Prefabs/Gameplay/Dealer.prefab";
        private const string GlassPath = Root + "Prefabs/Obstacles/Glass.prefab";
        private static readonly Color Gold = new Color(1f, 0.84f, 0.3f);
        private static readonly Color GoldLight = new Color(1f, 0.95f, 0.65f);
        private static readonly Color GoldDeep = new Color(0.95f, 0.62f, 0.15f);

        [MenuItem("CardThrow/Финальная полировка (победа, крупье, стакан)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }
            ImportFrame();
            BuildVictory();
            SetupDealer();
            FitGlass();
            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Финальная полировка применена.");
        }

        // ---------- 1. Победа ----------

        private static void ImportFrame()
        {
            TextureImporter ti = AssetImporter.GetAtPath(FramePath) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[CT] Нет {FramePath}"); return; }
            Vector4 border = new Vector4(72, 72, 72, 72);
            if (ti.textureType == TextureImporterType.Sprite && ti.spriteBorder == border && !ti.mipmapEnabled) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.spriteBorder = border;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }

        private static void BuildVictory()
        {
            TitleScreen title = Object.FindAnyObjectByType<TitleScreen>(FindObjectsInactive.Include);
            if (title == null) { EditorUtility.DisplayDialog("CardThrow", "Открой Scenes/Main.unity.", "OK"); return; }
            Canvas canvas = title.GetComponentInParent<Canvas>();
            Transform victory = canvas.transform.Find("VictoryScreen");
            RectTransform panel = victory != null ? victory.Find("Panel") as RectTransform : null;
            if (panel == null) { Debug.LogError("[CT] Нет VictoryScreen/Panel в Canvas"); return; }
            TMP_Text sample = title.GetComponentInChildren<TMP_Text>(true);

            panel.sizeDelta = new Vector2(1100f, 720f);

            // Рамка с узором поверх фона панели, под текстами
            Image frame = GetOrAdd<Image>(FindOrCreate("OrnateFrame", panel));
            frame.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = 1f;
            frame.raycastTarget = false;
            RectTransform fr = frame.rectTransform;
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one;
            fr.offsetMin = new Vector2(-22f, -22f); fr.offsetMax = new Vector2(22f, 22f);
            fr.SetSiblingIndex(1);

            Top(panel, "Title", 34f, 96f);
            // Крупный джекпот
            TextMeshProUGUI jackpot = GetOrAdd<TextMeshProUGUI>(FindOrCreate("Jackpot", panel));
            if (sample != null) jackpot.font = sample.font;
            jackpot.text = "67 000 000 $";
            jackpot.alignment = TextAlignmentOptions.Center;
            jackpot.enableAutoSizing = true; jackpot.fontSizeMin = 60f; jackpot.fontSizeMax = 120f;
            jackpot.color = Color.white;
            jackpot.enableVertexGradient = true;
            jackpot.colorGradient = new VertexGradient(GoldLight, GoldLight, GoldDeep, GoldDeep);
            jackpot.characterSpacing = 2f;
            jackpot.raycastTarget = false;
            Top(panel, "Jackpot", 130f, 150f);
            Top(panel, "Subtitle", 290f, 56f);
            Top(panel, "FinalScore", 352f, 58f);
            Top(panel, "HighScore", 418f, 46f);
            Top(panel, "NewRecord", 470f, 52f);

            // Фейерверки по бокам от панели (под панелью, над затемнением)
            RectTransform left = Side(victory, "FireworksLeft", 0f);
            RectTransform right = Side(victory, "FireworksRight", 1f);

            // Хостес слева у нижнего края экрана, поверх фейерверка и края панели (рука «выглядывает» на панель)
            Image hostess = GetOrAdd<Image>(FindOrCreate("Hostess", victory));
            hostess.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CharDir + "hostess.png");
            hostess.preserveAspect = true;
            hostess.raycastTarget = false;
            RectTransform hr = hostess.rectTransform;
            hr.anchorMin = hr.anchorMax = hr.pivot = Vector2.zero;
            hr.anchoredPosition = new Vector2(-10f, 0f);
            hr.sizeDelta = new Vector2(470f, 705f);
            hr.SetAsLastSibling();

            AudioSource src = GetOrAdd<AudioSource>(victory);
            src.playOnAwake = false; src.loop = false; src.spatialBlend = 0f;

            VictoryCelebration cel = GetOrAdd<VictoryCelebration>(victory);
            SerializedObject so = new SerializedObject(cel);
            so.FindProperty("jackpotLabel").objectReferenceValue = jackpot;
            so.FindProperty("leftZone").objectReferenceValue = left;
            so.FindProperty("rightZone").objectReferenceValue = right;
            SerializedProperty sprites = so.FindProperty("particleSprites");
            string[] names = { "fx_sparkle", "fx_star", "fx_coin" };
            sprites.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(VfxDir + names[i] + ".png");
            so.FindProperty("audioSource").objectReferenceValue = src;
            so.FindProperty("popClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(PopPath);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            Debug.Log("[CT] Победа: рамка с узором, «67 000 000 $», фейерверки с хлопками. Сцена сохранена.");
        }

        private static void Top(RectTransform panel, string name, float top, float height)
        {
            RectTransform rt = panel.Find(name) as RectTransform;
            if (rt == null) { Debug.LogWarning($"[CT] VictoryScreen/Panel: нет {name}"); return; }
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(-90f, height);
            rt.anchoredPosition = new Vector2(0f, -top);
        }

        private static RectTransform Side(Transform parent, string name, float x)
        {
            RectTransform rt = FindOrCreate(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(x, 0.5f);
            rt.pivot = new Vector2(x, 0.5f);
            rt.anchoredPosition = new Vector2(x == 0f ? 30f : -30f, 60f);
            rt.sizeDelta = new Vector2(380f, 640f);
            rt.SetSiblingIndex(1); // после Dim, до Panel
            return rt;
        }

        // ---------- 2. Крупье ----------

        private static void SetupDealer()
        {
            Sprite idle = ImportSprite(CharDir + "dealer_idle.png");
            Sprite pinch = ImportSprite(CharDir + "dealer_pinch.png");
            Sprite pull = ImportSprite(CharDir + "dealer_pull.png");
            if (idle == null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(DealerPath);
            try
            {
                Transform hand = root.transform.Find("Hand");
                DealerHand dh = root.GetComponent<DealerHand>();
                if (hand == null || dh == null) { Debug.LogError("[CT] Dealer.prefab: нет Hand или DealerHand"); return; }

                // Фигура как у игроков (Seat): масштаб 2, руками к столу, центр за краем стола
                hand.localPosition = new Vector3(-0.95f, 0f, 0f);
                hand.localRotation = Quaternion.Euler(0f, 0f, -90f);
                hand.localScale = Vector3.one * 2f;
                SpriteRenderer sr = hand.GetComponent<SpriteRenderer>();
                if (sr != null) { sr.enabled = true; sr.sprite = idle; }

                SerializedObject so = new SerializedObject(dh);
                so.FindProperty("handIdle").objectReferenceValue = idle;
                so.FindProperty("handPinch").objectReferenceValue = pinch;
                so.FindProperty("handPull").objectReferenceValue = pull;
                so.ApplyModifiedPropertiesWithoutUndo();

                Transform oldBody = root.transform.Find("DealerBody");
                if (oldBody != null) Object.DestroyImmediate(oldBody.gameObject);

                PrefabUtility.SaveAsPrefabAsset(root, DealerPath);
                Debug.Log("[CT] Крупье: фигура с тремя положениями рук");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Sprite ImportSprite(string path)
        {
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[CT] Нет {path}"); return null; }
            if (ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 200f || ti.mipmapEnabled)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 200f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------- 3. Стакан ----------

        private static void FitGlass()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GlassPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                SpriteRenderer sr = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
                PolygonCollider2D poly = root.GetComponent<PolygonCollider2D>();
                if (sr == null || sr.sprite == null || poly == null || poly.pathCount == 0) return;
                float radius = 0f;
                foreach (Vector2 p in poly.GetPath(0)) radius = Mathf.Max(radius, p.magnitude);
                float k = 2f * radius / Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                visual.localScale = new Vector3(k, k, 1f);
                PrefabUtility.SaveAsPrefabAsset(root, GlassPath);
                Debug.Log($"[CT] Стакан: картинка ×{k:0.##} под коллайдер r = {radius:0.##}");
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

        // Не «GetComponent() ?? AddComponent()»: в редакторе отсутствующий компонент — «фальшивый null»
        private static T GetOrAdd<T>(Component c) where T : Component
        {
            T t = c.GetComponent<T>();
            return t != null ? t : c.gameObject.AddComponent<T>();
        }
    }
}
