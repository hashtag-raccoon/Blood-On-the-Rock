# 인테리어 배치 UI 설정 가이드

## 📋 목차
1. [BuildInteriorButton Prefab 만들기](#1-buildinteriorbutton-prefab-만들기)
2. [InteriorScrollUI GameObject 만들기](#2-interiorscrollui-gameobject-만들기)
3. [DataManager 설정](#3-datamanager-설정)
4. [인테리어 배치 버튼 추가](#4-인테리어-배치-버튼-추가)

---

## 1. BuildInteriorButton Prefab 만들기

### 1-1. 기존 Prefab 복사하기 (가장 쉬운 방법)

1. **Project 창**에서 `Assets/Prefab/UI/BuildBuildingButton.prefab` 찾기
2. **우클릭** → **Duplicate** (또는 Ctrl+D)
3. 이름을 `BuildInteriorButton`으로 변경

### 1-2. Prefab 수정하기

1. **BuildInteriorButton.prefab** 더블클릭하여 Prefab 편집 모드 진입

2. **루트 GameObject 선택** (BuildBuildingButton)
   - Inspector에서 **Add Component** 클릭
   - `BuildInteriorButtonUI` 스크립트 추가

3. **Inspector에서 BuildInteriorButtonUI 컴포넌트 설정**
   ```
   UI 설정:
   ├─ Interior Icon Image: BuildingIcon (Image 컴포넌트)
   ├─ Interior Name Text: BuildingName (TextMeshProUGUI)
   ├─ Interior Amount Text: BuildingAmount (TextMeshProUGUI) - 선택사항
   ├─ Interior Price Money Text: BuildingPriceMoney (TextMeshProUGUI)
   ├─ Interior Price Wood Text: BuildingPriceWood (TextMeshProUGUI)
   └─ Buy Button: BuyButton (Button 컴포넌트)
   ```

4. **각 UI 요소 찾는 방법:**
   - Hierarchy에서 자식 GameObject들을 확인
   - Inspector에서 드래그 앤 드롭으로 할당
   - 또는 각 필드 옆의 원형 아이콘 클릭하여 선택

### 1-3. Prefab 저장
- Prefab 편집 모드에서 나가면 자동 저장
- 또는 **Ctrl+S**로 저장

---

## 2. InteriorScrollUI GameObject 만들기

### 2-1. 기존 BuildScrollUI 복사하기 (가장 쉬운 방법)

1. **Hierarchy**에서 `BuildScrollUI` GameObject 찾기
2. **우클릭** → **Duplicate** (또는 Ctrl+D)
3. 이름을 `InteriorScrollUI`로 변경

### 2-2. InteriorScrollUI 수정하기

1. **InteriorScrollUI GameObject 선택**

2. **기존 컴포넌트 제거:**
   - Inspector에서 `BuildScrollUI` 컴포넌트 찾기
   - 우클릭 → **Remove Component**

3. **새 컴포넌트 추가:**
   - **Add Component** 클릭
   - `InteriorScrollUI` 스크립트 추가

4. **Inspector에서 InteriorScrollUI 컴포넌트 설정:**

   #### 스크롤 세팅:
   ```
   Scroll UI: InteriorScrollUI GameObject 자체 (또는 스크롤 패널)
   Scroll Rect: ScrollRect 컴포넌트가 있는 GameObject
   Content: ScrollRect의 Content Transform
   Item Prefab: 위에서 만든 BuildInteriorButton Prefab
   ```

   #### 레이아웃 설정:
   ```
   Item Width: 100 (또는 원하는 크기)
   Item Height: 100 (또는 원하는 크기)
   Spacing: 10 (아이템 간 간격)
   Padding: 10 (좌우상하 여백)
   ```

   #### 버튼 할당:
   ```
   Open Button: 스크롤 UI를 여는 버튼
   Close Button: 스크롤 UI를 닫는 버튼
   ```

   #### IslandManager 할당/연결:
   ```
   Island Manager: Hierarchy에서 IslandManager GameObject 찾아서 할당
   ```

   #### UI 애니메이션 설정:
   ```
   Duration: 1 (애니메이션 지속 시간, 초 단위)
   ```

### 2-3. ScrollRect 구조 확인

InteriorScrollUI의 구조는 다음과 같아야 합니다:
```
InteriorScrollUI (GameObject)
├─ ScrollRect 컴포넌트
└─ Content (GameObject)
   └─ (여기에 BuildInteriorButton들이 자동 생성됨)
```

**Content GameObject 확인:**
1. InteriorScrollUI의 자식 GameObject 중 `Content` 찾기
2. Content에 **Horizontal Layout Group** 컴포넌트가 있는지 확인
3. 없으면 **Add Component** → **Layout** → **Horizontal Layout Group** 추가

---

## 3. DataManager 설정

### 3-1. InteriorData ScriptableObject 생성

1. **Project 창**에서 우클릭
2. **Create** → **Interior** → **InteriorData**
3. 이름을 적절하게 변경 (예: `InteriorData_Chair`)

4. **Inspector에서 설정:**
   ```
   인테리어 기본 정보:
   ├─ Interior Id: 1 (고유 ID)
   ├─ Interior Name: "의자" (인테리어 이름)
   └─ Interior Type: "Furniture" (타입)
   
   비용 및 배치:
   ├─ Purchase Cost Gold: 100 (골드 비용)
   ├─ Purchase Cost Wood: 50 (목재 비용)
   ├─ Tile Size: X=1, Y=1 (차지하는 타일 크기)
   └─ Marker Position Offset: -3 (마커 오프셋, 건물과 동일하게)
   
   스프라이트:
   ├─ Icon: 아이콘 스프라이트 (작은 이미지)
   └─ Interior Sprite: 배치용 스프라이트 (큰 이미지)
   ```

5. **여러 개의 InteriorData 생성** (의자, 테이블, 장식품 등)

### 3-2. DataManager에 InteriorDatas 할당

1. **Hierarchy**에서 `DataManager` GameObject 선택

2. **Inspector**에서 `인테리어 데이터` 섹션 찾기

3. **Interior Datas** 리스트:
   - **Size** 필드에 생성한 InteriorData 개수 입력
   - 각 **Element**에 InteriorData 에셋 드래그 앤 드롭

---

## 4. 인테리어 배치 버튼 추가

### 4-1. 인테리어 스크롤 UI 열기 버튼 만들기

1. **Canvas** 하위에 새 **Button** 생성
   - Hierarchy에서 Canvas 우클릭 → **UI** → **Button - TextMeshPro**
   - 이름을 `InteriorScrollOpenButton`으로 변경

2. **버튼에 스크립트 추가 (선택사항):**
   ```csharp
   // 간단한 방법: Inspector에서 OnClick 이벤트에 InteriorScrollUI GameObject를 드래그
   // 그리고 InteriorScrollUI → OnOpenButtonClicked() 선택
   ```

3. **또는 간단한 스크립트 만들기:**
   - 새 C# 스크립트 생성: `InteriorScrollOpenButton.cs`
   ```csharp
   using UnityEngine;
   using UnityEngine.UI;
   
   public class InteriorScrollOpenButton : MonoBehaviour
   {
       [SerializeField] private InteriorScrollUI interiorScrollUI;
       
       private void Start()
       {
           Button button = GetComponent<Button>();
           if (button != null && interiorScrollUI != null)
           {
               button.onClick.AddListener(() => 
               {
                   interiorScrollUI.gameObject.SetActive(true);
               });
           }
       }
   }
   ```

### 4-2. 테스트

1. **Play 모드** 실행
2. **인테리어 스크롤 열기 버튼** 클릭
3. **인테리어 목록**이 표시되는지 확인
4. **인테리어 버튼 클릭** → 자원 차감 및 배치 모드 진입 확인

---

## 🔧 문제 해결

### Q: InteriorScrollUI에 아이템이 표시되지 않아요
**A:** 
1. DataManager의 InteriorDatas 리스트에 InteriorData가 할당되었는지 확인
2. InteriorScrollUI의 Item Prefab이 BuildInteriorButton Prefab으로 설정되었는지 확인
3. Content GameObject에 Horizontal Layout Group이 있는지 확인

### Q: 버튼을 클릭해도 아무 일도 일어나지 않아요
**A:**
1. BuildInteriorButtonUI 컴포넌트의 Buy Button이 올바르게 할당되었는지 확인
2. InteriorScrollUI의 IslandManager가 할당되었는지 확인
3. Console 창에서 에러 메시지 확인

### Q: 자원이 차감되지 않아요
**A:**
1. DataManager의 ResourceData가 올바르게 설정되었는지 확인
2. BuildInteriorButtonUI의 Awake()에서 MoneyData와 WoodData가 제대로 로드되는지 확인

---

## 📝 체크리스트

- [ ] BuildInteriorButton Prefab 생성 및 BuildInteriorButtonUI 컴포넌트 추가
- [ ] InteriorScrollUI GameObject 생성 및 InteriorScrollUI 컴포넌트 추가
- [ ] InteriorData ScriptableObject 여러 개 생성
- [ ] DataManager의 InteriorDatas 리스트에 InteriorData 할당
- [ ] 인테리어 스크롤 UI 열기 버튼 생성
- [ ] Play 모드에서 테스트



