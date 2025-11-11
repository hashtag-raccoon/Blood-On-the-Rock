# Unity에서 Human1 리깅 애니메이션 구현 - 단계별 가이드

## 준비 단계

### 1. 필요한 패키지 설치
1. **Window > Package Manager** 열기
2. 상단에서 **Unity Registry** 선택
3. 검색: "2D Animation"
4. **Install** 클릭
5. (선택) "2D PSD Importer"도 설치

### 2. Sorting Layer 생성
1. **Edit > Project Settings > Tags and Layers** 열기
2. **Sorting Layers** 섹션에서:
   - **+** 버튼 클릭
   - 이름: "Character" 입력
   - Order: 0 (또는 원하는 값)

## 방법 1: Transform 기반 리깅 (간단한 방법)

### 단계 1: 캐릭터 루트 생성
1. Hierarchy에서 **Create Empty**
2. 이름: "Human1_Character"
3. Position: (0, 0, 0)

### 단계 2: 각 부위 GameObject 생성

#### Body 생성
1. Human1_Character 선택
2. 우클릭 > **Create Empty**
3. 이름: "Body"
4. **Add Component > Sprite Renderer**
5. Sprite 필드에 `Human1_body.png` 드래그
6. Sorting Layer: "Character"
7. Order in Layer: 2

#### BodyBack 생성
1. 위와 동일하게 생성
2. 이름: "BodyBack"
3. Sprite: `Human1_bodyback.png`
4. Order in Layer: 0

#### Head 계층 구조
1. Human1_Character 선택
2. 우클릭 > **Create Empty**
3. 이름: "HeadParent"
4. Position: (0, 0.5, 0) - Body 위에 위치

**HeadParent의 자식으로:**
- **HeadBack**: `Human1_headback.png`, Order: 3
- **Head**: `Human1_head.png`, Order: 6
- **HairBack**: `Human1_hairback.png`, Order: 4
- **Hair**: `Human1_hair.png`, Order: 7

#### 팔 생성
- **ArmBack_L**: `Human1_armback.png`, Position: (-0.3, 0.3, 0), Order: 5
- **ArmBack_R**: `Human1_armback.png`, Position: (0.3, 0.3, 0), Order: 5
- **ArmFront_L**: `Human1_armfront.png`, Position: (-0.3, 0.3, 0), Order: 8
- **ArmFront_R**: `Human1_armfront.png`, Position: (0.3, 0.3, 0), Order: 8

#### 다리 생성
- **LegBack_L**: `Human1_legback.png`, Position: (-0.15, -0.5, 0), Order: 1
- **LegBack_R**: `Human1_legback.png`, Position: (0.15, -0.5, 0), Order: 1
- **LegFront_L**: `Human1_legfront.png`, Position: (-0.15, -0.5, 0), Order: 9
- **LegFront_R**: `Human1_legfront.png`, Position: (0.15, -0.5, 0), Order: 9

### 단계 3: Animator Controller 생성
1. **Assets** 폴더에서 우클릭 > **Create > Animator Controller**
2. 이름: "Human1_Controller"
3. 더블클릭하여 열기

### 단계 4: Animation State 생성
1. Animator 창에서:
   - 기본 "Entry" → "Any State" 보임
2. 빈 공간 우클릭 > **Create State > Empty**
3. 이름: "Idle"
4. 다시 우클릭 > **Create State > Empty**
5. 이름: "Walk"

### 단계 5: Idle 애니메이션 생성
1. **Window > Animation > Animation** 열기
2. Human1_Character 선택
3. **Create** 버튼 클릭
4. 이름: "Idle"
5. 저장 위치 선택

**키프레임 추가:**
1. **0초**: 모든 부위 기본 위치
2. **0.5초**: 
   - Body Y: +0.02 (호흡 효과)
   - HeadParent Y: +0.01
3. **1초**: 0초와 동일 (루프)
4. **Loop** 체크박스 활성화

### 단계 6: Walk 애니메이션 생성
1. Animation 창에서 **Create New Clip**
2. 이름: "Walk"
3. **Loop** 활성화

**키프레임 추가:**
- **0초**: 기본 포즈
- **0.25초**: 
  - LegFront_L Y: -0.1 (앞으로)
  - LegBack_R Y: +0.1 (뒤로)
  - ArmFront_L Y: +0.1
  - ArmBack_R Y: -0.1
- **0.5초**: 반대편
- **0.75초**: 기본 포즈
- **1초**: 0초와 동일

### 단계 7: Animator 전환 설정
1. Animator Controller 창으로 돌아가기
2. **Parameters** 탭:
   - **+** > **Bool**: "IsWalking"
3. **Idle → Walk** 전환:
   - Idle 선택 > 우클릭 > **Make Transition** > Walk 클릭
   - 전환선 선택
   - Inspector에서 **Conditions**:
     - IsWalking = true
4. **Walk → Idle** 전환:
   - Walk → Idle 전환선 생성
   - Conditions: IsWalking = false

### 단계 8: 스크립트 연결
1. Human1_Character에 **Animator** 컴포넌트 추가
2. **Controller**: Human1_Controller 할당
3. **Human1AnimationController** 스크립트 추가
4. **Animator** 필드에 Animator 컴포넌트 드래그

### 단계 9: 테스트
1. Play 모드 실행
2. 마우스 우클릭으로 이동 테스트
3. 애니메이션 전환 확인

## 방법 2: Bone 기반 리깅 (고급 방법)

### 단계 1-2: 방법 1과 동일

### 단계 3: Bone 생성
1. **Window > 2D > Animation** 열기
2. Human1_Character 선택
3. 상단 메뉴: **Create > Bone**
4. Scene 뷰에서 뼈대 그리기:
   - **Root Bone**: (0, 0) - 캐릭터 중심
   - **Body Bone**: Root에서 위로
   - **Head Bone**: Body에서 위로
   - **Arm_L Bone**: Body에서 왼쪽으로
   - **Arm_R Bone**: Body에서 오른쪽으로
   - **Leg Bone**: Root에서 아래로
   - **Leg_L/R Bone**: Leg에서 양쪽으로

### 단계 4: Sprite Skin 설정
1. 각 부위 GameObject 선택
2. **Add Component > Sprite Skin**
3. **Root Bone** 필드에 Root Bone 할당
4. **Bones** 배열에 해당 Bone들 할당
5. **Auto Rebind** 클릭

### 단계 5-9: 방법 1과 동일 (애니메이션은 Bone Transform으로)

## 자동화 도구 사용

### CharacterRigSetupHelper 사용
1. **Tools > Character Rig Setup Helper** 열기
2. **캐릭터 루트**에 Human1_Character 할당
3. 각 스프라이트 필드에 해당 스프라이트 드래그
4. **캐릭터 구조 생성** 버튼 클릭
5. **Sorting Order 자동 설정** 버튼 클릭

## 팁

1. **Pivot 조정**: 각 스프라이트의 Pivot을 회전 중심에 맞게 설정
2. **프리팹 저장**: 완성 후 Prefab으로 저장
3. **애니메이션 이벤트**: 특정 프레임에 함수 호출 가능
4. **Blend Tree**: 여러 애니메이션을 부드럽게 전환

## 문제 해결

### 스프라이트가 겹쳐 보임
- Sorting Order 확인
- Sorting Layer 확인

### 애니메이션이 재생되지 않음
- Animator Controller 할당 확인
- Animator 컴포넌트의 Controller 필드 확인

### Bone이 작동하지 않음
- Sprite Skin의 Root Bone 할당 확인
- Bone 계층 구조 확인

