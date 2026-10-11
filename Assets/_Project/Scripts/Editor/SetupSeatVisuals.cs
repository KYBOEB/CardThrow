using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Seat: добавить визуал зон (D3)».
    // Дописывает в Prefabs/Seats/Seat.prefab дочерний объект Zone/ZoneVisual (SpriteRenderer)
    // и заполняет новые поля Seat. НЕ пересобирает префаб: правки Егора в раздачах (позиции, размеры зон)
    // сохраняются, а новый объект сам появится во всех Hand_0N, потому что они — экземпляры Seat.prefab.
    // Повторный запуск безопасен.
    public static class SetupSeatVisuals
    {
        private const string Root = "Assets/_Project";
        private const string SeatPath = Root + "/Prefabs/Seats/Seat.prefab";
        private const string PartnerZoneSprite = Root + "/Art/Sprites/Table/zone_partner.png";
        private const string OpponentZoneSprite = Root + "/Art/Sprites/Table/zone_opponent.png";
        private const string VisualName = "ZoneVisual";

        [MenuItem("CardThrow/Seat: добавить визуал зон (D3)")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(SeatPath);
            try
            {
                Seat seat = root.GetComponent<Seat>();
                Zone zone = root.GetComponentInChildren<Zone>(true);
                if (seat == null || zone == null)
                {
                    Debug.LogError($"[CT] В {SeatPath} нет Seat или Zone");
                    return;
                }

                Transform visual = zone.transform.Find(VisualName);
                if (visual == null)
                {
                    visual = new GameObject(VisualName).transform;
                    visual.SetParent(zone.transform, false);
                }
                visual.gameObject.layer = 0; // Default: это только картинка, в OverlapPoint по слою Zone не участвует

                SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                if (sr == null) sr = visual.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OpponentZoneSprite); // в Play Seat сам выберет по роли
                sr.sortingLayerName = "Table";
                sr.sortingOrder = 5; // над сукном, под игроками и предметами (слой Objects)

                SerializedObject so = new SerializedObject(seat);
                Set(so, "zoneVisual", sr);
                Set(so, "partnerZoneSprite", AssetDatabase.LoadAssetAtPath<Sprite>(PartnerZoneSprite));
                Set(so, "opponentZoneSprite", AssetDatabase.LoadAssetAtPath<Sprite>(OpponentZoneSprite));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, SeatPath);
                Debug.Log("[CT] Seat.prefab: добавлен Zone/ZoneVisual, поля D3 заполнены. Раздачи Hand_0N обновятся сами.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void Set(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[CT] У Seat нет поля «{field}» — Seat.cs не обновлён?"); return; }
            if (value == null) Debug.LogWarning($"[CT] Seat.{field}: ассет не найден");
            p.objectReferenceValue = value;
        }
    }
}
