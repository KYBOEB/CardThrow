using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Фон: декор у стола (пальмы и лаунж-столики)».  Плейтест 12.10.
    // Объект Decor в сцене Main: второй план, затемнён, рисуется над полом и под всем остальным
    // (Sorting Layer Table, order −50), без коллайдеров.
    //   • 4 пальмы — в углах ЭКРАНА, видна примерно треть кроны (ViewportAnchor держит угол при любом разрешении).
    //   • 2 лаунж-столика — у левого и правого края экрана на уровне стола, видна половина.
    // Повторный запуск безопасен: обновляет размеры, затемнение и привязки.
    public static class BuildDecor
    {
        private const string Dir = "Assets/_Project/Art/Sprites/Background/";
        private const string RootName = "Decor";
        private static readonly Color PalmTint = new Color(0.42f, 0.45f, 0.42f, 1f);
        private static readonly Color LoungeTint = new Color(0.45f, 0.42f, 0.42f, 1f);

        private struct Item { public string name, sprite; public Vector2 viewport, offset; public float rot, scale; public Color tint; }

        private static readonly Item[] Items =
        {
            new Item { name = "Palm_TopLeft",     sprite = "decor_palm",   viewport = new Vector2(0f, 1f),   offset = new Vector2( 0.7f, -0.7f), rot = 0f,   scale = 1.6f, tint = PalmTint },
            new Item { name = "Palm_TopRight",    sprite = "decor_palm",   viewport = new Vector2(1f, 1f),   offset = new Vector2(-0.7f, -0.7f), rot = 70f,  scale = 1.5f, tint = PalmTint },
            new Item { name = "Palm_BottomLeft",  sprite = "decor_palm",   viewport = new Vector2(0f, 0f),   offset = new Vector2( 0.7f,  0.7f), rot = 140f, scale = 1.5f, tint = PalmTint },
            new Item { name = "Palm_BottomRight", sprite = "decor_palm",   viewport = new Vector2(1f, 0f),   offset = new Vector2(-0.7f,  0.7f), rot = 40f,  scale = 1.6f, tint = PalmTint },
            new Item { name = "Lounge_Left",      sprite = "decor_lounge", viewport = new Vector2(0f, 0.5f), offset = new Vector2(-0.2f,  0f),   rot = 0f,   scale = 1.3f, tint = LoungeTint },
            new Item { name = "Lounge_Right",     sprite = "decor_lounge", viewport = new Vector2(1f, 0.5f), offset = new Vector2( 0.2f,  0f),   rot = 0f,   scale = 1.3f, tint = LoungeTint },
        };

        [MenuItem("CardThrow/Фон: декор у стола (пальмы и лаунж-столики)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            GameObject root = GameObject.Find(RootName);
            if (root == null)
            {
                root = new GameObject(RootName);
                Undo.RegisterCreatedObjectUndo(root, "Декор");
            }
            root.transform.position = Vector3.zero;

            // Старые объекты прошлой раскладки (Lounge_BottomLeft и т.п.) — убрать
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                string n = root.transform.GetChild(i).name;
                if (System.Array.FindIndex(Items, it => it.name == n) < 0) Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            Camera cam = Camera.main;
            foreach (Item it in Items)
            {
                Sprite sprite = Import(it.sprite);
                if (sprite == null) { Debug.LogWarning($"[CT] Нет {Dir}{it.sprite}.png"); continue; }

                Transform t = root.transform.Find(it.name);
                if (t == null)
                {
                    t = new GameObject(it.name).transform;
                    t.SetParent(root.transform, false);
                }
                t.localRotation = Quaternion.Euler(0f, 0f, it.rot);
                t.localScale = Vector3.one * it.scale;
                // Позиция в редакторе — как на экране 16:9; в игре ViewportAnchor поставит по реальному окну
                Vector3 p = cam != null ? cam.ViewportToWorldPoint(new Vector3(it.viewport.x, it.viewport.y, 0f)) : Vector3.zero;
                t.position = new Vector3(p.x + it.offset.x, p.y + it.offset.y, 0f);

                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = it.tint;
                sr.sortingLayerName = "Table";
                sr.sortingOrder = -50;

                ViewportAnchor anchor = t.GetComponent<ViewportAnchor>();
                if (anchor == null) anchor = t.gameObject.AddComponent<ViewportAnchor>();
                SerializedObject so = new SerializedObject(anchor);
                so.FindProperty("viewport").vector2Value = it.viewport;
                so.FindProperty("offset").vector2Value = it.offset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Debug.Log("[CT] Декор: 4 пальмы по углам экрана и 2 лаунж-столика по бокам, затемнены (Table, order −50). Сцена сохранена.");
        }

        // Импорт как у остального арта: Sprite, PPU 200, без мип-карт
        private static Sprite Import(string name)
        {
            string path = Dir + name + ".png";
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return null;
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
    }
}
