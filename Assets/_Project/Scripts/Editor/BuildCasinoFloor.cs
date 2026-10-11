using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Фон: пол казино».
    // Создаёт (или обновляет) в Main объект Floor: SpriteRenderer в режиме Tiled со спрайтом Table/floor_tile
    // под столом (Sorting Layer Table, order −100). Сам выставляет импорт тайла (Full Rect + Repeat),
    // иначе Unity пишет в Console предупреждение про Tiled. Повторный запуск безопасен. Сцену сохраняет.
    public static class BuildCasinoFloor
    {
        private const string TilePath = "Assets/_Project/Art/Sprites/Table/floor_tile.png";
        private const string FloorName = "Floor";

        // Пол с запасом закрывает любое окно Game (камера 24×14 ед., Free Aspect до ~4:1)
        private static readonly Vector2 WorldSize = new Vector2(64f, 36f);
        private const float TileScale = 1.6f;                       // тайл 512 px / PPU 200 = 2.56 ед. → 4.1 ед.
        private static readonly Color Tint = new Color(0.8f, 0.8f, 0.8f); // чуть темнее, чтобы стол «светился»

        [MenuItem("CardThrow/Фон: пол казино")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            if (!PrepareTileImport()) return;
            Sprite tile = AssetDatabase.LoadAssetAtPath<Sprite>(TilePath);

            GameObject floor = GameObject.Find(FloorName);
            if (floor == null)
            {
                floor = new GameObject(FloorName);
                Undo.RegisterCreatedObjectUndo(floor, "Create Floor");
            }
            floor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            floor.transform.localScale = new Vector3(TileScale, TileScale, 1f);
            floor.transform.SetAsFirstSibling();

            SpriteRenderer sr = floor.GetComponent<SpriteRenderer>();
            if (sr == null) sr = Undo.AddComponent<SpriteRenderer>(floor);
            sr.sprite = tile;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = WorldSize / TileScale;
            sr.color = Tint;
            sr.sortingLayerName = "Table";
            sr.sortingOrder = -100; // под сукном (order 0), зонами (5) и всем остальным

            EditorSceneManager.MarkSceneDirty(floor.scene);
            EditorSceneManager.SaveScene(floor.scene);
            Selection.activeGameObject = floor;
            Debug.Log("[CT] Пол казино: объект Floor (Tiled, Sorting Layer Table, order −100), сцена сохранена");
        }

        // Tiled-режиму нужен спрайт с Mesh Type = Full Rect, иначе предупреждение в Console
        private static bool PrepareTileImport()
        {
            TextureImporter ti = AssetImporter.GetAtPath(TilePath) as TextureImporter;
            if (ti == null)
            {
                EditorUtility.DisplayDialog("CardThrow", "Не найден " + TilePath, "OK");
                return false;
            }

            TextureImporterSettings s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            bool changed = s.spriteMeshType != SpriteMeshType.FullRect || ti.wrapMode != TextureWrapMode.Repeat;
            if (!changed) return true;

            s.spriteMeshType = SpriteMeshType.FullRect;
            ti.SetTextureSettings(s);
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.SaveAndReimport();
            return true;
        }
    }
}
