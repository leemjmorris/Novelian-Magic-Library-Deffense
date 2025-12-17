# AOE 이펙트 시스템 작업 기록 (2024-12-14)

## 작업 목표
AOE 스킬의 이펙트 크기를 데미지 범위(aoe_radius)에 맞게 자동 조절하는 시스템 구현

---

## 완료된 작업

### 1. AOE Preview 워크플로우 반전 (SkillEffectMapperWindow.cs)

**기존 워크플로우 (문제점):**
- 이펙트 크기를 기준으로 Gizmo를 맞추는 방식
- 직관적이지 않음

**새 워크플로우:**
1. **Step 1: Gizmo = aoe_radius (데미지 범위)**
   - 슬라이더로 Gizmo 반경 조절
   - CSV에 직접 저장 가능

2. **Step 2: Effect Scale 조절**
   - 이펙트를 스폰하고 스케일 슬라이더로 크기 조절
   - 이펙트가 Gizmo와 일치하도록 조정
   - `baseEffectRadius = aoe_radius / effectScale` 계산하여 저장

3. **Step 3: 런타임 미리보기**
   - 저장된 baseEffectRadius로 런타임 스케일 미리보기

**핵심 공식:**
```
baseEffectRadius = aoe_radius / effectScale (에디터에서 저장)
runtime_scale = aoe_radius / baseEffectRadius (런타임에서 계산)
```

### 2. 프리팹 인스턴스 에러 수정

**문제:**
- `InvalidOperationException: Destroying a GameObject inside a Prefab instance is not allowed`
- NewMaterialChange.cs 스크립트가 자식 오브젝트 삭제 시 에러

**해결:**
- `PrefabUtility.InstantiatePrefab` → `UnityEngine.Object.Instantiate` 변경
- 프리팹 연결을 끊어 자유롭게 수정 가능하도록 함

**수정된 메서드:**
- `SpawnEffectForScaling()`
- `SpawnEffectAtRuntimeScale()`

### 3. 런타임 AOE Gizmo 구현 (Character.cs)

**새로 추가된 필드:**
```csharp
[Header("Debug - AOE Gizmo (런타임 범위 표시)")]
[SerializeField] private bool showAOEGizmo = true;
[SerializeField] private Color aoeGizmoColor = new Color(1f, 0.5f, 0f, 0.3f);
[SerializeField] private Color aoeGizmoWireColor = new Color(1f, 0.3f, 0f, 1f);

private Vector3 lastAOETargetPosition;
private float lastAOERadius;
private float aoeGizmoDisplayTime;
private const float AOE_GIZMO_DURATION = 2f;
```

**새로 추가된 메서드:**
- `UpdateAOEGizmoInfo(Vector3 targetPosition, float radius)` - AOE 스킬 사용 시 호출
- `Update()` - Gizmo 표시 시간 감소
- `OnDrawGizmos()` - Scene View에 AOE 범위 원 그리기

**기능:**
- AOE 스킬 사용 시 타겟 위치에 주황색 원 표시
- 2초간 표시 후 페이드 아웃
- 반경 라벨 표시 ("AOE: X.X")
- Inspector에서 On/Off 및 색상 변경 가능

### 4. Character.SkillExecutor.cs 수정

`UseAOESkillAsync()` 메서드에 Gizmo 업데이트 호출 추가:
```csharp
// 3.5 런타임 AOE Gizmo 업데이트 (디버그용)
UpdateAOEGizmoInfo(impactPos, aoeRadius);
```

---

## 수정된 파일 목록

| 파일 | 변경 내용 |
|------|----------|
| `Assets/Editor/SkillEffectMapperWindow.cs` | AOE Preview 탭 워크플로우 반전, Instantiate 방식 변경 |
| `Assets/Scripts/Character/Character.cs` | AOE Gizmo 필드/메서드 추가 |
| `Assets/Scripts/Character/Character.SkillExecutor.cs` | UpdateAOEGizmoInfo() 호출 추가 |
| `Assets/Scripts/Skills/SkillEffectEntry.cs` | baseEffectRadius 필드 (이전에 추가됨) |
| `Assets/ScriptableObjects/Skills/SkillEffectDatabase.asset` | 질투의불꽃 baseEffectRadius=13.333333 저장됨 |

---

## 테스트 완료 항목

- [x] AOE Preview에서 Gizmo 반경 조절
- [x] Effect Scale 슬라이더로 이펙트 크기 조절
- [x] baseEffectRadius 저장 및 로드
- [x] 프리팹 인스턴스 에러 해결

---

## 다음 작업 예정

### 미테스트 항목
- [ ] 런타임 AOE Gizmo가 실제 게임 플레이에서 제대로 표시되는지 확인
- [ ] 다른 AOE 스킬들의 baseEffectRadius 설정

### 추가 개선 가능 항목
- [ ] AOE Gizmo 색상을 스킬 타입별로 다르게 설정
- [ ] 여러 캐릭터가 동시에 AOE 스킬 사용 시 Gizmo 겹침 처리
- [ ] Game View에서도 AOE 범위 표시 (디버그 옵션)

---

## 관련 스킬 데이터 (질투의불꽃 - skill_id: 39003)

```
skill_id: 39003
skill_name: 질투의불꽃
skillType: 2 (AOE)
aoe_radius: 40 (CSV 값)
baseEffectRadius: 13.333333 (저장됨)
runtime_scale: 40 / 13.333333 = 3.0
```

---

## 코드 참조

### 런타임 스케일 계산 (Character.SkillExecutor.cs:420-428)
```csharp
float effectScale = 1f;
var effectEntry = effectDb?.GetEntry(skillData.skill_id);
if (effectEntry != null && effectEntry.baseEffectRadius > 0)
{
    effectScale = aoeRadius / effectEntry.baseEffectRadius;
}
hitEffect.transform.localScale = Vector3.one * effectScale;
```

### AOE Gizmo 그리기 (Character.cs:756-773)
```csharp
private void OnDrawGizmos()
{
    if (!showAOEGizmo || aoeGizmoDisplayTime <= 0 || lastAOERadius <= 0)
        return;

    float alpha = Mathf.Clamp01(aoeGizmoDisplayTime / AOE_GIZMO_DURATION);
    Vector3 gizmoPos = lastAOETargetPosition;
    gizmoPos.y = 0.1f;

    // 채워진 원
    UnityEditor.Handles.color = fillColor;
    UnityEditor.Handles.DrawSolidDisc(gizmoPos, Vector3.up, lastAOERadius);

    // 와이어 원
    UnityEditor.Handles.color = wireColor;
    UnityEditor.Handles.DrawWireDisc(gizmoPos, Vector3.up, lastAOERadius);
}
```
