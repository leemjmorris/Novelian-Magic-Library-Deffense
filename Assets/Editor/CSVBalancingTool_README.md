# CSV Balancing Tool - 사용 가이드

## 🎯 개요
게임 밸런싱을 위한 CSV 데이터 실시간 편집 도구

## 📋 주요 기능
- ✅ 9개 테이블 편집 지원 (Character, MainSkill, SupportSkill, Monster, Wave, Stage, CharacterLevel, SkillLevel, MonsterLevel)
- ✅ 에디터에서 직접 CSV 값 수정
- ✅ CSV 파일로 저장 (덮어쓰기)
- ✅ 플레이모드 중 즉시 리로드
- ✅ 3행 헤더 형식 자동 유지 (스킬 테이블)
- ✅ 검색 필터 기능

## 🚀 사용 방법

### 1. 도구 열기
Unity 메뉴: `Tools → CSV Balancing Tool`

### 2. 플레이 모드 진입
**중요**: CSVLoader 초기화를 위해 먼저 플레이 모드로 진입해야 함
- Play 버튼 클릭
- CSVLoader가 모든 CSV 파일 로드
- 도구 창에 "Loaded: X characters, Y skills..." 메시지 표시

### 3. 데이터 편집
- 원하는 탭 선택 (Character / Main Skill / Support Skill 등)
- 표시된 값을 직접 수정
- Search 필터로 특정 ID 검색 가능

### 4. CSV에 저장
- `Save to CSV` 버튼 클릭
- 확인 다이얼로그에서 "Save" 선택
- 원본 CSV 파일이 수정된 내용으로 덮어써짐

### 5. 변경사항 즉시 반영
- `Reload All` 버튼 클릭
- CSV 파일에서 직접 읽어서 메모리에 다시 로드
- 게임 재시작 없이 즉시 반영

## 📝 지원 테이블

| 탭 | 파일명 | 설명 |
|---|---|---|
| Character | CharacterTable.csv | 캐릭터 기본 정보 |
| Main Skill | MainSkillTable.csv | 메인 스킬 (3행 헤더) |
| Support Skill | SupportSkillTable.csv | 보조 스킬 (3행 헤더) |
| Monster | MonsterTable.csv | 몬스터 기본 정보 |
| Wave | WaveTable.csv | 웨이브 스폰 정보 |
| Stage | StageTable.csv | 스테이지 설정 |
| Char Level | LevelTable.csv | 캐릭터 레벨별 스탯 |
| Skill Level | SkillLevelTable.csv | 스킬 레벨별 성장 (3행 헤더) |
| Monster Level | MonsterLevelTable.csv | 몬스터 레벨별 스탯 |

## ⚠️ 주의사항

1. **플레이 모드 필수**: CSVLoader가 초기화되지 않으면 데이터 로드 불가
2. **백업 권장**: CSV 저장 시 원본이 덮어써지므로 Git commit 또는 백업 필요
3. **3행 헤더 자동 유지**: 스킬 테이블(MainSkill, SupportSkill, SkillLevel)은 한글/영문/타입 헤더가 자동으로 보존됨
4. **Git 충돌 주의**: 여러 명이 동시에 같은 CSV를 수정하면 머지 충돌 발생 가능

## 🔧 기술 사양

### 에디터 직접 파일 읽기
- 에디터 모드: `Assets/Data/CSV/*.csv` 파일 직접 읽기
- 빌드 모드: Addressables 사용 (기존 방식)
- 수정 후 리로드 시 즉시 반영

### CSV Writer
- **표준 테이블**: CsvHelper 기본 Writer 사용
- **스킬 테이블**: 3행 헤더 보존 로직
  1. 원본 파일에서 한글/영문/타입 헤더 추출
  2. CsvHelper로 데이터만 임시 파일에 작성
  3. 헤더 + 데이터 조합하여 원본 덮어쓰기

### 리로드 이벤트
```csharp
CSVLoader.OnCSVReloaded += () => {
    // CSV 리로드 후 처리
};
```

## 🐛 문제 해결

### "CSVLoader not initialized" 오류
→ 플레이 모드로 먼저 진입하세요

### 저장 후 데이터가 반영 안 됨
→ `Reload All` 버튼을 클릭하세요

### CSV 파일이 깨짐
→ UTF-8 인코딩으로 저장됩니다. Git에서 복원하거나 백업 사용

## 📚 관련 Issue
- Issue #333: [Feature] 게임 밸런싱 도구 (CSV 데이터 실시간 편집 시스템)
