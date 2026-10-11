using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CardThrow.EditorTools
{
    // Одноразовый сборщик проекта: меню  CardThrow → «Собрать сцену Main (один раз)».
    // Делает: папки, слои, Sorting Layers, импорт спрайтов (PPU 200), ассеты настроек,
    // сцену Main.unity с иерархией из CONTEXT §4, префабы Алексея и все связи между объектами.
    public static class BuildMainScene
    {
        private const string Root = "Assets/_Project";
        private const string ScenePath = Root + "/Scenes/Main.unity";
        private const int LayerCard = 6, LayerObstacle = 7, LayerZone = 8, LayerTable = 9;

        [MenuItem("CardThrow/Собрать сцену Main (один раз)")]
        public static void Build()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("CardThrow",
                    "Main.unity уже существует. Пересборка перезапишет сцену и префабы Dealer/Card/TrajectoryPreview/FeedbackManager " +
                    "(правки Алексея в них пропадут). Продолжить?", "Пересобрать", "Отмена"))
                return;

            try
            {
                EnsureFolders();
                if (!EnsureLayersAndSortingLayers()) return;
                ImportSprites();
                FlightSettings flight = CreateAssetIfMissing<FlightSettings>(Root + "/Data/FlightSettings.asset");
                ConfigureFlight(flight);
                GameSettings game = CreateAssetIfMissing<GameSettings>(Root + "/Data/GameSettings.asset");
                SoundLibrary sounds = CreateAssetIfMissing<SoundLibrary>(Root + "/Data/SoundLibrary.asset");
                AssetDatabase.SaveAssets();

                BuildScene(flight, game, sounds);
                Debug.Log("[CT] Готово: сцена " + ScenePath + " собрана, префабы лежат в Prefabs/Gameplay.");
            }
            catch (Exception e)
            {
                Debug.LogError("[CT] Сборка прервана: " + e);
            }
        }

        // ---------- папки ----------
        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Art", "Art/Sprites", "Art/Fonts", "Audio", "Audio/SFX", "Audio/Music", "Data",
                "Prefabs", "Prefabs/Gameplay", "Prefabs/Obstacles", "Prefabs/Seats", "Prefabs/Levels", "Prefabs/UI", "Prefabs/VFX",
                "Scenes", "Scenes/Sandbox"
            };
            if (!AssetDatabase.IsValidFolder("Assets/_Project")) AssetDatabase.CreateFolder("Assets", "_Project");
            foreach (string f in folders)
            {
                string full = Root + "/" + f;
                if (AssetDatabase.IsValidFolder(full)) continue;
                int slash = f.LastIndexOf('/');
                string parent = slash < 0 ? Root : Root + "/" + f.Substring(0, slash);
                string name = slash < 0 ? f : f.Substring(slash + 1);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        // ---------- слои ----------
        private static bool EnsureLayersAndSortingLayers()
        {
            UnityEngine.Object tagManager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0];
            SerializedObject so = new SerializedObject(tagManager);

            SerializedProperty layers = so.FindProperty("layers");
            (int index, string name)[] wanted = { (LayerCard, "Card"), (LayerObstacle, "Obstacle"), (LayerZone, "Zone"), (LayerTable, "Table") };
            foreach (var (index, name) in wanted)
            {
                SerializedProperty el = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrEmpty(el.stringValue)) el.stringValue = name;
                else if (el.stringValue != name)
                {
                    Debug.LogError($"[CT] Слой {index} занят именем «{el.stringValue}», а нужен «{name}». Освободите его в Tags and Layers и запустите снова.");
                    return false;
                }
            }

            SerializedProperty sorting = so.FindProperty("m_SortingLayers");
            foreach (string name in new[] { "Table", "Objects", "Card", "FX" })
            {
                bool exists = false;
                for (int i = 0; i < sorting.arraySize; i++)
                    if (sorting.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) { exists = true; break; }
                if (exists) continue;
                sorting.InsertArrayElementAtIndex(sorting.arraySize);
                SerializedProperty el = sorting.GetArrayElementAtIndex(sorting.arraySize - 1);
                el.FindPropertyRelative("name").stringValue = name;
                el.FindPropertyRelative("uniqueID").intValue = (int)((uint)Guid.NewGuid().GetHashCode() & 0x7fffffff);
                el.FindPropertyRelative("locked").boolValue = false;
            }
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            return true;
        }

        // ---------- спрайты ----------
        private static void ImportSprites()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { Root + "/Art/Sprites" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool changed = false;
                if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; changed = true; }
                if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; changed = true; }
                float ppu = path.Contains("/Sprites/UI/") ? 100f : 200f;   // UI рисуем в 1×, остальное в 2×
                if (!Mathf.Approximately(ti.spritePixelsPerUnit, ppu)) { ti.spritePixelsPerUnit = ppu; changed = true; }
                if (path.EndsWith("/ui_panel.png") || path.EndsWith("/ui_button.png"))
                {
                    Vector4 border = new Vector4(32f, 32f, 32f, 32f);          // 9-slice
                    if (ti.spriteBorder != border) { ti.spriteBorder = border; changed = true; }
                }
                if (ti.mipmapEnabled) { ti.mipmapEnabled = false; changed = true; }
                if (ti.filterMode != FilterMode.Bilinear) { ti.filterMode = FilterMode.Bilinear; changed = true; }
                if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; changed = true; }
                if (!ti.alphaIsTransparency) { ti.alphaIsTransparency = true; changed = true; }
                if (changed) ti.SaveAndReimport();
            }
        }

        private static T CreateAssetIfMissing<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // Числа под геометрию «бросок вдоль всего стола» + маски слоёв (шаг 11 документа)
        private static void ConfigureFlight(FlightSettings f)
        {
            if (Mathf.Approximately(f.maxSpeed, 16f)) f.maxSpeed = 21f;
            if (Mathf.Approximately(f.maxDragDistance, 2.5f)) f.maxDragDistance = 2.0f;
            if (f.obstacleMask.value == 0) f.obstacleMask = 1 << LayerObstacle;
            if (f.zoneMask.value == 0) f.zoneMask = 1 << LayerZone;
            if (f.tableMask.value == 0) f.tableMask = 1 << LayerTable;
            EditorUtility.SetDirty(f);
        }

        private static Sprite LoadSprite(string relative)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Sprites/" + relative);
            if (s == null) Debug.LogWarning("[CT] Не найден спрайт " + relative);
            return s;
        }

        // ---------- сцена ----------
        private static void BuildScene(FlightSettings flight, GameSettings game, SoundLibrary sounds)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // оставляем только камеру
            Camera cam = null;
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                Camera c = go.GetComponent<Camera>();
                if (c != null && cam == null) cam = c; else UnityEngine.Object.DestroyImmediate(go);
            }
            if (cam == null) cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();

            // Main Camera
            cam.gameObject.name = "Main Camera";
            cam.gameObject.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.06f, 1f);
            if (cam.GetComponent<CameraFitter>() == null) cam.gameObject.AddComponent<CameraFitter>();
            AddGlobalLight2D();

            // Table
            GameObject table = new GameObject("Table");
            table.layer = LayerTable;
            SpriteRenderer tsr = table.AddComponent<SpriteRenderer>();
            tsr.sprite = LoadSprite("Table/table_felt.png");
            tsr.sortingLayerName = "Table";
            BoxCollider2D tcol = table.AddComponent<BoxCollider2D>();
            tcol.isTrigger = true;
            tcol.size = new Vector2(19f, 9.5f);

            // Dealer (с LaunchPoint)
            GameObject dealer = new GameObject("Dealer");
            dealer.transform.position = new Vector3(-9f, 0f, 0f);   // левый торец стола, бросок вдоль +X
            DealerHand hand = dealer.AddComponent<DealerHand>();
            AimController aim = dealer.AddComponent<AimController>();
            GameObject launch = new GameObject("LaunchPoint");
            launch.transform.SetParent(dealer.transform, false);
            launch.transform.position = new Vector3(-7.6f, 0f, 0f);
            SetRef(aim, "dealerHand", hand);
            SetRef(aim, "launchPoint", launch.transform);

            // Card
            GameObject card = new GameObject("Card");
            card.layer = LayerCard;
            card.transform.position = new Vector3(-7.6f, 0f, 0f);
            SpriteRenderer csr = card.AddComponent<SpriteRenderer>();
            csr.sprite = LoadSprite("Cards/card_back.png");
            csr.sortingLayerName = "Card";
            CardFlight flightComp = card.AddComponent<CardFlight>();
            SetRef(flightComp, "settings", flight);

            // TrajectoryPreview, FeedbackManager
            GameObject traj = new GameObject("TrajectoryPreview");
            traj.AddComponent<TrajectoryPreview>();
            GameObject feedback = new GameObject("FeedbackManager");
            feedback.AddComponent<FeedbackManager>();

            // превращаем в префабы (экземпляры остаются в сцене и связаны с префабами)
            string pf = Root + "/Prefabs/Gameplay/";
            GameObject dealerI = PrefabUtility.SaveAsPrefabAssetAndConnect(dealer, pf + "Dealer.prefab", InteractionMode.AutomatedAction);
            GameObject cardI = PrefabUtility.SaveAsPrefabAssetAndConnect(card, pf + "Card.prefab", InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(traj, pf + "TrajectoryPreview.prefab", InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAssetAndConnect(feedback, pf + "FeedbackManager.prefab", InteractionMode.AutomatedAction);

            // LevelRoot
            GameObject levelRoot = new GameObject("LevelRoot");

            // [Systems]
            GameObject systems = new GameObject("[Systems]");
            GameManager gm = AddChild<GameManager>(systems, "GameManager");
            LevelManager lm = AddChild<LevelManager>(systems, "LevelManager");
            AddChild<TutorialController>(systems, "TutorialController");
            AudioManager am = AddChild<AudioManager>(systems, "AudioManager");

            // связи между объектами сцены (только Данила; для префабов Алексея — как переопределения экземпляров)
            AimController aimI = dealerI.GetComponent<AimController>();
            DealerHand handI = dealerI.GetComponent<DealerHand>();
            CardFlight flightI = cardI.GetComponent<CardFlight>();
            SetRef(aimI, "cardFlight", flightI);
            SetRef(gm, "settings", game);
            SetRef(gm, "levelManager", lm);
            SetRef(gm, "dealerHand", handI);
            SetRef(gm, "aimController", aimI);
            SetRef(gm, "cardFlight", flightI);
            SetRef(lm, "levelRoot", levelRoot.transform);
            SetRef(am, "library", sounds);

            // EventSystem для будущего UI
            GameObject es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem));
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static T AddChild<T>(GameObject parent, string name) where T : Component
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<T>();
        }

        private static void SetRef(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"[CT] У {target.GetType().Name} нет сериализуемого поля «{field}»"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Без 2D-света спрайты в URP 2D могут рисоваться чёрными. Создаём Global Light 2D через рефлексию,
        // чтобы скрипт компилировался даже если пакета URP нет.
        private static void AddGlobalLight2D()
        {
            // Полное имя типа + имя сборки URP 2D: null, если пакета нет (без перебора всех сборок домена)
            Type t = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime");
            if (t == null) return;
            GameObject go = new GameObject("Global Light 2D");
            Component light = go.AddComponent(t);
            var prop = t.GetProperty("lightType");
            if (prop != null) prop.SetValue(light, Enum.Parse(prop.PropertyType, "Global"));
        }
    }
}
