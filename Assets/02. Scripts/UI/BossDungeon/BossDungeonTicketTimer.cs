using TMPro;
using UnityEngine;

namespace NovelianMagicLibraryDefense.UI
{
    /// <summary>
    /// Issue #646: BossDungeonSelectPanel의 던전 출입증 타이머 업데이트
    /// </summary>
    public class BossDungeonTicketTimer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI ticketTimeText;

        private int maxDungeonPass = 5;

        private void OnEnable()
        {
            // CurrencyTable에서 최대 던전 출입증 조회
            if (CSVLoader.Instance != null && CSVLoader.Instance.IsInit)
            {
                var dungeonPassData = CSVLoader.Instance.GetData<CurrencyData>(CurrencyManager.DUNGEON_PASS_ID);
                if (dungeonPassData != null && dungeonPassData.Currency_Max_Count > 0)
                {
                    maxDungeonPass = dungeonPassData.Currency_Max_Count;
                }
            }
        }

        private void Update()
        {
            UpdateTicketTimeText();
        }

        private void UpdateTicketTimeText()
        {
            if (ticketTimeText == null) return;
            if (CurrencyManager.Instance == null) return;

            int currentTicket = CurrencyManager.Instance.GetCurrency(CurrencyManager.DUNGEON_PASS_ID);

            // 티켓이 최대치면 텍스트 비활성화
            if (currentTicket >= maxDungeonPass)
            {
                ticketTimeText.gameObject.SetActive(false);
                return;
            }

            // 티켓이 최대치 미만이면 남은 시간 표시
            ticketTimeText.gameObject.SetActive(true);
            float remainingSeconds = CurrencyManager.Instance.GetDungeonPassRecoveryRemainingTime();
            int minutes = Mathf.FloorToInt(remainingSeconds / 60f);
            int seconds = Mathf.FloorToInt(remainingSeconds % 60f);
            ticketTimeText.text = $"{minutes:D2}:{seconds:D2}";
        }
    }
}
