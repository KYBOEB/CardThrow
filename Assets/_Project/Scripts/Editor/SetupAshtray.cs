using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Препятствия: пепельница вместо стакана (Hand_00c, Hand_02)».  Решение команды 12.10:
    // гранёный стакан диаметром 0.9 ед. выглядел «как ведро». Рикошет теперь — от большой хрустальной пепельницы
    // (её размер правдоподобен). Стаканы остаются на уровне с официантками (Hand_05, WaiterRush их переставляет).
    //   1. Prefabs/Obstacles/Ashtray.prefab — копия Glass.prefab (тот же RicochetObstacle и восьмиугольник r = 0.45,
    //      слой Obstacle) со спрайтом Art/Sprites/Obstacles/ashtray.png, подогнанным под диаметр коллайдера.
    //      Физика та же → проходимость уровней не меняется, превью и инструмент проверки работают как раньше.
    //   2. В Hand_00c и Hand_02 каждый Glass заменяется на Ashtray в той же точке (в Hand_00c — сначала оставляем 2 крайних).
    //   3. Тексты: подсказка Hand_02 и шаги обучения в UiTexts — «пепельница» вместо «стакан».
    // Повторный запуск безопасен: обновит спрайт/масштаб (если Кирилл заменил ashtray.png), стаканов в 00c/02 уже не найдёт.
    // Hand_00c, Hand_02 — файлы Егора, Glass.prefab — Алексея (только читается): «замок» в #locks на Hand_00c и Hand_02.
    public static class SetupAshtray
    {
        private const string Obstacles = "Assets/_Project/Prefabs/Obstacles/";
        private const string Levels = "Assets/_Project/Prefabs/Levels/";
        private const string GlassPath = Obstacles + "Glass.prefab";
        private const string AshtrayPath = Obstacles + "Ashtray.prefab";
        private const string SpritePath = "Assets/_Project/Art/Sprites/Obstacles/ashtray.png";
        private const string TextsPath = "Assets/_Project/Data/UiTexts.asset";
        private static readonly string[] Hands = { "Hand_00c", "Hand_02" };

        private const string Hint02 = "Пепельница отражает карту. Целься в её левую грань — карта уйдёт вверх";
        private const string TutorialRicochet = "Путь закрыт. Отрази карту от пепельницы: целься в её грань, тяни ЛКМ и отпусти";
        private const string TutorialSuccessSpin = "Красиво! Последний приём — рикошет от пепельницы";

        [MenuItem("CardThrow/Препятствия: пепельница вместо стакана (Hand_00c, Hand_02)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            Sprite sprite = ImportSprite();
            if (sprite == null) { Debug.LogError($"[CT] Нет {SpritePath} — положи картинку и запусти ещё раз"); return; }

            GameObject ashtray = BuildPrefab(sprite);
            if (ashtray == null) return;

            foreach (string hand in Hands) ReplaceGlasses(hand, ashtray);
            UpdateTexts();

            AssetDatabase.SaveAssets();
            Debug.Log("[CT] Пепельница: префаб готов, стаканы в Hand_00c и Hand_02 заменены, тексты обновлены. " +
                      "Дальше: «Уровни: проверить проходимость» — цифры должны остаться прежними.");
        }

        // Импорт как у остальных препятствий: Sprite, PPU 200, без мип-карт
        private static Sprite ImportSprite()
        {
            TextureImporter ti = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (ti == null) return null;
            if (ti.textureType != TextureImporterType.Sprite || ti.spritePixelsPerUnit != 200f || ti.mipmapEnabled ||
                ti.spriteImportMode != SpriteImportMode.Single)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 200f;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }

        private static GameObject BuildPrefab(Sprite sprite)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(AshtrayPath) == null)
            {
                if (!AssetDatabase.CopyAsset(GlassPath, AshtrayPath))
                {
                    Debug.LogError($"[CT] Не удалось скопировать {GlassPath} → {AshtrayPath}");
                    return null;
                }
                AssetDatabase.ImportAsset(AshtrayPath);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(AshtrayPath);
            try
            {
                Transform visual = root.transform.Find("Visual");
                SpriteRenderer sr = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
                if (sr == null)
                {
                    Debug.LogError("[CT] В Ashtray.prefab нет дочернего Visual со SpriteRenderer — Glass.prefab изменился?");
                    return null;
                }
                sr.sprite = sprite;

                // Картинку — ровно под диаметр коллайдера (как делает меню Алексея для стакана); сам корень не масштабируем
                float radius = 0.45f;
                PolygonCollider2D poly = root.GetComponent<PolygonCollider2D>();
                if (poly != null && poly.pathCount > 0)
                {
                    radius = 0f;
                    foreach (Vector2 p in poly.GetPath(0)) radius = Mathf.Max(radius, p.magnitude);
                }
                float k = 2f * radius / Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
                visual.localScale = new Vector3(k, k, 1f);

                PrefabUtility.SaveAsPrefabAsset(root, AshtrayPath);
                Debug.Log($"[CT] Ashtray.prefab: восьмиугольник r = {radius:0.##}, картинка {sprite.bounds.size.x:0.##} ед. × {k:0.##}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(AshtrayPath);
        }

        private static void ReplaceGlasses(string hand, GameObject ashtrayPrefab)
        {
            string path = Levels + hand + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) { Debug.LogWarning($"[CT] Нет {path}"); return; }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                List<Transform> glasses = FindInstances(root, "Glass");
                if (glasses.Count == 0) { Debug.Log($"[CT] {hand}: стаканов нет — уже заменено"); return; }

                // Hand_00c: три стакана стопкой налезали друг на друга — оставляем два крайних (стенка та же, 0.27% → 0.28%)
                if (glasses.Count >= 3)
                {
                    glasses.Sort((a, b) => a.localPosition.y.CompareTo(b.localPosition.y));
                    for (int i = glasses.Count - 2; i >= 1; i--)
                    {
                        Object.DestroyImmediate(glasses[i].gameObject);
                        glasses.RemoveAt(i);
                    }
                }

                for (int i = 0; i < glasses.Count; i++)
                {
                    Transform g = glasses[i];
                    GameObject ash = (GameObject)PrefabUtility.InstantiatePrefab(ashtrayPrefab, g.parent);
                    ash.name = i == 0 ? "Ashtray" : $"Ashtray ({i})";
                    ash.transform.localPosition = g.localPosition;
                    ash.transform.localRotation = g.localRotation;
                    ash.transform.SetSiblingIndex(g.GetSiblingIndex());
                    Object.DestroyImmediate(g.gameObject);
                }

                if (hand == "Hand_02")
                {
                    LevelConfig level = root.GetComponent<LevelConfig>();
                    if (level != null)
                    {
                        SerializedObject so = new SerializedObject(level);
                        so.FindProperty("hint").stringValue = Hint02;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[CT] {hand}: стаканов → пепельниц: {glasses.Count}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateTexts()
        {
            UiTexts texts = AssetDatabase.LoadAssetAtPath<UiTexts>(TextsPath);
            if (texts == null) { Debug.LogWarning($"[CT] Нет {TextsPath}"); return; }
            Undo.RecordObject(texts, "Пепельница в текстах");
            texts.tutorialRicochetStep = TutorialRicochet;
            texts.tutorialSuccessSpin = TutorialSuccessSpin;
            EditorUtility.SetDirty(texts);
        }

        private static List<Transform> FindInstances(GameObject root, string prefabName)
        {
            List<Transform> result = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) continue;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject);
                string n = source != null ? source.name : t.name;
                if (n == prefabName) result.Add(t);
            }
            return result;
        }
    }
}
