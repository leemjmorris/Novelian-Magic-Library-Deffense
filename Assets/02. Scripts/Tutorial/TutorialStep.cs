using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 단일 스텝 데이터
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        [Header("기본 설정")]
        [Tooltip("스텝 타입")]
        public TutorialStepType StepType = TutorialStepType.FullDialog;

        [Tooltip("텍스트 ID (TutorialData CSV 참조)")]
        public int TextId;

        [Tooltip("음성 Addressable Key (없으면 비워두기)")]
        public string VoiceKey;

        [Header("진행 조건")]
        [Tooltip("진행 방식")]
        public TutorialAdvanceType AdvanceType = TutorialAdvanceType.OnTouch;

        [Tooltip("자동 진행 딜레이 (Auto 타입일 때)")]
        public float AutoAdvanceDelay = 2f;

        [Tooltip("완료 이벤트 키 (WaitForEvent 타입일 때)")]
        public string CompleteEventKey;

        [Header("하이라이트 설정 (Highlight/WaitForTargetClick)")]
        [Tooltip("하이라이트 대상 오브젝트 경로 (Canvas/MainUI/Button 형식)")]
        public string HighlightTargetPath;

        [Tooltip("하이라이트 대상 RectTransform (에디터에서 직접 참조)")]
        public RectTransform HighlightTarget;

        [Header("캐릭터 표시 설정 (FullDialog)")]
        [Tooltip("캐릭터 표시 정보")]
        public List<CharacterDisplayInfo> Characters = new List<CharacterDisplayInfo>();

        [Tooltip("화자 캐릭터 ID (Characters 리스트의 인덱스)")]
        public int SpeakerIndex = 0;

        [Header("게임 제어")]
        [Tooltip("이 스텝에서 게임 일시정지")]
        public bool PauseGame = false;

        [Tooltip("이 스텝 완료 후 게임 재개")]
        public bool ResumeGameOnComplete = false;

        [Header("특수 연출")]
        [Tooltip("배경 불투명 처리")]
        public bool DimBackground = true;

        [Tooltip("스텝 시작 딜레이")]
        public float StartDelay = 0f;
    }

    /// <summary>
    /// 캐릭터 표시 정보
    /// </summary>
    [Serializable]
    public class CharacterDisplayInfo
    {
        [Tooltip("캐릭터 ID (CharacterData 참조)")]
        public int CharacterId;

        [Tooltip("캐릭터 이름 (직접 입력, 비어있으면 CharacterData에서 가져옴)")]
        public string CharacterName;

        [Tooltip("캐릭터 일러스트 Addressable Key")]
        public string IllustrationKey;

        [Tooltip("표시 상태")]
        public CharacterDisplayState DisplayState = CharacterDisplayState.Active;

        [Tooltip("표시 위치 (0: 왼쪽, 1: 중앙, 2: 오른쪽)")]
        public int Position = 0;
    }
}
