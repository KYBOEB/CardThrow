using TMPro;
using UnityEngine;

namespace CardThrow
{
    // Игрок за столом: счёт над головой, поворот головы («смотрит на стол / отвернулся»), подсветка напарника.
    public class Seat : MonoBehaviour
    {
        [SerializeField] private string displayName = "Игрок";
        [SerializeField] private bool isPartner;
        [SerializeField] private int score = 12;
        [SerializeField] private Zone zone;

        [Header("Визуал")]
        [SerializeField] private TMP_Text scoreLabel;          // TextMeshPro (world space) над головой
        [SerializeField] private Transform head;               // спрайт головы, вращается по Z
        [SerializeField] private GameObject partnerHighlight;  // золотой контур
        [SerializeField] private float lookAwayAngle = 150f;
        [SerializeField] private float headTurnSpeed = 360f;   // градусов в секунду

        private float baseHeadAngle;
        private float targetHeadAngle;
        private float currentTurnSpeed;

        public string DisplayName => displayName;
        public bool IsPartner => isPartner;
        public int Score => score;
        public Zone Zone => zone;

        private void Awake()
        {
            if (head != null) baseHeadAngle = head.localEulerAngles.z;
            targetHeadAngle = baseHeadAngle;
            currentTurnSpeed = headTurnSpeed;
            if (partnerHighlight != null) partnerHighlight.SetActive(isPartner);
            RefreshLabel();
        }

        public void SetScore(int value)
        {
            score = value;
            RefreshLabel();
        }

        public void SetLookingAway(bool away)
        {
            targetHeadAngle = baseHeadAngle + (away ? lookAwayAngle : 0f);
            currentTurnSpeed = headTurnSpeed;
        }

        // Плавный поворот за заданное время (например, за 3 с до конца «окна внимания»)
        public void SetLookingAway(bool away, float turnDuration)
        {
            targetHeadAngle = baseHeadAngle + (away ? lookAwayAngle : 0f);
            currentTurnSpeed = turnDuration > 0.01f ? Mathf.Abs(lookAwayAngle) / turnDuration : headTurnSpeed;
        }

        private void Update()
        {
            if (head == null) return;
            float z = Mathf.MoveTowardsAngle(head.localEulerAngles.z, targetHeadAngle, currentTurnSpeed * Time.deltaTime);
            head.localEulerAngles = new Vector3(0f, 0f, z);
        }

        private void RefreshLabel()
        {
            if (scoreLabel != null) scoreLabel.text = score.ToString();
        }
    }
}
