# Issue #476 - 도전던전 콘텐츠 구현 작업내역

## 작업일: 2025-12-17

---

## 1. CSV 데이터 리팩토링

### 1.1 MonsterLevelTable.csv 최신화
**경로**: `Assets/Data/CSV/Monster/MonsterLevelTable.csv`

**변경사항**:
- `Level_Type`: enum(Level1~Level10) → INT(1~10)로 변경
- `Monster_Grade`: 값 변경
  - 1~4레벨: Normal(1) - 일반 몬스터
  - 5~7레벨: Elite(2) - 정예 몬스터
  - 8~10레벨: Boss(3) - 보스 몬스터
- `Move_Speed`: 기획 CSV 값 반영 (4.0~6.0)

### 1.2 MonsterLevelData.cs 수정
**경로**: `Assets/02. Scripts/Data/CSV/Data/MonsterLevelData.cs`

**변경사항**:
```csharp
// Level_Type: enum → int로 변경
public int Level_Type { get; set; }  // 1~10 레벨

// MonsterGrade enum 업데이트
public enum MonsterGrade
{
    Normal = 1,   // 일반 몬스터
    Elite = 2,    // 정예 몬스터 (구 MidBoss)
    Boss = 3      // 보스 몬스터 (구 FinalBoss)
}
```

### 1.3 MonsterTable.csv 최신화
**경로**: `Assets/Data/CSV/Monster/MonsterTable.csv`

**변경사항**:
- `Description_ID` 데이터 추가 (80050106~80050140)
- `Is_Boss` 컬럼 추가 (1=보스, 0=일반)

**보스 몬스터 목록 (Is_Boss=1)**:
| Monster_ID | Monster_Name | Genre |
|------------|--------------|-------|
| 031001 | 핑크 슬라임 | Horror (1) |
| 033017 | 사해 탐구자 | Adventure (3) |
| 034025 | 로봇 깡통 로봇 | Comedy (4) |
| 035030 | 질문 악마 | Mystery (5) |

### 1.4 MonsterData.cs 수정
**경로**: `Assets/02. Scripts/Data/CSV/Data/MonsterData.cs`

**변경사항**:
```csharp
public int Is_Boss { get; set; }

/// <summary>
/// 보스 몬스터 여부 확인
/// </summary>
public bool IsBoss => Is_Boss == 1;
```

---

## 2. 도전던전 CSV 생성

### 2.1 BossDungeonTable.csv 생성
**경로**: `Assets/Data/CSV/BossDungeon/BossDungeonTable.csv`

**컬럼 구조**:
| 컬럼명 | 타입 | 설명 |
|--------|------|------|
| Dungeon_ID | INT | 던전 고유 ID (24001~24100) |
| Floor_Index | INT | 스테이지 번호 (1~100) |
| Boss_ID | INT | 보스 몬스터 ID (MonsterTable 참조) |
| Boss_Level_ID | INT | 보스 레벨 ID (MonsterLevelTable 참조) |
| Attack_Count | INT | 스턴 전 공격 횟수 |
| Attack_Period | FLOAT | 보스 공격 주기 (초) |
| Stun_Damage | INT | 스턴 게이지 감소량 |
| Stun_Gauge | FLOAT | 스턴 게이지 최대값 |
| Stun_Duration | FLOAT | 스턴 지속 시간 (초) |
| Time_Limit | FLOAT | 제한 시간 (초) |
| Reward_Group_ID | INT | 보상 그룹 ID |
| Is_Implemented | INT | 구현 여부 (1=구현, 0=미구현) |

**1~10 스테이지 데이터**:
| 스테이지 | Boss_ID | Boss_Level | 제한시간 | 스턴지속 |
|---------|---------|------------|---------|---------|
| 1 | 031001 | 2208 | 30초 | 3초 |
| 2 | 033017 | 2208 | 30초 | 3초 |
| 3 | 034025 | 2209 | 30초 | 4초 |
| 4 | 035030 | 2209 | 40초 | 4초 |
| 5 | 031001 | 2209 | 40초 | 4초 |
| 6 | 033017 | 2210 | 40초 | 5초 |
| 7 | 034025 | 2210 | 50초 | 5초 |
| 8 | 035030 | 2210 | 50초 | 5초 |
| 9 | 031001 | 2210 | 50초 | 6초 |
| 10 | 033017 | 2210 | 60초 | 6초 |

### 2.2 BossDungeonData.cs 생성
**경로**: `Assets/02. Scripts/Data/CSV/Data/BossDungeonData.cs`

```csharp
public class BossDungeonData
{
    public int Dungeon_ID { get; set; }
    public int Floor_Index { get; set; }
    public int Boss_ID { get; set; }
    public int Boss_Level_ID { get; set; }
    public int Attack_Count { get; set; }
    public float Attack_Period { get; set; }
    public int Stun_Damage { get; set; }
    public float Stun_Gauge { get; set; }
    public float Stun_Duration { get; set; }
    public float Time_Limit { get; set; }
    public int Reward_Group_ID { get; set; }
    public int Is_Implemented { get; set; }

    public bool IsImplemented => Is_Implemented == 1;
}
```

---

## 3. CSVLoader 및 AddressableKey 등록

### 3.1 Defines.cs 수정
**경로**: `Assets/02. Scripts/Defines.cs`

```csharp
// Issue #476 - 도전던전 테이블
public static readonly string BossDungeonTable = "BossDungeonTable";
```

### 3.2 CSVLoader.cs 수정
**경로**: `Assets/02. Scripts/Data/CSV/CSVLoader.cs`

**csvPathMap 추가**:
```csharp
// BossDungeon 폴더 (Issue #476)
{ AddressableKey.BossDungeonTable, "BossDungeon/BossDungeonTable.csv" },
```

**LoadAll() 및 ReloadAllTablesAsync() 추가**:
```csharp
// 도전던전 테이블 (Issue #476)
RegisterTableAsync<BossDungeonData>(AddressableKey.BossDungeonTable, x => x.Dungeon_ID),
```

---

## 4. 남은 작업 (다음 세션에서 진행)

### 4.1 씬 & UI
- [ ] 도전던전 전용 씬 생성 (BossDungeonScene)
- [ ] BossDungeonSelectPanel UI 구현 (100개 스테이지 표시)
- [ ] 스테이지 정보 팝업 구현
- [ ] "구현 예정" 안내 팝업 구현 (11~100 스테이지용)

### 4.2 보스 시스템
- [ ] 보스 AI 수정 (도전던전용 행동 패턴)
- [ ] 스턴 게이지 UI 및 스턴 시스템 구현
- [ ] 타이머 UI 및 경고 효과 구현

### 4.3 게임플레이
- [ ] 카드 4연속 선택 시스템 구현
- [ ] 캐릭터 자동 배치 & 책갈피 적용 로직
- [ ] 결과 화면 (클리어/실패) 구현
- [ ] 일시정지 기능 구현

### 4.4 데이터 연동
- [ ] 서버 연동 (스테이지 해금 상태 저장)
- [ ] 보상 RewardGroupTable 추가 (18001~18010)

---

## 5. 파일 변경 목록

| 파일 | 변경 유형 |
|------|----------|
| `Assets/Data/CSV/Monster/MonsterLevelTable.csv` | 수정 |
| `Assets/Data/CSV/Monster/MonsterTable.csv` | 수정 |
| `Assets/Data/CSV/BossDungeon/BossDungeonTable.csv` | 신규 |
| `Assets/02. Scripts/Data/CSV/Data/MonsterLevelData.cs` | 수정 |
| `Assets/02. Scripts/Data/CSV/Data/MonsterData.cs` | 수정 |
| `Assets/02. Scripts/Data/CSV/Data/BossDungeonData.cs` | 신규 |
| `Assets/02. Scripts/Data/CSV/CSVLoader.cs` | 수정 |
| `Assets/02. Scripts/Defines.cs` | 수정 |
