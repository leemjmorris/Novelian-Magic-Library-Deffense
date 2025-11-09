# 📚 Novelian: Magic Library Defense

> 좋아하는 캐릭터를 모집하고, 애정 캐릭터를 나만의 스타일로 성장시키는 2D 캐주얼 방치형 디펜스 게임

## 🎮 프로젝트 개요

### 게임 정보
- **프로젝트명**: Novelian: Magic Library Defense (노벨리안: 마법 도서관 디펜스)
- **장르**: 방치형 디펜스, 2D 캐주얼, 서브컬쳐
- **개발 엔진**: Unity 6.0
- **프로젝트 유형**: 기업 협약 프로젝트

### 게임 컨셉
캐릭터를 모집하고, 파티를 구성하여 책에서 튀어나오려는 마물들을 전투를 통해 막으며 캐릭터들을 성장시키는 게임입니다.

**핵심 테마**: "자신이 좋아하는 캐릭터의 성능을 커스텀 해 오래동안 게임을 즐길 수 있게 하자"

---

## 👥 팀 구성

| 이름 | 역할 | GitHub |
|------|------|--------|
| 이명진 | 개발 팀장 | [@leemjmorris] |
| 이재문 | 프로그래밍 | [@jaemoon23] |
| 이채빈 | 프로그래밍 | [@LeeChaeBin002] |
| 김동욱 | PD | [@Kdwio] |
| 김민휘 | 시스템 기획 / BM | [@bigwaterplz] |
| 김지원 | 콘텐츠 기획 | [@kimjiw8698-crypto] |

---

## 🎯 핵심 시스템

### 📖 책갈피 시스템 (Core)
- 캐릭터에게 능력치를 부여하는 특별한 아이템
- 플레이어가 캐릭터의 성능 방향을 직접 설계 가능
- 같은 캐릭터라도 책갈피에 따라 완전히 다른 전투 스타일 구현

### 🎲 파생 콘텐츠
- **도감 시스템**: 캐릭터 수집 및 기록
- **채집 시스템**: 자동 재료 수집
- **도전 던전**: 성장 상태 테스트
- **일일/주간 퀘스트**: 꾸준한 플레이 유도

---

## 📊 프로젝트 관리

### 백로그 & 스프린트 관리
우리 팀은 **백로그 시스템**을 사용하여 작업을 관리합니다.

- 📥 **백로그**: 아직 스프린트에 배정되지 않은 모든 작업
- ✅ **준비 완료**: 다음 스프린트에 투입 가능한 작업
- 🚀 **진행 중**: 현재 작업 중인 이슈
- 👀 **리뷰 중**: 코드 리뷰가 필요한 작업
- ✨ **완료**: 마무리된 작업

📖 **가이드**: [백로그 관리 가이드](docs/BACKLOG_GUIDE.md)

### 🤖 AI 기반 Slack 알림
**Gemini API**를 활용하여 Issue를 자동으로 요약하고 Slack으로 전송합니다.

- ✨ AI가 Issue 내용을 한국어로 요약
- 📊 중요 정보 자동 강조 (담당자, 우선순위, 마일스톤)
- 🎨 이모지로 가독성 향상
- 🔔 실시간 팀 알림

🔧 **설정 방법**: [Gemini-Slack 연동 가이드](docs/GEMINI_SLACK_SETUP.md)

### 이슈 & PR 규칙
- 모든 기능/버그는 Issue로 등록
- Branch 명명: `타입/이슈번호-설명` (예: `feature/32-bookmark-system`)
- PR은 반드시 관련 Issue를 참조 (`Closes #32`)

---

# Novelian 프로젝트 코딩 컨벤션

## **1. 명명 규칙 (Naming Conventions)**

### **클래스 & 구조체**

    // 클래스: PascalCase
    public class BookmarkSystem { }

    // 인터페이스: I + PascalCase
    public interface IBookmarkable { }

    // 추상 클래스: Base/Abstract + PascalCase
    public abstract class BaseBookmarkSystem { }

### **메서드**

    // 메서드: PascalCase
    public void ApplyBookmark() { }

    // 이벤트 핸들러: Handle + PascalCase
    private void HandleBookmarkApplied() { }

    // UniTask: 동사 + Async 접미사
    private async UniTask LoadBookmarkAsync() { }

### **변수**

    // 변수: camelCase
    private int bookmarkCount;

    // 상수: UPPER_SNAKE_CASE
    private const int MAX_BOOKMARK_SLOTS = 5;

    // SerializeField: camelCase
    [SerializeField] private int maxBookmarkSlots = 5;

    // Public 프로퍼티: PascalCase
    public int MaxBookmarkSlots => maxBookmarkSlots;

### **이벤트 & 델리게이트**

    // 이벤트: On + PascalCase
    public event Action OnBookmarkApplied;

---

## **2. 네임스페이스 (Namespace)**

    namespace Novelian.Bookmark { }
    namespace Novelian.UI { }
    namespace Novelian.Managers { }
    namespace Novelian.Core { }
    namespace Novelian.Utilities { }

---

## **3. 주석 규칙 (Comments)**

    // 본인이 작성한 메서드에는 영어로 필수 기입
    // 형식: //이니셜 : 설명

    //LMJ : Applies the bookmark to current position
    public void ApplyBookmark() 
    {
        // 구현
    }

    //LMJ : Loads bookmark data asynchronously from server
    private async UniTask LoadBookmarkAsync() 
    {
        // 구현
    }

---

## **4. Inspector 노출 변수**

    // Header로 그룹화, Tooltip으로 설명 추가 (기획자 협업용)
    [Header("Bookmark Settings")]
    [SerializeField, Tooltip("Maximum number of bookmarks")]
    private int maxBookmarkSlots = 5;

    // 범위 제한이 필요한 경우
    [SerializeField, Tooltip("Spawn interval in seconds"), Range(1f, 10f)]
    private float spawnInterval = 3f;

---

## **5. 매직 넘버 금지**

    // Bad
    if (count > 5) { }

    // Good
    private const int MAX_BOOKMARK_COUNT = 5;
    if (count > MAX_BOOKMARK_COUNT) { }

---

## **6. 프리팹/에셋 명명**
- **Prefabs**: PascalCase (예: `BookmarkUI`, `PlayerCharacter`)
- **ScriptableObjects**: PascalCase + Data/Config (예: `BookmarkData`, `StageConfig`)
- **Scenes**: PascalCase (예: `MainMenu`, `Stage01`)

---

## **7. 폴더 구조**

    Assets/
    ├── Scripts/
    │   ├── Core/
    │   ├── UI/
    │   ├── Managers/
    │   ├── Bookmark/
    │   └── Utilities/
    ├── Prefabs/
    ├── Resources/
    ├── ScriptableObjects/
    └── Scenes/

---

## 📞 Contact

프로젝트 관련 문의사항은 Issues 또는 Slack을 통해 남겨주세요.

---

## 📚 문서

- [백로그 관리 가이드](docs/BACKLOG_GUIDE.md)
- [Gemini-Slack 연동 가이드](docs/GEMINI_SLACK_SETUP.md)

---

**Last Updated**: 2025-11-09
