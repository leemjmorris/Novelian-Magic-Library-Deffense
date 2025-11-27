# 작업 중인 내용 (2025-11-26)

## GitHub Issue #302 - 전투 시스템 개선

### 완료된 작업
- FindWithTag 캐싱
- CancellationToken Dispose 추가
- Double Despawn 방지
- TargetRegistry null 체크 수정
- Chain exploit 수정 (중복 히트 방지)
- 이모지 제거
- Mark 데미지 배율 구현
- Boss CC 면역 구현
- Slow/Root/Knockback CC 효과 구현
- Monster 스폰 시 Wall 방향 바라보기
- Projectile 지면 충돌 처리 (spawn grace period 추가)
- FloatingDamageText 및 DamageTextManager 생성
- CharacterPlacementManager gridPlaneY 높이 조절 수정

---

## 현재 진행 중: AOE 메테오 스킬 이펙트 문제

### 문제 상황
- `projectileEffectPrefab`에 **Meteor.prefab** 사용 중
- 이 프리팹은 "떨어지는 불덩이 + 자동 폭발"이 모두 포함된 완전한 시퀀스
- 코드에서 GameObject를 Lerp로 이동시켜도 ParticleSystem 내부 타이밍은 변하지 않음
- 결과: 메테오가 지면에 도착하기 전에 허공에서 폭발

### 분석 결과
**현재 광역 스킬.asset 설정:**
- `projectileEffectPrefab`: Meteor.prefab (문제!)
- `hitEffectPrefab`: 다른 프리팹

**Hovl Studio 에셋 구조:**
```
Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/
├── Meteor.prefab          ← 전체 시퀀스 (떨어지기 + 폭발)
├── Meteor 2.prefab        ← 전체 시퀀스 변형
├── Meteor hit.prefab      ← 순수 착탄 폭발만
├── Meteor hit 2.prefab    ← 순수 착탄 폭발 변형
├── Meteor shower.prefab   ← 메테오 샤워
└── Meteor shower 2.prefab ← 메테오 샤워 변형
```

### 해결 옵션

#### 옵션 1: hitEffectPrefab만 사용 (간단)
1. Unity에서 `광역 스킬.asset` 선택
2. Inspector에서:
   - `projectileEffectPrefab`: None (비움)
   - `hitEffectPrefab`: **Meteor hit.prefab** 드래그

#### 옵션 2: 트레일 + 폭발 분리 (권장)
1. `Meteor.prefab`을 복제하여 `Meteor Trail.prefab` 생성
2. 복제본에서 폭발 파티클 제거 (트레일만 남김)
3. 설정:
   - `projectileEffectPrefab`: Meteor Trail.prefab
   - `hitEffectPrefab`: Meteor hit.prefab

#### 옵션 3: 코드에서 임시 비주얼 생성
프리팹 수정 없이 코드만으로 간단한 구체 생성 후 이동

---

## 관련 파일

### AOE 스킬 코드
- `Assets/Scripts/Character.cs` - UseAOESkillAsync() 메서드 (589줄~)

### 스킬 데이터
- `Assets/ScriptableObjects/Skills/광역 스킬.asset`
- `Assets/Scripts/Skills/SkillData.cs` - SkillAssetData 구조

### 이펙트 프리팹
- `Assets/Hovl Studio/AOE Magic spells Vol.1/Prefabs/Meteor*.prefab`

---

## 다음 단계
사용자에게 3가지 옵션 제시 완료. 사용자가 선택하면 해당 방식으로 구현 진행.
