using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Фон: декор у стола (пальмы и лаунж-столики)».  Плейтест 12.10: «вокруг поля слишком пусто».
    // В открытой сцене Main создаёт объект Decor с 4 спрайтами по диагоналям от углов стола:
    //   пальма в кадке (Art/Sprites/Background/decor_palm.png) и лаунж-столик с креслами (decor_lounge.png).
    // Вид строго сверху, как стол и игроки. Sorting Layer Table, order −50: над полом (−100), под всеми игроками,
    // официантками и UI. Чуть затемнены — фон не спорит со столом. Коллайдеров нет — на игру не влияют.
    // На экране 16:9 декор выглядывает из-за краёв, на широком — виден целиком. Повторный запуск безопасен.
    public static class BuildDecor
    {
        private const string Dir = "Assets/_Project/Art/Sprites/Background/";
        private const string RootName = "Decor";
        private static readonly Color Tint = new Color(0.82f, 0.8f, 0.8f, 1f);

        private struct Item { public string name, sprite; public Vector2 pos; public float rot; }

        private static readonly Item[] Items =
        {
            new Item { name = "Palm_TopLeft",      sprite = "decor_palm",   pos = new Vector2(-13.3f,  4.3f), rot = 0f },
            new Item { name = "Lounge_BottomLeft", sprite = "decor_lounge", pos = new Vector2(-13.4f, -4.1f), rot = 90f },
            new Item { name = "Lounge_TopRight",   sprite = "decor_lounge", pos = new Vector2( 13.4f,  4.1f), rot = 90f },
            new Item { name = "Palm_BottomRight",  sprite = "decor_palm",   pos = new Vector2( 13.3f, -4.3f), rot = 40f },
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
                t.localPosition = new Vector3(it.pos.x, it.pos.y, 0f);
                t.localRotation = Quaternion.Euler(0f, 0f, it.rot);
                t.localScale = Vector3.one;

                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr == null) sr = t.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = Tint;
                sr.sortingLayerName = "Table";
                sr.sortingOrder = -50;
            }

            EditorSceneManager.MarkSceneDirty(root.scene);
            EditorSceneManager.SaveScene(root.scene);
            Debug.Log("[CT] Декор: 2 пальмы и 2 лаунж-столика у углов стола (Table, order −50). Сцена сохранена.");
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
