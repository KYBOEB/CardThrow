using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CardThrow.EditorTools
{
    // Меню  CardThrow → «Починить ссылки сцены».
    // Проставляет в GameManager и AimController ссылки на объекты ИЗ СЦЕНЫ (Dealer, Card),
    // а не на файлы-префабы из окна Project. Ничего не создаёт и не удаляет. Сцену сохраняет сам.
    public static class FixSceneLinks
    {
        [MenuItem("CardThrow/Починить ссылки сцены")]
        public static void Fix()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("CardThrow", "Сначала выйди из Play (кнопка ▶ сверху).", "OK");
                return;
            }

            // FindAnyObjectByType ищет только среди объектов открытой сцены — префабы в папке он не видит
            GameManager gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            DealerHand hand = Object.FindAnyObjectByType<DealerHand>(FindObjectsInactive.Include);
            AimController aim = Object.FindAnyObjectByType<AimController>(FindObjectsInactive.Include);
            CardFlight card = Object.FindAnyObjectByType<CardFlight>(FindObjectsInactive.Include);

            string missing = "";
            if (gm == null) missing += "\n• GameManager (в [Systems])";
            if (hand == null) missing += "\n• Dealer с компонентом DealerHand";
            if (aim == null) missing += "\n• Dealer с компонентом AimController";
            if (card == null) missing += "\n• Card с компонентом CardFlight";
            if (missing.Length > 0)
            {
                EditorUtility.DisplayDialog("CardThrow", "В открытой сцене не найдено:" + missing + "\n\nОткрой Scenes/Main.unity.", "OK");
                return;
            }

            SerializedObject gmSo = new SerializedObject(gm);
            gmSo.FindProperty("dealerHand").objectReferenceValue = hand;
            gmSo.FindProperty("aimController").objectReferenceValue = aim;
            gmSo.FindProperty("cardFlight").objectReferenceValue = card;
            gmSo.ApplyModifiedProperties();

            SerializedObject aimSo = new SerializedObject(aim);
            aimSo.FindProperty("cardFlight").objectReferenceValue = card;
            aimSo.FindProperty("dealerHand").objectReferenceValue = hand;
            aimSo.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
            EditorSceneManager.SaveScene(gm.gameObject.scene);

            Debug.Log($"[CT] Ссылки починены и сцена сохранена: GameManager → {hand.name}, {aim.name}, {card.name}; AimController → {card.name}");
        }
    }
}
