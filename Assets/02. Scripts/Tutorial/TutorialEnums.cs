using System;

namespace Tutorial
{
    /// <summary>
    /// 튜토리얼 스텝 UI 타입
    /// </summary>
    public enum TutorialStepType
    {
        /// <summary>기본 구조: 하단 고정, 캐릭터 일러스트 + 이름 + 대사</summary>
        FullDialog,

        /// <summary>변형 구조: 중앙/상단, 썸네일 + 대사 (이름 없음)</summary>
        CompactDialog,

        /// <summary>시스템 설명: UI 강조 + 텍스트 박스</summary>
        Highlight
    }

    /// <summary>
    /// 튜토리얼 진행 조건
    /// </summary>
    public enum TutorialAdvanceType
    {
        /// <summary>터치/클릭으로 진행</summary>
        OnTouch,

        /// <summary>특정 UI 클릭 대기</summary>
        WaitForTargetClick,

        /// <summary>이벤트 대기 (EventChannel)</summary>
        WaitForEvent,

        /// <summary>자동 진행 (딜레이 후)</summary>
        Auto
    }

    /// <summary>
    /// 캐릭터 표시 상태
    /// </summary>
    public enum CharacterDisplayState
    {
        /// <summary>활성 (채도 100%)</summary>
        Active,

        /// <summary>비활성 (채도 33%)</summary>
        Inactive,

        /// <summary>숨김</summary>
        Hidden
    }
}
