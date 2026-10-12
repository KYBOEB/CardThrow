using UnityEditor;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Фидбек: настроить FeedbackManager».
    // Заполняет поля FeedbackManager в префабе Prefabs/Gameplay/FeedbackManager.prefab (спрайты VFX, шрифт, UiTexts).
    // Экземпляр префаба уже стоит в Main.unity — он обновится сам. Повторный запуск безопасен.
    public static class SetupFeedback
    {
        private const string Root = "Assets/_Project";
        private const string PrefabPath = Root + "/Prefabs/Gameplay/FeedbackManager.prefab";
        private const string VfxDir = Root + "/Art/Sprites/VFX/";
        private const string FontPath = Root + "/Art/Fonts/Montserrat-Bold SDF.asset";
        private const string TextsPath = Root + "/Data/UiTexts.asset";

        [MenuItem("CardThrow/Фидбек: настроить FeedbackManager")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                FeedbackManager fm = root.GetComponent<FeedbackManager>();
                if (fm == null)
                {
                    Debug.LogError($"[CT] В {PrefabPath} нет компонента FeedbackManager");
                    return;
                }

                SerializedObject so = new SerializedObject(fm);
                Set(so, "texts", AssetDatabase.LoadAssetAtPath<UiTexts>(TextsPath));
                Set(so, "popupFont", AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontPath));
                Set(so, "shardSprite", Fx("fx_shard"));
                Set(so, "foamSprite", Fx("fx_foam"));
                Set(so, "sparkleSprite", Fx("fx_sparkle"));
                Set(so, "coinSprite", Fx("fx_coin"));
                Set(so, "dustSprite", Fx("fx_dust"));
                Set(so, "starSprite", Fx("fx_star"));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[CT] FeedbackManager настроен: 6 спрайтов VFX, шрифт, UiTexts. Экземпляр в Main обновится сам.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            if (Object.FindAnyObjectByType<FeedbackManager>(FindObjectsInactive.Include) == null)
                Debug.LogWarning("[CT] В открытой сцене нет FeedbackManager — перетащи Prefabs/Gameplay/FeedbackManager в Main.");
        }

        private static Sprite Fx(string name)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(VfxDir + name + ".png");
            if (s == null) Debug.LogWarning($"[CT] Не найден спрайт {VfxDir}{name}.png");
            return s;
        }

        private static void Set(SerializedObject so, string field, Object value)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[CT] У FeedbackManager нет поля «{field}»"); return; }
            p.objectReferenceValue = value;
        }
    }
}
