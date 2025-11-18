# 다중 캐릭터 리깅 설정 가이드

이 가이드는 `MultiCharacterRigSetup` 스크립트를 사용하여 여러 캐릭터를 자동으로 생성하고 리깅 설정하는 방법을 설명합니다.

## 지원 캐릭터

- Human2
- Human3
- Oak1
- Oak2
- Oak3
- Player1
- Vampire1
- Vampire2
- Vampire3

## 사용 방법

### 1단계: 캐릭터 생성

1. Unity 에디터에서 **Tools > Multi Character Rig Setup** 메뉴를 엽니다.
2. 생성할 캐릭터를 선택합니다 (체크박스로 선택/해제 가능).
3. **"모든 캐릭터 생성"** 또는 **"선택된 캐릭터만 생성"** 버튼을 클릭합니다.
4. 각 캐릭터가 Hierarchy에 생성됩니다.

### 2단계: 스켈레톤 생성

각 캐릭터에 대해 다음 단계를 수행합니다:

1. Hierarchy에서 생성된 캐릭터를 선택합니다 (예: `Human2`).
2. **Window > 2D > Animation** 창을 엽니다.
3. 상단 메뉴에서 **Create > Bone**을 선택합니다.
4. Scene 뷰에서 뼈대 구조를 그립니다:

**권장 Bone 구조:**
```
Root (0, 0) - 캐릭터 중심
├── Body_Bone (0, 0.5) - 몸통
│   ├── Head_Bone (0, 1.2) - 머리
│   │   └── Hair_Bone (0, 1.4) - 머리카락
│   ├── Arm_L_Bone (-0.3, 0.8) - 왼쪽 팔
│   │   └── Hand_L_Bone (-0.5, 0.8) - 왼쪽 손
│   └── Arm_R_Bone (0.3, 0.8) - 오른쪽 팔
│       └── Hand_R_Bone (0.5, 0.8) - 오른쪽 손
└── Leg_Bone (0, -0.3) - 다리
    ├── Leg_L_Bone (-0.2, -0.8) - 왼쪽 다리
    └── Leg_R_Bone (0.2, -0.8) - 오른쪽 다리
```

### 3단계: 스프라이트 스킨 설정

각 부위 GameObject에 Sprite Skin 컴포넌트를 추가하고 Bone을 연결합니다:

1. 캐릭터의 각 부위 GameObject를 선택합니다 (예: `Body`, `Head`, `ArmFront_L` 등).
2. Inspector에서 **Add Component > Sprite Skin**을 추가합니다.
3. **Root Bone** 필드에 Root Bone을 할당합니다.
4. **Bones** 배열에 해당하는 Bone들을 할당합니다:
   - Body, BodyBack → Body_Bone
   - Head, HeadBack → Head_Bone
   - Hair, HairBack → Hair_Bone
   - ArmFront_L, ArmBack_L → Arm_L_Bone, Hand_L_Bone
   - ArmFront_R, ArmBack_R → Arm_R_Bone, Hand_R_Bone
   - LegFront_L, LegBack_L → Leg_L_Bone
   - LegFront_R, LegBack_R → Leg_R_Bone
5. **Auto Rebind** 버튼을 클릭하여 자동 바인딩합니다.

### 4단계: 애니메이션 생성

1. **Window > Animation > Animation** 창을 엽니다.
2. 캐릭터를 선택합니다.
3. **Create** 버튼을 클릭하여 새 애니메이션 클립을 만듭니다.
4. 애니메이션 이름을 입력합니다 (예: `{CharacterName}_IDLE`, `{CharacterName}_Walk`).
5. 키프레임을 추가하여 애니메이션을 만듭니다.

**IDLE 애니메이션 예시:**
- 0초: 기본 포즈
- 0.5초: Body Y: +0.02 (호흡 효과)
- 1초: 기본 포즈로 복귀
- Loop 활성화

**Walk 애니메이션 예시:**
- 0초: 기본 포즈
- 0.25초: 왼쪽 다리 앞, 오른쪽 팔 앞
- 0.5초: 반대편
- 0.75초: 기본 포즈
- 1초: 반복
- Loop 활성화

### 5단계: Animator Controller 설정

1. **Assets/Animation** 폴더에서 생성된 `{CharacterName}.controller` 파일을 엽니다.
2. 생성한 애니메이션 클립들을 State로 추가합니다.
3. 전환(Transition)을 설정합니다:
   - Idle → Walk: 조건 "IsWalking = true"
   - Walk → Idle: 조건 "IsWalking = false"
4. Parameters에 다음을 추가:
   - `IsWalking` (Bool)
   - `Speed` (Float) - 선택사항

## 참고사항

- 각 캐릭터는 `Resources/Image/Charactor/{CharacterName}` 폴더에서 스프라이트를 자동으로 로드합니다.
- Oak1의 경우 `Oak1_bodypng.png` 파일을 사용합니다.
- Sorting Layer는 "Character"로 설정됩니다.
- Sorting Order는 자동으로 설정됩니다:
  - BodyBack: 0
  - LegBack: 1
  - Body: 2
  - HeadBack: 3
  - HairBack: 4
  - ArmBack: 5
  - Head: 6
  - Hair: 7
  - ArmFront: 8
  - LegFront: 9

## 문제 해결

### 스프라이트를 찾을 수 없음
- `Resources/Image/Charactor/{CharacterName}` 폴더에 올바른 이름의 스프라이트 파일이 있는지 확인하세요.
- Oak1의 경우 `Oak1_bodypng.png` 파일이 있어야 합니다.

### Bone이 보이지 않음
- 2D Animation 패키지가 설치되어 있는지 확인하세요.
- Window > Package Manager에서 "2D Animation" 패키지를 설치하세요.

### Sprite Skin이 작동하지 않음
- Root Bone이 올바르게 할당되었는지 확인하세요.
- Bones 배열에 해당하는 Bone들이 모두 할당되었는지 확인하세요.
- Auto Rebind 버튼을 클릭했는지 확인하세요.

