using System;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Data;

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

        [Header("파견 타입")]
        [SerializeField] private DispatchType dispatchType = DispatchType.Combat;

        /// <summary>
        /// 프리셋 변경 시 호출되는 이벤트 (새 프리셋 인덱스 전달)
        /// </summary>
        public event Action<int> OnPresetSelected;

        // 프리셋 변경 잠금 상태 (파견 중일 때 true)
        private bool isLocked = false;

        // 현재 이 패널에서 사용 중인 프리셋 인덱스 (-1은 미사용)
        private int lockedPresetIndex = -1;

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
            // 파견 중이면 프리셋 변경 불가
            if (isLocked)
            {
                Debug.Log("[DispatchPresetSelector] 파견 중에는 프리셋을 변경할 수 없습니다.");
                return;
            }

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

            // 프리셋 전환 (DeckManager에서 Firebase 저장 및 이벤트 발생)
            DeckManager.Instance.SwitchPreset(presetIndex);

            Debug.Log($"[DispatchPresetSelector] 프리셋 {currentPreset + 1} → {presetIndex + 1} 전환");
        }

        /// <summary>
        /// 프리셋 변경 잠금 (파견 시작 시 호출)
        /// </summary>
        public void Lock()
        {
            isLocked = true;
            lockedPresetIndex = GetCurrentPresetIndex();
            Debug.Log($"[DispatchPresetSelector] 프리셋 변경 잠금 (프리셋 {lockedPresetIndex + 1})");
        }

        /// <summary>
        /// 프리셋 변경 잠금 해제 (파견 완료 시 호출)
        /// </summary>
        public void Unlock()
        {
            isLocked = false;
            lockedPresetIndex = -1;
            Debug.Log("[DispatchPresetSelector] 프리셋 변경 잠금 해제");
        }

        /// <summary>
        /// 현재 잠금 상태 반환
        /// </summary>
        public bool IsLocked => isLocked;

        /// <summary>
        /// 현재 잠긴 프리셋 인덱스 반환 (-1이면 잠금 없음)
        /// </summary>
        public int LockedPresetIndex => lockedPresetIndex;

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
                    // 프리셋 마크는 항상 선택 가능 (비활성화 해제)
                    presetMarks[i].SetDisabled(false);

                    // 선택 상태 업데이트
                    presetMarks[i].SetSelected(i == currentPreset);
                }
            }
        }

        /// <summary>
        /// 현재 선택된 프리셋이 다른 파견에서 사용 중인지 확인
        /// </summary>
        public bool IsCurrentPresetUsedByOtherDispatch()
        {
            int currentPreset = GetCurrentPresetIndex();
            int otherDispatchPreset = GetOtherDispatchPresetIndex();
            return otherDispatchPreset >= 0 && currentPreset == otherDispatchPreset;
        }

        /// <summary>
        /// 다른 파견에서 사용 중인 프리셋 인덱스 반환 (-1이면 없음)
        /// </summary>
        private int GetOtherDispatchPresetIndex()
        {
            var dispatchData = FirebaseSaveManager.Instance?.CachedData?.dispatch;
            if (dispatchData == null) return -1;

            // 현재 패널이 전투형이면 채집형 파견의 프리셋 확인, 반대도 마찬가지
            DispatchStateData otherState = dispatchType == DispatchType.Combat
                ? dispatchData.gathering
                : dispatchData.combat;

            if (otherState != null && otherState.isActive)
            {
                return otherState.presetIndex;
            }

            return -1;
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
