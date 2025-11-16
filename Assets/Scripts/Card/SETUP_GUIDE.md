# Dynamic Slot Position System 설정 가이드

## 개요
이 시스템은 다양한 모바일 기기의 해상도와 화면비에 대응하여 캐릭터 슬롯의 월드 좌표를 정확하게 계산합니다.

## 핵심 원리
1. **가상 평면(Virtual Plane)**: 캐릭터가 배치될 Z축 위치(-7.5f)에 투명한 계산용 평면 생성
2. **Raycast 기반 계산**: UI 슬롯 위치에서 메인 카메라로 Ray를 쏴서 가상 평면과의 교점을 구함
3. **디바이스별 보정**: 화면 비율(16:9, 18:9, 19.5:9 등)에 따라 오프셋 자동 적용
4. **동적 캐싱**: 계산된 위치를 캐싱하여 성능 최적화

---

## 설정 방법

### 1단계: DeviceSlotPositionConfig 생성

1. Unity 에디터에서 프로젝트 창 우클릭
2. `Create > Game > Device Slot Position Config` 선택
3. 이름을 `DeviceSlotConfig`로 지정
4. Inspector에서 설정 조정:

```
기본 설정:
- Base Z Position: -7.5 (캐릭터 배치 Z 위치)
- Default Offset: (0, 0)

화면 비율별 오프셋:
- 16:9 (일반적인 모바일): (0, 0)
- 18:9 (Galaxy S8, S9): (0, 0.2)
- 19.5:9 (iPhone X, 11, 12): (0, 0.3)
- 20:9 (Galaxy S20, S21): (0, 0.35)
- 4:3 (iPad): (0, -0.5)

고급 설정:
- Aspect Ratio Tolerance: 0.05 (비교 허용 오차)
- Use Screen Size Scaling: true
- Reference Screen Height: 1080
```

### 2단계: DynamicSlotPositionManager 추가

1. Canvas 또는 슬롯 부모 오브젝트에 `DynamicSlotPositionManager` 컴포넌트 추가
2. Inspector에서 참조 설정:

```
References:
- Slot Canvas: 슬롯이 있는 Canvas
- UI Camera: Canvas의 렌더링 카메라 (Screen Space - Camera의 경우)
- Main Camera: Main Camera 태그가 있는 카메라
- Device Config: 위에서 만든 DeviceSlotConfig 에셋

Virtual Plane Settings:
- Show Debug Plane: true (디버깅용, 배포 시 false)
- Debug Plane Material: (옵션) 커스텀 반투명 머티리얼
```

### 3단계: 기존 PlayerSlot 확인

`PlayerSlot.cs`는 자동으로 `DynamicSlotPositionManager`를 찾아 사용합니다.
- 찾지 못하면 기존 Legacy 방식으로 동작 (하위 호환성)

---

## 사용 예시

### 자동 사용 (권장)
```csharp
// PlayerSlot에서 캐릭터 할당 시 자동으로 동적 위치 계산
playerSlot.AssignCharacterData(characterData);
```

### 수동 위치 계산
```csharp
// DynamicSlotPositionManager 직접 사용
DynamicSlotPositionManager manager = FindFirstObjectByType<DynamicSlotPositionManager>();

// 특정 슬롯의 월드 위치 계산
RectTransform slotRect = playerSlot.transform as RectTransform;
Vector3 worldPos = manager.CalculateWorldPositionForSlot(slotRect);

// 화면 좌표에서 직접 계산
Vector3 worldPosFromScreen = manager.CalculateWorldPositionFromScreen(new Vector2(500, 300));

// 모든 슬롯 위치 재계산 (화면 회전 시)
PlayerSlot[] allSlots = FindObjectsOfType<PlayerSlot>();
manager.RecalculateAllPositions(allSlots);
```

---

## 디바이스별 오프셋 조정 방법

### 실제 기기에서 테스트
1. 빌드하여 실제 기기에서 실행
2. 로그에서 현재 화면 비율 확인:
   ```
   [DynamicSlotPositionManager] Device Info -
   Resolution: 1080x2340, Aspect Ratio: 2.17, Offset: (0, 0.3)
   ```
3. 캐릭터가 슬롯과 어긋나면 `DeviceSlotConfig`에서 해당 비율의 오프셋 조정
4. 재빌드 후 확인

### 새로운 화면 비율 추가
```csharp
// DeviceSlotConfig Inspector에서 Aspect Ratio Offsets 배열에 추가
{
    aspectRatio = 21f/9f,  // 예: Galaxy S21 Ultra
    description = "21:9 (Ultra Wide)",
    offset = new Vector2(0, 0.4f)
}
```

---

## 디버깅

### 가상 평면 시각화
`Show Debug Plane = true`로 설정하면 파란색 반투명 평면이 표시됩니다.
- Scene 뷰에서 캐릭터가 이 평면 위에 배치되는지 확인

### Gizmo 확인
Scene 뷰에서 초록색 와이어 구체들이 캐시된 슬롯 위치를 표시합니다.

### 로그 확인
```
[DynamicSlotPositionManager] Raycast Hit -
Screen: (540, 960),
Ray Origin: (0, 0, 10),
Ray Direction: (0.1, -0.2, -1),
Hit Distance: 17.50,
World Position: (1.75, -3.5, -7.5)
```

---

## 주의사항

1. **Canvas 설정**: Canvas는 반드시 `Screen Space - Camera` 모드여야 합니다.
2. **Camera 설정**: UI Camera와 Main Camera가 모두 활성화되어 있어야 합니다.
3. **Z 위치**: 모든 캐릭터는 동일한 Z 위치(-7.5f)에 배치됩니다.
4. **성능**: 위치는 한 번 계산 후 캐싱되므로 성능 영향은 거의 없습니다.

---

## 트러블슈팅

### 문제: 캐릭터가 슬롯과 다른 위치에 생성됨
- `DeviceSlotConfig`의 오프셋 값 조정
- 로그에서 Raycast 계산 결과 확인
- Main Camera의 위치/회전 확인

### 문제: "DynamicSlotPositionManager not found" 경고
- Canvas나 부모 오브젝트에 컴포넌트가 추가되었는지 확인
- 문제없음: Legacy 방식으로 자동 폴백

### 문제: Raycast가 평면을 맞추지 못함
- Main Camera가 평면을 향하고 있는지 확인
- Base Z Position이 카메라 시야 안에 있는지 확인
- 가상 평면을 시각화하여 위치 확인 (`Show Debug Plane = true`)

---

## 향후 확장 가능성

- 슬롯별 개별 Y 오프셋 (상단/하단 슬롯 다르게)
- 화면 회전 감지 및 자동 재계산
- 런타임 오프셋 조정 UI (디버그용)
- 여러 Z 레이어 지원 (전경/후경 캐릭터)
