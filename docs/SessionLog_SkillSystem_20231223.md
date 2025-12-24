# 스킬 시스템 개발 세션 로그

**날짜**: 2023-12-23
**프로젝트**: Novelian Magic Library Defense
**주제**: POE2 스타일 스킬 시스템 + 에디터 툴 개발

---

## 세션 개요

이 문서는 스킬 시스템 개발 과정에서 나눈 대화 내용을 정리한 것입니다.

---

## 1. 이전 세션 요약 (컨텍스트 요약에서 복원)

### 1.1 핵심 아키텍처
- **POE2 스타일 스킬 시스템**: 메인 스킬 + 서포트 스킬 조합
- **VFX Container + Branch 패턴**: 외부 VFX 에셋을 수정 없이 감싸서 사용
- **CSV 기반 데이터 관리**: MainSkillTable.csv에서 스킬 스탯 관리

### 1.2 주요 파일 구조
```
Assets/
├── 02. Scripts/
│   ├── Skills/
│   │   ├── SkillProjectile.cs      # 투사체 로직
│   │   ├── SkillExecutor.cs        # 스킬 실행 로직
│   │   ├── SkillVFXDatabase.cs     # VFX 프리팹 데이터베이스
│   │   ├── SkillVFXContainer.cs    # 외부 VFX 래퍼
│   │   └── TargetableUtils.cs      # 타겟팅 유틸리티
│   └── Data/CSV/Data/
│       └── MainSkillData.cs        # CSV 데이터 클래스
├── Data/CSV/Skill/
│   └── MainSkillTable.csv          # 스킬 데이터 테이블
├── Editor/
│   └── SkillEditorWindow.cs        # 스킬 에디터 툴
└── SpecialSkillsEffectsPack/       # 외부 VFX 에셋
```

### 1.3 CLAUDE.md 규칙
- SerializedField 우선 사용 (FindByTag는 대안)
- UniTask 사용 (다른 비동기 방식 금지)
- 한국어로 대화

---

## 2. SpecialSkillsEffectsPack 분석

### 2.1 질문
> "이런것도 활용할 수 있게 툴을 만들 수 있어? SpriptBase 이펙트들도 Tool에서 기능을 지원하나?"

### 2.2 분석 결과
- **위치**: `Assets/SpecialSkillsEffectsPack/AllEffects/`
- **구조**:
  - `NotScriptBased/` - 파티클만 있는 이펙트 (직접 사용 가능)
  - `ScriptBased/` - 스크립트 포함 이펙트 (래핑 필요)
- **포함된 스크립트**:
  - ObjectMove, MultipleObjectsMake, DestroyObject
  - LookAtTarget, ShieldActivate
- **호환성**: 90%+ 호환 가능

### 2.3 해결 방안
- **NotScriptBased**: VFXDatabase에 직접 등록
- **ScriptBased**: SkillVFXContainer로 래핑 후 등록

---

## 3. 스킬 에디터 5번째 탭 추가 (외부 에셋)

### 3.1 구현 내용
SkillEditorWindow.cs에 "외부 에셋" 탭 추가

```csharp
private enum Tab
{
    VFXDatabase,
    CSVSync,
    Preview,
    Test,
    ExternalAssets  // 새로 추가
}
```

### 3.2 주요 기능
1. **에셋 스캔**: 지정 폴더에서 프리팹 자동 탐색
2. **ScriptBased/NotScriptBased 자동 분류**
3. **behavior_type 자동 추천**: 이펙트 이름 기반 매핑
4. **Container 자동 생성**: ScriptBased 이펙트 래핑
5. **일괄 등록 기능**

### 3.3 behavior_type 자동 매핑 테이블
```csharp
private static readonly Dictionary<string, string> EffectNameToBehaviorType = new Dictionary<string, string>
{
    // AOE 타입
    { "tornado", "MovingAOE" },
    { "storm", "MovingAOE" },
    { "nuke", "TargetAOE" },
    { "explosion", "TargetAOE" },

    // Falling 타입
    { "orbital", "FallingProjectile" },
    { "meteor", "FallingProjectile" },

    // Beam 타입
    { "beam", "BeamRay" },
    { "laser", "BeamRay" },

    // Projectile 타입
    { "shot", "SingleProjectile" },
    { "ball", "SingleProjectile" },

    // 기타
    { "shield", "Barrier" },
    { "slash", "LinearAOE" },
};
```

---

## 4. SkillExecutor.cs 에러 수정

### 4.1 에러
```
error CS1061: 'MainSkillData' does not contain a definition for 'cast_time'
```

### 4.2 원인
MainSkillData.cs에 `cast_time` 필드가 없음. `duration` 필드만 존재.

### 4.3 수정
```csharp
// 변경 전
float warningDuration = mainSkill.cast_time > 0 ? mainSkill.cast_time : DEFAULT_WARNING_DURATION;

// 변경 후
float warningDuration = mainSkill.duration > 0 ? mainSkill.duration : DEFAULT_WARNING_DURATION;
```

---

## 5. 초기화/리셋 기능 추가

### 5.1 질문
> "툴에 초기화 하는 버튼도 만들어줘. CSV도 그렇고 지금 다시 만들어야 할것 같아."

### 5.2 구현 기능

#### VFX Database 탭
- **VFX Database 초기화**: 모든 Entry 삭제
- **새 Database 생성**: 새 ScriptableObject 생성

#### CSV 동기화 탭
- **CSV 템플릿 생성**: 빈 헤더만 있는 CSV
- **샘플 CSV 생성**: 16개 예제 스킬 포함
- **전체 초기화**: Database + Containers + 캐시 모두 리셋

### 5.3 샘플 CSV 내용
```csv
skill_id,//skill_name,behavior_type,base_damage,cooldown,range,projectile_speed,aoe_radius,duration,//description
1001,파이어볼,SingleProjectile,100,2,15,20,0,0,기본 화염 투사체
1101,폭발 화살,ExplosiveProjectile,150,3,20,15,3,0,폭발하는 투사체
1201,운석 낙하,FallingProjectile,300,8,25,0,5,1.5,하늘에서 떨어지는 운석
2001,레이저 빔,BeamRay,50,5,20,0,0,3,지속 빔 공격
3001,폭발,TargetAOE,200,6,15,0,5,0,즉시 폭발
3201,독 웅덩이,GroundAOE,20,8,15,0,4,5,지속 피해 장판
3301,토네이도,MovingAOE,80,10,20,3,3,6,이동하는 회오리
4001,보호막,Barrier,0,15,0,0,5,10,아군 보호막
```

---

## 6. Deprecated API 경고 수정

### 6.1 경고
```
warning CS0618: 'Object.FindObjectOfType<T>()' is obsolete
warning CS0618: 'Object.FindObjectsOfType<T>()' is obsolete
```

### 6.2 수정
```csharp
// 변경 전
var testManager = FindObjectOfType<SkillTestManager>();
var monsters = FindObjectsOfType<Monster>();

// 변경 후
var testManager = FindFirstObjectByType<SkillTestManager>();
var monsters = FindObjectsByType<Monster>(FindObjectsSortMode.None);
```

---

## 7. CSV 경로 수정

### 7.1 문제
기본 CSV 경로가 `Assets/06. Data/`로 되어있어 파일을 찾지 못함.

### 7.2 수정
```csharp
// 변경 전
private string csvPath = "Assets/06. Data/MainSkillTable.csv";

// 변경 후
private string csvPath = "Assets/Data/CSV/Skill/MainSkillTable.csv";
```

### 7.3 영향 받은 메서드
- `CreateCSVTemplate()` 기본 저장 경로
- `CreateSampleCSV()` 기본 저장 경로

---

## 8. CSV 파싱 로직 수정 (현재 세션)

### 8.1 문제
> "분명 CSV에 이전에 사용한 CSV 값들이 있을텐데 왜 0개 로드되었다고 뜨지?"

### 8.2 원인
CSV 파일 형식이 예상과 다름:
```csv
//스킬ID,//스킬명,행동타입,...           <- 줄 1: 주석 헤더
skill_id,//skill_name,behavior_type,...  <- 줄 2: 실제 헤더
int,//string,string,...                  <- 줄 3: 타입 정의
1001,자연탄,SingleProjectile,...         <- 줄 4+: 데이터
```

기존 코드는 줄 1을 헤더로 인식해서 `skill_id` 컬럼을 찾지 못함.

### 8.3 수정된 LoadCSVData()
```csharp
private void LoadCSVData()
{
    // 헤더 라인 찾기 (skill_id가 포함된 줄)
    int headerLineIndex = -1;
    for (int i = 0; i < Math.Min(5, lines.Length); i++)
    {
        if (lines[i].Contains("skill_id"))
        {
            headerLineIndex = i;
            break;
        }
    }

    // 헤더 파싱 - //로 시작하는 주석 헤더 처리
    for (int i = 0; i < headers.Length; i++)
    {
        string header = headers[i].Trim();
        if (header.StartsWith("//"))
            header = header.Substring(2);
        headerIndex[header] = i;
    }

    // 타입 정의 줄 건너뛰기
    int dataStartLine = headerLineIndex + 1;
    if (dataStartLine < lines.Length)
    {
        string firstVal = ParseCSVLine(lines[dataStartLine])[0].Trim().ToLower();
        if (firstVal == "int" || firstVal == "float" || firstVal == "string")
        {
            dataStartLine++; // 타입 정의 줄 건너뛰기
        }
    }

    // 데이터 파싱 (주석 줄 건너뛰기)
    for (int i = dataStartLine; i < lines.Length; i++)
    {
        if (lines[i].TrimStart().StartsWith("//")) continue;
        // ... 파싱 로직
    }
}
```

### 8.4 현재 CSV 데이터 (34개 스킬)
```
1001~1010: SingleProjectile (10개)
1101~1103: ExplosiveProjectile (3개)
1201~1206: BeamRay (6개)
1301~1306: TargetAOE (6개)
1401~1402: LinearAOE (2개)
1501~1502: GroundAOE (2개)
1601~1602: Barrier (2개)
1701~1702: Buff (2개)
```

---

## 9. 현재 상태 요약

### 9.1 완료된 작업
- [x] SkillEditorWindow 5개 탭 구현
- [x] 외부 에셋 스캔 및 자동 분류
- [x] Container 자동 생성 기능
- [x] CSV 초기화/샘플 생성 기능
- [x] deprecated API 경고 수정
- [x] CSV 경로 수정
- [x] CSV 파싱 로직 개선 (주석/타입 줄 처리)

### 9.2 사용 방법
1. Unity에서 `Tools → Novelian → 스킬 에디터` (Ctrl+Shift+K)
2. VFX Database 선택
3. "CSV 동기화" 탭에서 CSV 로드
4. "외부 에셋" 탭에서 VFX 스캔 및 할당

### 9.3 남은 작업
- [ ] VFX 매핑 도우미 윈도우 구현 완료
- [ ] 실제 게임에서 스킬 테스트
- [ ] 서포트 스킬 시스템 연동

---

## 10. 주요 코드 변경 이력

| 파일 | 변경 내용 |
|------|----------|
| SkillEditorWindow.cs | 외부 에셋 탭, 초기화 기능, CSV 파싱 개선 |
| SkillExecutor.cs | cast_time → duration 수정 |
| MainSkillData.cs | 변경 없음 (참조용) |
| SkillVFXDatabase.cs | 변경 없음 (참조용) |
| SkillVFXContainer.cs | 변경 없음 (참조용) |

---

## 11. 다음 세션에서 이어서 할 작업

1. **스킬 에디터 테스트**: CSV 34개 스킬 로드 확인
2. **외부 에셋 연동**: SpecialSkillsEffectsPack VFX 할당
3. **런타임 테스트**: 실제 게임에서 스킬 발사 테스트
4. **버그 수정**: 발견되는 이슈 해결

---

*이 문서는 다른 컴퓨터에서 작업을 이어갈 때 컨텍스트 복원용으로 사용됩니다.*
