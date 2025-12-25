using System;
using System.Collections.Generic;
using NovelianMagicLibraryDefense.Managers;
using UnityEngine;
using TMPro;
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

        [Header("프리셋 정보 텍스트")]
        [SerializeField] private TextMeshProUGUI presetInfoText;

        /// <summary>
        /// 프리셋 변경 시 호출되는 이벤트 (새 프리셋 인덱스 전달)
        /// </summary>
        public event Action<int> OnPresetSelected;

        // 프리셋 변경 잠금 상태 (파견 중일 때 true)
        private bool isLocked = false;

        // 현재 이 패널에서 사용 중인 프리셋 인덱스 (-1은 미사용)
        private int lockedPresetIndex = -1;

        // 로컬 선택 인덱스 (파견 시작 전까지 임시 저장, DeckManager와 별개)
        private int localSelectedPresetIndex = 0;

        // 더티 플래그 (로컬 선택이 변경되었는지)
        private bool isDirty = false;

        private void OnEnable()
        {
            // DeckManager 이벤트 구독
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.OnPresetChanged += OnPresetChangedFromManager;
            }

            // 파견 중인지 확인 (isActive 기준 - 보상 받기 전까지 유지)
            bool isThisPanelDispatching = dispatchType == DispatchType.Combat
                ? DispatchStateHelper.IsCombatDispatching()
                : DispatchStateHelper.IsGatheringDispatching();

            // 파견 중이면 (보상 받기 전까지) 파견 중인 프리셋으로 로컬 선택 유지
            if (isThisPanelDispatching)
            {
                int thisPanelDispatchPreset = GetThisPanelDispatchPresetIndex();
                if (thisPanelDispatchPreset >= 0)
                {
                    localSelectedPresetIndex = thisPanelDispatchPreset;
                    Debug.Log($"[DispatchPresetSelector] 파견 중 - 프리셋 {thisPanelDispatchPreset + 1}번으로 로컬 선택 유지");
                }
                else
                {
                    Debug.LogWarning($"[DispatchPresetSelector] 파견 중이지만 프리셋 인덱스를 찾을 수 없음");
                }
            }
            else
            {
                // 파견 중이 아닐 때만 로컬 선택 초기화
                // 다른 파견에서 사용 중인 프리셋 확인
                int otherDispatchPreset = GetOtherDispatchPresetIndex();

                // 사용 가능한 첫 번째 프리셋으로 로컬 선택 초기화
                if (otherDispatchPreset >= 0)
                {
                    localSelectedPresetIndex = FindFirstAvailablePreset(otherDispatchPreset);
                    Debug.Log($"[DispatchPresetSelector] 패널 진입 - 프리셋 {otherDispatchPreset + 1}은 다른 파견에서 사용 중, 로컬 선택을 {localSelectedPresetIndex + 1}로 초기화");
                }
                else
                {
                    // 사용 중인 프리셋 없으면 0번으로 초기화
                    localSelectedPresetIndex = 0;
                }
                isDirty = false;
            }

            // 프리셋 마크 초기화
            InitializePresetMarks();

            // 프리셋 마크 UI 갱신 (항상 호출)
            RefreshPresetMarks();
            Debug.Log($"[DispatchPresetSelector] 패널 진입 - 프리셋 UI 갱신 완료 (로컬 선택: {localSelectedPresetIndex + 1})");
        }

        /// <summary>
        /// 사용 가능한 첫 번째 프리셋 인덱스 반환
        /// </summary>
        private int FindFirstAvailablePreset(int excludePreset)
        {
            for (int i = 0; i < 4; i++)
            {
                if (i != excludePreset)
                    return i;
            }
            return 0;
        }

        private void OnDisable()
        {
            // DeckManager 이벤트 구독 해제
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.OnPresetChanged -= OnPresetChangedFromManager;
            }

            // 로컬 선택 초기화 (패널 나갈 때)
            localSelectedPresetIndex = 0;
            isDirty = false;
            Debug.Log("[DispatchPresetSelector] 패널 퇴장 - 로컬 선택 초기화");
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
            // 현재 패널의 파견 상태 확인 (isActive 기준 - 보상 받기 전까지)
            bool isThisPanelDispatching = dispatchType == DispatchType.Combat
                ? DispatchStateHelper.IsCombatDispatching()
                : DispatchStateHelper.IsGatheringDispatching();

            // 현재 패널이 파견 중이면 (보상 받기 전까지) 파견 중인 슬롯만 클릭 가능
            if (isThisPanelDispatching)
            {
                int thisPanelDispatchPreset = GetThisPanelDispatchPresetIndex();

                // 파견 중인 슬롯이 아니면 경고 표시
                if (presetIndex != thisPanelDispatchPreset)
                {
                    if (WarningUIManager.Instance != null)
                    {
                        WarningUIManager.Instance.ShowWarning("파견 진행 중입니다.");
                    }
                    Debug.Log($"[DispatchPresetSelector] 파견 중 - {thisPanelDispatchPreset + 1}번 슬롯만 선택 가능");
                    return;
                }

                // 파견 중인 슬롯 클릭은 이미 선택된 상태이므로 무시
                return;
            }

            // 이미 선택된 프리셋이면 무시
            if (presetIndex == localSelectedPresetIndex)
            {
                Debug.Log($"[DispatchPresetSelector] 이미 프리셋 {presetIndex + 1}이 선택되어 있습니다.");
                return;
            }

            // 로컬 선택만 변경 (DeckManager에 저장하지 않음)
            int previousPreset = localSelectedPresetIndex;
            localSelectedPresetIndex = presetIndex;
            isDirty = true;

            // UI 갱신
            RefreshPresetMarks();

            // 외부 리스너에게 알림 (덱 캐릭터 갱신 등)
            OnPresetSelected?.Invoke(presetIndex);

            Debug.Log($"[DispatchPresetSelector] 로컬 프리셋 선택 {previousPreset + 1} → {presetIndex + 1} (저장 안함)");
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
            int otherDispatchPreset = GetOtherDispatchPresetIndex();
            int thisPanelDispatchPreset = GetThisPanelDispatchPresetIndex();

            // 현재 패널이 파견 중인지 확인 (isActive 기준 - 보상 받기 전까지)
            bool isThisPanelDispatching = dispatchType == DispatchType.Combat
                ? DispatchStateHelper.IsCombatDispatching()
                : DispatchStateHelper.IsGatheringDispatching();

            for (int i = 0; i < presetMarks.Count; i++)
            {
                if (presetMarks[i] != null)
                {
                    // 다른 파견에서 사용 중인 프리셋은 비활성화 표시
                    bool isUsedByOther = otherDispatchPreset >= 0 && i == otherDispatchPreset;
                    presetMarks[i].SetDisabled(isUsedByOther);

                    // 선택 상태: 현재 패널이 파견 중이면 파견 중인 프리셋, 아니면 로컬 선택 프리셋
                    if (isThisPanelDispatching && thisPanelDispatchPreset >= 0)
                    {
                        presetMarks[i].SetSelected(i == thisPanelDispatchPreset);
                    }
                    else
                    {
                        // 파견 중이 아닐 때는 로컬 선택 인덱스 사용
                        presetMarks[i].SetSelected(i == localSelectedPresetIndex);
                    }
                }
            }

            // 프리셋 정보 텍스트 업데이트 (파견 중이면 표시)
            UpdatePresetInfoText(isThisPanelDispatching, thisPanelDispatchPreset);
        }

        /// <summary>
        /// 프리셋 정보 텍스트 업데이트
        /// </summary>
        private void UpdatePresetInfoText(bool isDispatching, int dispatchPreset)
        {
            if (presetInfoText == null) return;

            if (isDispatching && dispatchPreset >= 0)
            {
                // 파견 중일 때: 텍스트 표시
                presetInfoText.gameObject.SetActive(true);
                presetInfoText.text = $"{dispatchPreset + 1}번 프리셋 파견 중입니다.";
            }
            else
            {
                // 파견 중이 아닐 때: 텍스트 숨김
                presetInfoText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 현재 패널에서 파견 중인 프리셋 인덱스 반환 (-1이면 파견 중 아님)
        /// </summary>
        private int GetThisPanelDispatchPresetIndex()
        {
            var dispatchData = FirebaseSaveManager.Instance?.CachedData?.dispatch;
            if (dispatchData == null) return -1;

            DispatchStateData thisState = dispatchType == DispatchType.Combat
                ? dispatchData.combat
                : dispatchData.gathering;

            if (thisState != null && thisState.isActive)
            {
                return thisState.presetIndex;
            }

            return -1;
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
        /// 현재 선택된 프리셋 인덱스 반환 (DeckManager 기준)
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
        /// 로컬 선택된 프리셋 인덱스 반환 (저장되지 않은 임시 선택)
        /// </summary>
        public int GetLocalSelectedPresetIndex()
        {
            return localSelectedPresetIndex;
        }

        /// <summary>
        /// 더티 플래그 확인 (로컬 선택이 변경되었는지)
        /// </summary>
        public bool IsDirty => isDirty;

        /// <summary>
        /// 프리셋 선택 적용 (파견 시작 시 호출)
        /// 로컬 선택을 DeckManager에 저장하고 더티 플래그 초기화
        /// </summary>
        /// <returns>적용된 프리셋 인덱스</returns>
        public int ApplyPresetSelection()
        {
            if (DeckManager.Instance != null)
            {
                // 로컬 선택을 DeckManager에 적용
                DeckManager.Instance.SwitchPreset(localSelectedPresetIndex);
                Debug.Log($"[DispatchPresetSelector] 프리셋 선택 적용: {localSelectedPresetIndex + 1}");
            }

            // 더티 플래그 초기화
            isDirty = false;

            return localSelectedPresetIndex;
        }

        /// <summary>
        /// 현재 프리셋의 덱 데이터 반환 (로컬 선택 기준)
        /// 주의: ApplyPresetSelection 호출 후 사용해야 정확한 데이터 반환
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
        /// 로컬 선택 프리셋의 덱 데이터 반환
        /// DeckManager의 GetPresetDeck 메서드 사용
        /// </summary>
        public List<int> GetLocalSelectedDeck()
        {
            if (DeckManager.Instance != null)
            {
                return DeckManager.Instance.GetPresetDeck(localSelectedPresetIndex);
            }
            return new List<int> { -1, -1, -1, -1 };
        }

        /// <summary>
        /// 현재 프리셋이 유효한지 (3명 이상) 확인 (DeckManager 기준)
        /// 주의: ApplyPresetSelection 호출 후 사용해야 정확한 결과 반환
        /// </summary>
        public bool IsCurrentPresetValid()
        {
            if (DeckManager.Instance != null)
            {
                return DeckManager.Instance.IsDeckValid();
            }
            return false;
        }

        /// <summary>
        /// 로컬 선택 프리셋이 유효한지 (3명 이상) 확인
        /// </summary>
        public bool IsLocalSelectedPresetValid()
        {
            var deck = GetLocalSelectedDeck();
            int validCount = 0;
            foreach (int charId in deck)
            {
                if (charId >= 0) validCount++;
            }
            return validCount >= 3;
        }

        /// <summary>
        /// 현재 선택된 프리셋이 다른 파견에서 사용 중인지 확인 (로컬 선택 기준)
        /// </summary>
        public bool IsLocalSelectedPresetUsedByOtherDispatch()
        {
            int otherDispatchPreset = GetOtherDispatchPresetIndex();
            return otherDispatchPreset >= 0 && localSelectedPresetIndex == otherDispatchPreset;
        }
    }
}
