# 인벤토리 시스템 사용 가이드

## 개요

이 인벤토리 시스템은 **ItemTable**(CSV)의 데이터를 기반으로 재료 아이템을 UI에 표시합니다.
**BookMarkCraft**에서 재료를 추가하면 **IngredientManager**를 통해 자동으로 인벤토리에 반영됩니다.

---

## 파일 구조

### 1. **InventoryItemInfo.cs**
- **역할**: 인벤토리에 표시될 아이템의 데이터를 담는 클래스
- **주요 기능**:
  - ItemTable에서 아이템 정보 로드 (이름, 등급, 타입, 최대 수량 등)
  - 현재 보유 수량 관리
  - 여러 슬롯에 걸쳐 표시될 때 슬롯별 수량 계산

### 2. **InventoryItemSlot.cs**
- **역할**: UI에 표시되는 개별 아이템 슬롯 (와이어프레임의 그리드 하나)
- **주요 기능**:
  - 아이템 아이콘, 수량, 등급 배경색 표시
  - 길게 누르기(Long Press) 감지 → 아이템 정보 팝업
  - 빈 슬롯 처리

### 3. **InventoryController.cs**
- **역할**: 인벤토리 UI의 메인 컨트롤러
- **주요 기능**:
  - IngredientManager로부터 보유 재료 정보 로드
  - 아이템 정렬 (이름 순)
  - 슬롯 생성 및 배치 (최대 스택 초과 시 여러 슬롯 사용)
  - 아이템 정보 팝업 표시/숨김
  - 뒤로가기 버튼 처리

---

## 연동 구조

```
BookMarkCraft (재료 추가/사용)
        ↓
IngredientManager (재료 데이터 관리)
        ↓
InventoryController (인벤토리 UI 표시)
        ↓
InventoryItemSlot (개별 슬롯 UI)
```

### 데이터 흐름

1. **BookMarkCraft**에서 `IngredientManager.Instance.AddIngredient(id, count)` 호출
2. **IngredientManager**가 `Dictionary<int, int>`에 재료 저장
3. **InventoryController**가 `LoadInventoryData()`로 IngredientManager에서 데이터 가져오기
4. **ItemTable**에서 아이템 정보 조회 (`CSVLoader.Instance.GetData<ItemData>(id)`)
5. **InventoryItemInfo** 객체 생성 (ItemTable 데이터 + 보유 수량)
6. **InventoryItemSlot**에 아이템 정보 표시

---

## Unity 에디터 설정

### InventoryController 설정

1. **인벤토리 씬에 빈 GameObject 생성** → `InventoryController` 이름 지정
2. **InventoryController.cs** 컴포넌트 추가
3. **Inspector에서 다음 항목 할당**:

#### UI References
- `Inventory Grid Parent`: 아이템 슬롯들이 배치될 부모 Transform (Grid Layout Group 권장)
- `Item Slot Prefab`: InventoryItemSlot 프리팹
- `Scroll Rect`: ScrollRect 컴포넌트 (상하 스와이프용)
- `Back Button`: 뒤로가기 버튼

#### Item Detail Popup
- `Item Detail Popup`: 아이템 정보 팝업 패널 GameObject
- `Popup Item Icon`: 팝업의 아이템 아이콘 Image
- `Popup Item Name Text`: 팝업의 아이템 이름 TextMeshProUGUI
- `Popup Item Description Text`: 팝업의 아이템 설명 TextMeshProUGUI
- `Popup Item Current Count Text`: 팝업의 현재 보유량 TextMeshProUGUI
- `Popup Close Button`: 팝업 닫기 버튼 (배경 클릭 영역)

#### Settings
- `Grid Columns Count`: 3 (와이어프레임 기준)

---

### InventoryItemSlot 프리팹 구조

```
ItemSlot (Prefab)
├─ Background (Image)          // 슬롯 배경
├─ GradeBackground (Image)     // 등급별 배경색 (옵션)
├─ ItemIcon (Image)            // 아이템 아이콘
└─ CountText (TextMeshProUGUI) // "X 999"
```

**프리팹 설정**:
1. `InventoryItemSlot.cs` 컴포넌트 추가
2. Inspector에서 다음 할당:
   - `Item Icon Image`: ItemIcon Image 컴포넌트
   - `Count Text`: CountText TextMeshProUGUI 컴포넌트
   - `Grade Background Image`: GradeBackground Image 컴포넌트 (옵션)
   - `Long Press Duration`: 0.5 (기본값)

---

## 사용 예시

### 1. BookMarkCraft에서 재료 추가 시

```csharp
// BookMarkCraft.cs의 OnClickAddIngredientButton()
private void OnClickAddIngredientButton()
{
    IngredientManager.Instance.AddIngredient(102113, 10); // 재료1 추가
    IngredientManager.Instance.AddIngredient(102118, 10); // 재료2 추가

    // 인벤토리 씬이 활성화되어 있다면 UI 갱신
    // (인벤토리 씬 진입 시 자동으로 LoadInventoryData() 호출됨)
}
```

### 2. 외부에서 인벤토리 강제 갱신

```csharp
// 재료 추가/사용 후 인벤토리 UI 즉시 갱신
var inventoryController = FindObjectOfType<InventoryController>();
if (inventoryController != null)
{
    inventoryController.RefreshInventory();
}
```

---

## 와이어프레임 요구사항 구현

### 1. 메인 로비 -> 인벤토리 진입
- 로비 씬의 "인벤토리" 버튼 클릭
- Scene 전환 시 로딩 화면 표시 (TODO: 추가 구현 필요)
- 인벤토리 씬 로드

### 2. 인벤토리 기본 구성
- ScrollRect를 이용한 상하 스와이프
- 뒤로가기 버튼 클릭 → 로비 씬 이동 (TODO: 씬 전환 코드 활성화)

### 3. 아이템 정보 확인
- 슬롯을 **길게 누르면** 아이템 정보 팝업 표시
- 팝업 외부 영역 터치 시 닫힘

### 4. 아이템 스택 및 정렬
- **최대 스택 수량**: ItemTable의 `Max_Count` 값 사용
- **정렬 순서**: 이름 순 (가나다 순)
- **여러 슬롯 사용**: 보유 수량이 최대 스택을 초과하면 다음 슬롯에 표시
  - 예: Max_Count=999, 보유량=1009 → 슬롯1: X 999, 슬롯2: X 10

---

## 확장 및 커스터마이징

### 아이템 아이콘 및 정보 로드

**이미 구현 완료!** CardSelectionManager 패턴을 재활용하여 구현되었습니다.

#### 1. ItemTable CSV 설정
ItemTable.csv에 다음 필드가 필요합니다:
- `Icon_Path`: 아이템 아이콘의 Addressable 키 (예: "ItemIcon_102113")
- `Description`: 아이템 설명 (예: "마력이 깃든 종이")

#### 2. Addressables 설정
1. 아이템 아이콘 스프라이트를 Addressables 그룹에 추가
2. 각 스프라이트의 Address를 ItemTable의 `Icon_Path`와 일치시키기
   - 예: `ItemIcon_102113`, `ItemIcon_102118`

#### 3. 자동 로드
- **InventoryItemSlot**: 슬롯에 아이템이 설정되면 자동으로 아이콘 로드
- **InventoryController**: 팝업 표시 시 자동으로 아이콘, 이름, 설명, 수량 표시

```csharp
// 이미 구현된 로드 로직 (InventoryItemSlot.cs)
Sprite icon = await Addressables.LoadAssetAsync<Sprite>(itemInfo.IconPath).Task;
if (icon != null && itemIconImage != null)
{
    itemIconImage.sprite = icon;
}
```

---

## 주의사항

1. **IngredientManager가 DontDestroyOnLoad 상태**이므로 씬 전환 시에도 데이터 유지
2. **ItemTable의 `Inventory` 필드가 `true`**인 아이템만 인벤토리에 표시
3. **Material 타입**만 인벤토리에 표시 (다른 타입 추가 가능)
4. **ItemTable CSV에 Icon_Path와 Description 필드 필수**
5. **Addressables에 아이콘 스프라이트 등록 필요**

### ItemTable.csv 예시

```csv
Item_ID,Item_Name,Item_Type,Item_Grade,Use_Type,Inventory,Max_Count,Icon_Path,Description
102113,일반 마력의 종이,2,1,1,TRUE,999,ItemIcon_102113,"마력이 깃든 종이,\n사서들의 책갈피\n제작 시 사용한다."
102118,고급 마력의 종이,2,2,1,TRUE,999,ItemIcon_102118,"강력한 마력이 깃든\n고급 종이입니다."
```

**필드 설명:**
- `Item_Type`: 2 = Material (재료)
- `Item_Grade`: 1=Common, 2=Rare, 3=Unique, 4=Legendary, 5=Mythic
- `Use_Type`: 1=BookmarkCraft, 2=UserLevelUp, 3=ProductPurchase
- `Inventory`: TRUE = 인벤토리에 표시
- `Icon_Path`: Addressable 키 (예: ItemIcon_102113)
- `Description`: 아이템 설명 (\n으로 줄바꿈 가능)

---

## 다음 단계

- [ ] **ItemTable.csv에 Icon_Path와 Description 필드 추가**
- [ ] **아이템 아이콘 스프라이트를 Addressables에 등록**
  - Window > Asset Management > Addressables > Groups
  - 스프라이트를 그룹에 추가하고 Address 설정 (예: ItemIcon_102113)
- [ ] 로비 씬에 인벤토리 버튼 추가
- [ ] 인벤토리 씬 생성 및 InventoryController 설정
- [ ] InventoryItemSlot 프리팹 제작
- [ ] 씬 전환 시 로딩 화면 구현 (선택)

---

## 문의 및 수정

코드 수정이 필요하거나 기능 추가가 필요한 경우 해당 파일을 직접 수정하시면 됩니다.
각 클래스는 독립적으로 동작하도록 설계되어 유지보수가 쉽습니다.
