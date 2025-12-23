# Tutorial Manager 상세 가이드

## 목차
1. [시스템 개요](#시스템-개요)
2. [주요 컴포넌트](#주요-컴포넌트)
3. [에디터 툴 사용법](#에디터-툴-사용법)
4. [TutorialSequence 생성](#tutorialsequence-생성)
5. [TutorialStep 설정](#tutorialstep-설정)
6. [튜토리얼 시작 방법](#튜토리얼-시작-방법)
7. [이벤트 연동](#이벤트-연동)
8. [CSV 텍스트 설정](#csv-텍스트-설정)
9. [트러블슈팅](#트러블슈팅)

---

## 시스템 개요

튜토리얼 시스템은 게임 내 사용자 가이드를 제공하는 시스템입니다.

### 특징
- **Addressables 기반**: TutorialCanvas, TutorialEvents를 Addressables로 로드
- **ScriptableObject 기반**: 튜토리얼 시퀀스를 에셋으로 관리
- **자동 완료 체크**: 한 번 완료한 튜토리얼은 다시 실행되지 않음
- **에디터 툴 제공**: Inspector 없이 에디터 윈도우에서 편집 가능

### 아키텍처
```
TutorialManager (싱글톤)
    ├── TutorialEvents (이벤트 채널)
    ├── TutorialUIController (UI 관리)
    │   ├── FullDialogView (전체 대화창)
    │   ├── CompactDialogView (간략 대화창)
    │   └── HighlightView (하이라이트)
    └── TutorialSequence (시퀀스 데이터)
        └── TutorialStep[] (스텝 데이터)
```

---

## 주요 컴포넌트

### TutorialManager
- **위치**: `Assets/02. Scripts/Tutorial/TutorialManager.cs`
- **역할**: 튜토리얼 진행 관리, 싱글톤
- **초기화**: BootScene에서 자동 초기화

### TutorialSequence
- **위치**: `Assets/02. Scripts/Tutorial/TutorialSequence.cs`
- **역할**: 튜토리얼 시퀀스 데이터 (ScriptableObject)
- **저장 위치**: `Assets/ScriptableObjects/Tutorials/`

### TutorialStep
- **위치**: `Assets/02. Scripts/Tutorial/TutorialStep.cs`
- **역할**: 개별 스텝 데이터

### TutorialEvents
- **위치**: `Assets/02. Scripts/Tutorial/TutorialEvents.cs`
- **역할**: 이벤트 채널 (ScriptableObject)
- **Addressables 키**: `TutorialEvents`

### TutorialCanvas
- **역할**: 튜토리얼 UI 프리팹
- **Addressables 키**: `TutorialCanvas`

---

## 에디터 툴 사용법

### 에디터 열기
**메뉴**: `Tools > Tutorial Editor`

### 탭 구성

#### 1. Sequences 탭
- 모든 튜토리얼 시퀀스 목록
- 시퀀스 선택 시 상세 정보 표시
- Step 추가/편집/삭제/순서변경

#### 2. Quick Create 탭
- 새 튜토리얼 시퀀스 빠른 생성

#### 3. Settings 탭
- TutorialEvents 생성/관리
- 진행 상황 리셋
- 통계 확인

### Step 편집

Step 항목 클릭 시 상세 편집 패널이 열립니다:

| 항목 | 설명 |
|------|------|
| Step Type | 표시 방식 (FullDialog, CompactDialog, Highlight) |
| Text ID | CSV에서 가져올 텍스트 ID |
| Voice Key | 음성 Addressable 키 |
| Advance Type | 진행 조건 |
| Pause Game | 게임 일시정지 여부 |
| Resume On Complete | 완료 시 게임 재개 |
| Dim Background | 배경 어둡게 처리 |

### Target 선택 (드래그앤드롭)

WaitForTargetClick 또는 Highlight 타입에서:
1. **경로 직접 입력**: 텍스트 필드에 Hierarchy 경로 입력
2. **드래그앤드롭**: Hierarchy에서 오브젝트를 ObjectField로 드래그
   - 자동으로 경로 계산 및 입력

---

## TutorialSequence 생성

### 방법 1: 에디터 툴 사용 (권장)
1. `Tools > Tutorial Editor` 열기
2. Quick Create 탭 이동
3. Tutorial ID, Name, Description 입력
4. Create 버튼 클릭

### 방법 2: 직접 생성
1. Project 창에서 우클릭
2. `Create > Tutorial > Tutorial Sequence`

### 필수 설정

| 필드 | 설명 | 예시 |
|------|------|------|
| TutorialId | 고유 식별자 | `BATTLE_BASIC` |
| TutorialName | 표시 이름 | `기본 전투 튜토리얼` |
| CompletionSaveKey | 저장 키 | `Tutorial_BATTLE_BASIC_Completed` |
| CanSkip | 스킵 가능 여부 | true/false |
| NextTutorialId | 다음 튜토리얼 ID | (선택) |

---

## TutorialStep 설정

### StepType (표시 방식)

#### FullDialog
전체 화면 대화창. 캐릭터 일러스트와 함께 표시.

```
사용 상황: 스토리 설명, 캐릭터 대사
필수 설정: Characters 리스트, SpeakerIndex
```

**캐릭터 설정 필드:**
| 필드 | 설명 |
|------|------|
| 캐릭터 ID | CharacterData 참조 ID (입력 시 자동 로드) |
| 자동 버튼 | ID 기반으로 이름, 일러스트 키 자동 채우기 |
| 이름 | 자동 로드 또는 직접 입력 |
| 일러스트 Key | Path_ID 기반 자동 로드 또는 직접 입력 |
| 표시 상태 | Active(밝게), Inactive(어둡게), Hidden(숨김) |
| 위치 | 왼쪽(0), 중앙(1), 오른쪽(2) |
| 화자 (Speaker) | 현재 말하는 캐릭터 선택 |

> **팁**: 캐릭터 ID만 입력하면 CharacterTable.csv와 StringTable.csv에서 이름과 일러스트 키를 자동으로 가져옵니다.

#### CompactDialog
간략한 대화창. 화면 일부에 표시.

```
사용 상황: 간단한 안내, 힌트
```

#### Highlight
특정 UI 요소 하이라이트.

```
사용 상황: 버튼 안내, UI 설명
필수 설정: HighlightTargetPath 또는 HighlightTarget
```

---

### AdvanceType (진행 조건)

#### OnTouch
화면 아무 곳이나 터치하면 다음 스텝으로 진행.

```
설정: 없음
사용: 단순 설명, 스토리
```

#### Auto
지정된 시간 후 자동으로 다음 스텝 진행.

```
설정: AutoAdvanceDelay (초)
사용: 짧은 안내, 자동 진행 연출
```

#### WaitForTargetClick
특정 버튼/UI를 클릭해야 진행.

```
설정: HighlightTargetPath (Hierarchy 경로)
예시: "Canvas/MainUI/BattleButton"

필수 작업: 해당 버튼에 클릭 이벤트 연동 (아래 참조)
```

#### WaitForEvent
특정 게임 이벤트가 발생하면 진행.

```
설정: CompleteEventKey (이벤트 키)
예시: "BATTLE_START", "ENEMY_KILLED", "SKILL_USED"

필수 작업: 해당 이벤트 발생 코드에서 호출 (아래 참조)
```

---

## 튜토리얼 시작 방법

### 기본 사용

```csharp
using Tutorial;

public class MyScript : MonoBehaviour
{
    [SerializeField] private TutorialSequence battleTutorial;

    private void StartBattleTutorial()
    {
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsInitialized)
        {
            TutorialManager.Instance.StartTutorial(battleTutorial);
        }
    }
}
```

### 강제 실행 (디버그용)

```csharp
// 이미 완료된 튜토리얼도 강제로 다시 실행
TutorialManager.Instance.StartTutorial(battleTutorial, forceStart: true);
```

### 완료 여부 확인

```csharp
bool isCompleted = TutorialManager.Instance.IsTutorialCompleted("BATTLE_BASIC");
```

### 진행 상황 리셋

```csharp
// 모든 튜토리얼 리셋
TutorialManager.Instance.ResetAllProgress();

// 특정 시퀀스만 리셋
battleTutorial.ResetCompletion();
```

---

## 이벤트 연동

### WaitForTargetClick 연동

버튼 클릭 시 튜토리얼 진행:

```csharp
using Tutorial;
using UnityEngine;
using UnityEngine.UI;

public class TutorialButton : MonoBehaviour
{
    [SerializeField] private TutorialEvents tutorialEvents;
    [SerializeField] private Button button;

    private void Awake()
    {
        button.onClick.AddListener(OnButtonClick);
    }

    private void OnButtonClick()
    {
        // 튜토리얼 이벤트 발생
        // 경로는 Hierarchy 전체 경로와 일치해야 함
        string path = GetHierarchyPath(transform);
        tutorialEvents?.RaiseStepActionCompleted($"CLICK_{path}");
    }

    private string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
```

### WaitForEvent 연동

게임 이벤트 발생 시 튜토리얼 진행:

```csharp
using Tutorial;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private TutorialEvents tutorialEvents;

    public void StartBattle()
    {
        // 전투 시작 로직...

        // 튜토리얼 이벤트 발생
        tutorialEvents?.RaiseStepActionCompleted("BATTLE_START");
    }

    public void OnEnemyKilled()
    {
        // 적 처치 로직...

        tutorialEvents?.RaiseStepActionCompleted("ENEMY_KILLED");
    }

    public void OnWaveCleared(int waveNumber)
    {
        // 웨이브 클리어 로직...

        tutorialEvents?.RaiseStepActionCompleted($"WAVE_{waveNumber}_CLEARED");
    }
}
```

### 이벤트 키 규칙 (권장)

| 상황 | 이벤트 키 형식 | 예시 |
|------|---------------|------|
| 버튼 클릭 | `CLICK_{경로}` | `CLICK_Canvas/BattleBtn` |
| 전투 관련 | `BATTLE_{액션}` | `BATTLE_START`, `BATTLE_END` |
| 웨이브 관련 | `WAVE_{번호}_{액션}` | `WAVE_1_CLEARED` |
| 스킬 관련 | `SKILL_{액션}` | `SKILL_USED`, `SKILL_UNLOCKED` |
| UI 관련 | `UI_{액션}` | `UI_OPENED`, `UI_CLOSED` |

---

## CSV 텍스트 설정

### 파일 위치
`Assets/Data/CSV/Tutorial/TutorialTable.csv`

### CSV 형식

```csv
아이디,캐릭터ID,캐릭터이름,텍스트
id,characterId,characterName,text
int,int,string,string
8110001,1,안내자,"관장님, 부임하시고 첫 업무이시니 하나씩 설명해 드릴게요"
8110002,1,안내자,"이곳은 서재입니다. 책을 관리하는 곳이에요"
```

### 헤더 구조 (3행)
1. **1행**: 한글 컬럼명
2. **2행**: 영문 컬럼명
3. **3행**: 데이터 타입
4. **4행~**: 실제 데이터

### Text ID 규칙 (권장)

| 범위 | 용도 |
|------|------|
| 8110001 ~ 8119999 | 기본 튜토리얼 |
| 8120001 ~ 8129999 | 전투 튜토리얼 |
| 8130001 ~ 8139999 | 도서관 튜토리얼 |
| 8140001 ~ 8149999 | 캐릭터 튜토리얼 |

---

## 트러블슈팅

### TutorialManager 초기화 실패

**증상**: `TutorialManager failed to initialize!`

**원인 및 해결**:
1. BootScene에서 TutorialManager 참조 확인
2. Addressables에 `TutorialCanvas`, `TutorialEvents` 등록 확인
3. TutorialCanvas 프리팹에 TutorialUIController 컴포넌트 확인

### 텍스트가 표시되지 않음

**증상**: `[Text ID: 123]` 형태로 표시

**원인 및 해결**:
1. CSV 파일 존재 여부 확인
2. Text ID가 CSV에 있는지 확인
3. CSVLoader 초기화 확인

### WaitForTargetClick이 작동하지 않음

**증상**: 버튼 클릭해도 다음 스텝으로 진행 안 됨

**원인 및 해결**:
1. HighlightTargetPath가 정확한지 확인 (찾기 버튼 사용)
2. 해당 버튼에 이벤트 호출 코드 추가 확인
3. 이벤트 키 형식 확인: `CLICK_{전체경로}`

### WaitForEvent가 작동하지 않음

**증상**: 이벤트 발생해도 다음 스텝으로 진행 안 됨

**원인 및 해결**:
1. CompleteEventKey와 RaiseStepActionCompleted의 키가 일치하는지 확인
2. TutorialEvents 참조가 null이 아닌지 확인
3. 튜토리얼이 해당 스텝에서 대기 중인지 확인

### 에디터에서 텍스트 미리보기 안 됨

**증상**: `(ID 123: 텍스트를 찾을 수 없음)`

**해결**:
1. `Tools > Tutorial Editor > Reload CSV` 실행
2. CSV 파일 경로 확인: `Assets/Data/CSV/Tutorial/TutorialTable.csv`

---

## Addressables 설정 체크리스트

| 에셋 | Addressables 키 | 그룹 |
|------|-----------------|------|
| TutorialCanvas.prefab | `TutorialCanvas` | Tutorial |
| TutorialEvents.asset | `TutorialEvents` | Tutorial |
| 음성 파일들 | 각 파일명 | Tutorial (또는 Audio) |

---

## 파일 구조

```
Assets/
├── 02. Scripts/Tutorial/
│   ├── TutorialManager.cs          # 메인 매니저
│   ├── TutorialSequence.cs         # 시퀀스 데이터
│   ├── TutorialStep.cs             # 스텝 데이터
│   ├── TutorialEvents.cs           # 이벤트 채널
│   ├── TutorialUIController.cs     # UI 컨트롤러
│   ├── TutorialEnums.cs            # 열거형
│   ├── Views/
│   │   ├── FullDialogView.cs
│   │   ├── CompactDialogView.cs
│   │   └── HighlightView.cs
│   └── Editor/
│       └── TutorialEditorWindow.cs # 에디터 툴
├── Data/CSV/Tutorial/
│   └── TutorialTable.csv           # 텍스트 데이터
├── ScriptableObjects/
│   ├── Tutorial/
│   │   └── TutorialEvents.asset
│   └── Tutorials/
│       └── BATTLE_BASIC.asset      # 시퀀스 에셋들
└── 05. Prefabs/Tutorial/
    └── TutorialCanvas.prefab       # UI 프리팹
```

---

## 버전 정보

- **작성일**: 2025-12-23
- **버전**: 1.0
- **작성자**: Claude Code
