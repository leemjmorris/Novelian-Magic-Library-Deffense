using System;
using System.Collections.Generic;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;

namespace Dispatch
{
    /// <summary>
    /// 파견씬에서 프리셋 선택을 담당하는 컴포넌트
    /// DeckManager의 프리셋 시스템과 연동하여 4개 프리셋 중 하나를 선택
    /// </summary>
    public class DispatchPresetSelector : MonoBehaviour
    {
        [Header("프리셋 마크 (4개)")]
        [SerializeField] private List<PresetMark> presetMarks = new List<PresetMark>();

        /// <summary>
        /// 프리셋 변경 시 호출되는 이벤트 (새 프리셋 인덱스 전달)
        /// </summary>
        public event Action<int> OnPresetSelected;

        private void OnEnable()
        {
            // DeckManager 이벤트 구독
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.OnPresetChanged += OnPresetChangedFromManager;
            }

            // 프리셋 마크 초기화
            InitializePresetMarks();
        }

        private void OnDisable()
        {
            // DeckManager 이벤트 구독 해제
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.OnPresetChanged -= OnPresetChangedFromManager;
            }
        }

        /// <summary>
        /// 프리셋 마크 초기화
        /// </summary>
        private void InitializePresetMarks()
        {
            for (int i = 0; i < presetMarks.Count; i++)
            {
                if (presetMarks[i] != null)
                {
                    presetMarks[i].Initialize(i, OnPresetMarkClicked);
                }
            }

            // 현재 프리셋에 맞게 UI 업데이트
            RefreshPresetMarks();
            Debug.Log($"[DispatchPresetSelector] 프리셋 마크 초기화 완료. 현재 프리셋: {GetCurrentPresetIndex() + 1}");
        }

        /// <summary>
        /// 프리셋 마크 클릭 시 호출
        /// </summary>
        private void OnPresetMarkClicked(int presetIndex)
        {
            if (DeckManager.Instance == null)
            {
                Debug.LogWarning("[DispatchPresetSelector] DeckManager.Instance가 null입니다.");
                return;
            }

            int currentPreset = DeckManager.Instance.GetCurrentPresetIndex();
            if (presetIndex == currentPreset)
            {
                Debug.Log($"[DispatchPresetSelector] 이미 프리셋 {presetIndex + 1}이 선택되어 있습니다.");
                return;
            }

            // 파견 중일 때 프리셋 변경 차단
            if (DispatchStateHelper.IsDispatching())
            {
                Debug.LogWarning("[DispatchPresetSelector] 파견 중에는 프리셋을 변경할 수 없습니다.");

                // WarningUIManager를 통해 토스트 경고 표시
                if (WarningUIManager.Instance != null)
                {
                    WarningUIManager.Instance.ShowWarning("파견 중에는 프리셋을 변경할 수 없습니다.");
                }
                return;
            }

            // 프리셋 전환 (DeckManager에서 Firebase 저장 및 이벤트 발생)
            DeckManager.Instance.SwitchPreset(presetIndex);

            Debug.Log($"[DispatchPresetSelector] 프리셋 {currentPreset + 1} → {presetIndex + 1} 전환");
        }

        /// <summary>
        /// DeckManager에서 프리셋 변경 이벤트 수신
        /// </summary>
        private void OnPresetChangedFromManager(int newPresetIndex)
        {
            // UI 업데이트
            RefreshPresetMarks();

            // 외부 리스너에게 알림 (덱 캐릭터 갱신 등)
            OnPresetSelected?.Invoke(newPresetIndex);

            Debug.Log($"[DispatchPresetSelector] 프리셋 변경 이벤트 수신: {newPresetIndex + 1}");
        }

        /// <summary>
        /// 프리셋 마크 UI 갱신
        /// </summary>
        public void RefreshPresetMarks()
        {
            int currentPreset = GetCurrentPresetIndex();

            for (int i = 0; i < presetMarks.Count; i++)
            {
                if (presetMarks[i] != null)
                {
                    presetMarks[i].SetSelected(i == currentPreset);
                }
            }
        }

        /// <summary>
        /// 현재 선택된 프리셋 인덱스 반환
        /// </summary>
        public int GetCurrentPresetIndex()
        {
            if (DeckManager.Instance != null)
            {
                return DeckManager.Instance.GetCurrentPresetIndex();
            }
            return 0;
        }

        /// <summary>
        /// 현재 프리셋의 덱 데이터 반환
        /// </summary>
        public List<int> GetCurrentDeck()
        {
            if (DeckManager.Instance != null)
            {
                return DeckManager.Instance.GetDeck();
            }
            return new List<int> { -1, -1, -1, -1 };
        }

        /// <summary>
        /// 현재 프리셋이 유효한지 (3명 이상) 확인
        /// </summary>
        public bool IsCurrentPresetValid()
        {
            if (DeckManager.Instance != null)
            {
                return DeckManager.Instance.IsDeckValid();
            }
            return false;
        }
    }
}
