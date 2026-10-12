using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Правки к плейтесту (тексты, пепельницы Ур.5, О3, зона Ур.4, крупье)».  12.10, 22:00.
    //   1. Тексты: хрупкий предмет теперь стакан (Кирилл заменил картинку бутылки) — «бутылка» → «стакан» в UiTexts и подсказках.
    //   2. Ур.5 (Hand_05): стаканы-рикошеты → пепельницы (Ashtray). Официантки WaiterRush ставят пепельницы —
    //      на столе нет двух видов стаканов. WaiterRush ищет RicochetObstacle и дочерний Visual — у пепельницы оба есть.
    //   3. О3 (Hand_00c): верхний домик (повёрнут на 90°) лежал на бортике → (−5.45, 3.15), не пересекает соседний.
    //      Модель полёта: 0.24% → 0.24%.
    //   4. Ур.4 (Hand_04): зона напарника 1.2×0.85 → 1.6×1.4, внешний край 8.6 — до бортика.
    //   5. Крупье: фигура (Characters/dealer_body.png — туловище в тёмном жилете + голова) за руками в Dealer.prefab.
    // Повторный запуск безопасен. Hand_00c/04 — Егор, Dealer.prefab — Алексей: «замок» в #locks.
    public static class PlaytestFixes
    {
        private const string Levels = "Assets/_Project/Prefabs/Levels/";
        private const string AshtrayPath = "Assets/_Project/Prefabs/Obstacles/Ashtray.prefab";
        private const string DealerPath = "Assets/_Project/Prefabs/Gameplay/Dealer.prefab";
        private const string DealerBodySprite = "Assets/_Project/Art/Sprites/Characters/dealer_body.png";
        private const string TextsPath = "Assets/_Project/Data/UiTexts.asset";

        // Фигура крупье в локальных координатах Dealer: руки (спрайт 2 ед.) начинаются у x ≈ −1, плечи — там же
        private const float DealerBodyX = -1.2f;
        private const float DealerBodyScale = 1.2f;

        private static readonly (string from, string to)[] TextPairs =
        {
            ("Бутылку задевать нельзя", "Стакан задевать нельзя"),
            ("Бутылка! Закрути сильнее", "Стакан! Сделай дугу круче"),
            ("Бутылки не задевай", "Стаканы не задевай"),
            ("Бутылку впишут в твой счёт", "Стакан впишут в твой счёт"),
            ("Бутылки и чужие карты не трогай", "Стаканы и чужие карты не трогай"),
            ("Эта бутылка стоила дороже карты", "Этот стакан стоил дороже карты"),
        };

        private static readonly (string hand, string hint)[] Hints =
        {
            ("Hand_01", "Стакан на пути — обойди его дугой. Успей до разворота соперников!"),
            ("Hand_04", "Обходи домики там, откуда стакан только что улетел"),
            ("Hand_05", "Официантки ставят пепельницы — отрази от них карту, стаканы обходи"),
        };

        [MenuItem("CardThrow/Правки к плейтесту (тексты, пепельницы Ур.5, О3, зона Ур.4, крупье)")]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("CardThrow", "Это меню уже применено, повторный запуск откатит более поздние правки (старый крупье DealerBody). Запускать?", "Всё равно запустить", "Отмена"))
                return;
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            FixTexts();
            foreach (var h in Hints) Edit(h.hand, root => SetHint(root, h.hint));
            Edit("Hand_05", AshtraysInFinal);
            Edit("Hand_00c", MoveTutorialHouse);
            Edit("Hand_04", root => SetPartnerZone(root, -0.1f, new Vector2(1.6f, 1.4f)));
            AddDealerBody();

            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Правки к плейтесту применены. Дальше: «Уровни: проверить проходимость» и Play.");
        }

        // ---------- 1. Тексты ----------

        private static void FixTexts()
        {
            UiTexts texts = AssetDatabase.LoadAssetAtPath<UiTexts>(TextsPath);
            if (texts == null) { Debug.LogWarning($"[CT] Нет {TextsPath}"); return; }
            SerializedObject so = new SerializedObject(texts);
            SerializedProperty p = so.GetIterator();
            int changed = 0;
            while (p.Next(true))
            {
                if (p.propertyType != SerializedPropertyType.String) continue;
                string s = p.stringValue, r = s;
                foreach (var t in TextPairs) r = r.Replace(t.from, t.to);
                if (r != s) { p.stringValue = r; changed++; }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[CT] UiTexts: «бутылка» → «стакан» в {changed} строках");
        }

        private static void SetHint(GameObject root, string hint)
        {
            LevelConfig level = root.GetComponent<LevelConfig>();
            if (level == null) return;
            SerializedObject so = new SerializedObject(level);
            so.FindProperty("hint").stringValue = hint;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- 2. Пепельницы на финале ----------

        private static void AshtraysInFinal(GameObject root)
        {
            GameObject ashtray = AssetDatabase.LoadAssetAtPath<GameObject>(AshtrayPath);
            if (ashtray == null) { Debug.LogError($"[CT] Нет {AshtrayPath} — сначала меню «Препятствия: пепельница вместо стакана»"); return; }
            List<Transform> glasses = FindInstances(root, "Glass");
            for (int i = 0; i < glasses.Count; i++)
            {
                Transform g = glasses[i];
                GameObject ash = (GameObject)PrefabUtility.InstantiatePrefab(ashtray, g.parent);
                ash.name = i == 0 ? "Ashtray" : $"Ashtray ({i})";
                ash.transform.localPosition = g.localPosition;
                ash.transform.SetSiblingIndex(g.GetSiblingIndex());
                Object.DestroyImmediate(g.gameObject);
            }
            Debug.Log($"[CT] Hand_05: стаканов → пепельниц: {glasses.Count}");
        }

        // ---------- 3. Домик в О3 ----------

        private static void MoveTutorialHouse(GameObject root)
        {
            // Повёрнутый на 90° домик — тот, что лежал у бортика
            foreach (Transform t in FindInstances(root, "CardHouse"))
            {
                float z = t.localEulerAngles.z;
                bool rotated = Mathf.Abs(Mathf.DeltaAngle(z, 90f)) < 5f || Mathf.Abs(Mathf.DeltaAngle(z, -90f)) < 5f;
                if (!rotated) continue;
                t.localPosition = new Vector3(-5.45f, 3.15f, t.localPosition.z);
                Debug.Log("[CT] Hand_00c: верхний домик → (−5.45, 3.15)");
            }
        }

        // ---------- 4. Зона напарника ----------

        private static void SetPartnerZone(GameObject root, float localX, Vector2 size)
        {
            LevelConfig level = root.GetComponent<LevelConfig>();
            Zone zone = level != null && level.PartnerSeat != null ? level.PartnerSeat.Zone : null;
            BoxCollider2D box = zone != null ? zone.GetComponent<BoxCollider2D>() : null;
            if (box == null) { Debug.LogWarning($"[CT] {root.name}: не нашёл зону напарника"); return; }
            zone.transform.localPosition = new Vector3(localX, 0f, zone.transform.localPosition.z);
            box.offset = Vector2.zero;
            box.size = size;
            Debug.Log($"[CT] {root.name}: зона напарника {size.x}×{size.y}");
        }

        // ---------- 5. Фигура крупье ----------

        private static void AddDealerBody()
        {
            TextureImporter ti = AssetImporter.GetAtPath(DealerBodySprite) as TextureImporter;
            if (ti == null) { Debug.LogWarning($"[CT] Нет {DealerBodySprite}"); return; }
            if (ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 200f || ti.mipmapEnabled)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 200f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(DealerBodySprite);

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
                    body.localPosition = new Vector3(DealerBodyX, 0f, 0f);
                    body.localScale = Vector3.one * DealerBodyScale;
                }
                body.localRotation = Quaternion.Euler(0f, 0f, -90f); // спрайт нарисован «руками вверх» → к столу (+X)
                SpriteRenderer sr = body.GetComponent<SpriteRenderer>();
                if (sr == null) sr = body.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                if (handSr != null)
                {
                    sr.sortingLayerID = handSr.sortingLayerID;
                    sr.sortingOrder = handSr.sortingOrder - 1; // под руками и картами крупье
                }
                PrefabUtility.SaveAsPrefabAsset(root, DealerPath);
                Debug.Log("[CT] Dealer.prefab: фигура крупье DealerBody (подгони X и Scale в Play, если нужно)");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------- Общее ----------

        private static void Edit(string hand, System.Action<GameObject> change)
        {
            string path = Levels + hand + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning($"[CT] Нет {path}"); return; }
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                change(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static List<Transform> FindInstances(GameObject root, string prefabName)
        {
            List<Transform> result = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                if (source != null && source.name == prefabName) result.Add(t);
            }
            return result;
        }
    }
}
