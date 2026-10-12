using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Дуга вместо закрута, зона Ур.3, фигура крупье».  12.10, 22:10.
    //   1. Тексты: команда называет закрут «дугой» — UiTexts, подсказки уровней, название О2.
    //   2. Ур.3 (Hand_03): зона напарника стандартного размера 1.6×1.4 (как Ур.4 и Ур.5), край ровно до бортика.
    //   3. Крупье: фигура размером как у игроков (dealer_body.png 640×480 = 3.2×2.4 ед., масштаб 1 — как игрок ×2):
    //      белая рубашка, тёмный жилет, рыжий. Рукава нарисованы от плеч к точкам (−0.95; ±0.3) — ровно туда, где
    //      начинаются рукава спрайта рук крупье. Поэтому центр фигуры x = −1.6, поворот −90° (руки к столу).
    // Повторный запуск безопасен.
    public static class ArcAndDealer
    {
        private const string Levels = "Assets/_Project/Prefabs/Levels/";
        private const string DealerPath = "Assets/_Project/Prefabs/Gameplay/Dealer.prefab";
        private const string BodySprite = "Assets/_Project/Art/Sprites/Characters/dealer_body.png";
        private const string TextsPath = "Assets/_Project/Data/UiTexts.asset";

        private static readonly (string from, string to)[] TextPairs =
        {
            ("Закрути карту: колесо мыши или Q / E — она полетит дугой", "Пусти карту дугой: колесо мыши или Q / E"),
            ("Закрути карту: колесо мыши или Q / E", "Пусти карту дугой: колесо мыши или Q / E"),
            ("Теперь научимся закручивать карту", "Теперь научимся пускать карту дугой"),
            ("Поправь прицел или закрут", "Поправь прицел или дугу"),
            ("Стакан! Закрути сильнее", "Стакан! Сделай дугу круче"),
            ("Колесо или Q / E — закрут", "Колесо или Q / E — дуга"),
            ("Закрут {0}", "Дуга {0}"),
        };

        private static readonly (string hand, string hint)[] Hints =
        {
            ("Hand_01", "Стакан на пути — обойди его дугой. Успей до разворота соперников!"),
            ("Hand_03", "Домик перед напарником — обойди его дугой. Фишки тормозят карту"),
        };

        [MenuItem("CardThrow/Дуга вместо закрута, зона Ур.3, фигура крупье")]
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
            foreach (var h in Hints) Edit(h.hand, root => SetLevelString(root, "hint", h.hint));
            Edit("Hand_00b", root => SetLevelString(root, "handTitle", "Дуга"));
            Edit("Hand_03", SetStandardZone);
            SetupDealer();

            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Дуга, зона Ур.3, крупье — готово. Дальше: «Уровни: проверить проходимость» и Play.");
        }

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
            Debug.Log($"[CT] UiTexts: «закрут» → «дуга» в {changed} строках");
        }

        private static void SetLevelString(GameObject root, string field, string value)
        {
            LevelConfig level = root.GetComponent<LevelConfig>();
            if (level == null) return;
            SerializedObject so = new SerializedObject(level);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogWarning($"[CT] У LevelConfig нет поля {field}"); return; }
            p.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStandardZone(GameObject root)
        {
            LevelConfig level = root.GetComponent<LevelConfig>();
            Zone zone = level != null && level.PartnerSeat != null ? level.PartnerSeat.Zone : null;
            BoxCollider2D box = zone != null ? zone.GetComponent<BoxCollider2D>() : null;
            if (box == null) { Debug.LogWarning("[CT] Hand_03: не нашёл зону напарника"); return; }
            zone.transform.localPosition = new Vector3(-0.1f, 0f, zone.transform.localPosition.z);
            box.offset = Vector2.zero;
            box.size = new Vector2(1.6f, 1.4f);
            Debug.Log("[CT] Hand_03: зона напарника 1.6×1.4");
        }

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
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BodySprite);

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
                body.localPosition = new Vector3(-1.6f, 0f, 0f);
                body.localRotation = Quaternion.Euler(0f, 0f, -90f);
                body.localScale = Vector3.one;
                SpriteRenderer sr = body.GetComponent<SpriteRenderer>();
                if (sr == null) sr = body.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                if (handSr != null)
                {
                    sr.sortingLayerID = handSr.sortingLayerID;
                    sr.sortingOrder = handSr.sortingOrder - 1;
                }
                PrefabUtility.SaveAsPrefabAsset(root, DealerPath);
                Debug.Log("[CT] Dealer.prefab: фигура крупье 3.2×2.4 ед. в (−1.6, 0), рукава стыкуются с руками");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

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
    }
}
