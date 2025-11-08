# Unity 스프라이트 리깅 애니메이션 구현 가이드

## 개요
이 가이드는 `Resources/Image/Charactor/Human1` 폴더의 분리된 스프라이트들을 사용하여 Unity에서 리깅 애니메이션을 구현하는 방법을 설명합니다.

## 필요한 Unity 패키지

### 1. 2D Animation 패키지 설치
1. **Window > Package Manager** 열기
2. 상단 드롭다운에서 **Unity Registry** 선택
3. 검색창에 "2D Animation" 입력
4. **2D Animation** 패키지 설치 (버전 8.0.0 이상 권장)
5. **2D PSD Importer** 패키지도 함께 설치 (선택사항)

## 작업 단계

### 단계 1: 스프라이트 Import 설정 확인

각 스프라이트 파일의 Import 설정을 확인하고 필요시 수정:

1. **Human1_body.png** 선택
2. Inspector에서 다음 설정 확인:
   - **Texture Type**: Sprite (2D and UI)
   - **Sprite Mode**: Single
   - **Pixels Per Unit**: 100 (프로젝트에 맞게 조정)
   - **Filter Mode**: Point (no filter) - 픽셀 아트용
   - **Compression**: None (픽셀 아트용)

3. 나머지 스프라이트들도 동일하게 설정:
   - Human1_head.png
   - Human1_hair.png
   - Human1_armfront.png / Human1_armback.png
   - Human1_legfront.png / Human1_legback.png
   - Human1_bodyback.png

### 단계 2: 캐릭터 프리팹 구조 생성

#### 2-1. 빈 GameObject 생성
1. Hierarchy에서 우클릭 > **Create Empty**
2. 이름을 "Human1_Character"로 변경
3. Position: (0, 0, 0)

#### 2-2. 각 부위별 GameObject 생성
각 스프라이트를 자식으로 추가:

**구조 예시:**
```
Human1_Character (GameObject)
├── Body (SpriteRenderer) - Human1_body.png
├── BodyBack (SpriteRenderer) - Human1_bodyback.png
├── Head (SpriteRenderer) - Human1_head.png
├── HeadBack (SpriteRenderer) - Human1_headback.png
├── Hair (SpriteRenderer) - Human1_hair.png
├── HairBack (SpriteRenderer) - Human1_hairback.png
├── ArmFront_L (SpriteRenderer) - Human1_armfront.png
├── ArmBack_L (SpriteRenderer) - Human1_armback.png
├── ArmFront_R (SpriteRenderer) - Human1_armfront.png
├── ArmBack_R (SpriteRenderer) - Human1_armback.png
├── LegFront_L (SpriteRenderer) - Human1_legfront.png
├── LegBack_L (SpriteRenderer) - Human1_legback.png
├── LegFront_R (SpriteRenderer) - Human1_legfront.png
└── LegBack_R (SpriteRenderer) - Human1_legback.png
```

**생성 방법:**
1. Hierarchy에서 Human1_Character 선택
2. 우클릭 > **2D Object > Sprites > Square** (또는 빈 GameObject)
3. 이름을 "Body"로 변경
4. Inspector에서 **Add Component > Sprite Renderer**
5. Sprite Renderer의 **Sprite** 필드에 Human1_body.png 드래그
6. **Sorting Layer** 설정 (예: "Character")
7. **Order in Layer** 설정:
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

### 단계 3: Bone 구조 생성 (2D Animation 사용 시)

#### 3-1. Bone 생성
1. **Window > 2D > Animation** 열기
2. Human1_Character 선택
3. **Create > Bone** 선택
4. Scene 뷰에서 뼈대 구조 생성:

**Bone 구조 예시:**
```
Root (0, 0)
├── Body_Bone (0, 0.5)
│   ├── Head_Bone (0, 1.2)
│   │   └── Hair_Bone (0, 1.4)
│   ├── Arm_L_Bone (-0.3, 0.8)
│   │   └── Hand_L_Bone (-0.5, 0.8)
│   └── Arm_R_Bone (0.3, 0.8)
│       └── Hand_R_Bone (0.5, 0.8)
└── Leg_Bone (0, -0.3)
    ├── Leg_L_Bone (-0.2, -0.8)
    └── Leg_R_Bone (0.2, -0.8)
```

#### 3-2. Sprite Skin 설정
1. 각 부위 GameObject에 **Sprite Skin** 컴포넌트 추가
2. **Root Bone** 필드에 Root Bone 할당
3. **Bones** 배열에 해당하는 Bone들 할당
4. **Auto Rebind** 버튼 클릭하여 자동 바인딩

### 단계 4: 애니메이션 클립 생성

#### 4-1. Animator Controller 생성
1. **Assets** 폴더에서 우클릭 > **Create > Animator Controller**
2. 이름: "Human1_Controller"
3. Human1_Character에 **Animator** 컴포넌트 추가
4. **Controller** 필드에 Human1_Controller 할당

#### 4-2. Idle 애니메이션 생성
1. **Window > Animation > Animation** 열기
2. Human1_Character 선택
3. **Create New Clip** 클릭
4. 이름: "Idle"
5. 키프레임 추가:
   - **0초**: 기본 포즈
   - **0.5초**: 약간의 호흡 (Body Y: +0.02)
   - **1초**: 기본 포즈로 복귀
6. **Loop** 체크

#### 4-3. Walk 애니메이션 생성
1. 새 클립 생성: "Walk"
2. 키프레임 추가:
   - **0초**: 기본 포즈
   - **0.25초**: 왼쪽 다리 앞, 오른쪽 팔 앞
   - **0.5초**: 반대편
   - **0.75초**: 기본 포즈
   - **1초**: 반복
3. **Loop** 체크

### 단계 5: 애니메이션 전환 설정

1. Animator Controller 열기
2. **Parameters** 탭에서:
   - **Speed** (Float) 추가
   - **IsWalking** (Bool) 추가
3. **States** 간 전환 설정:
   - Idle → Walk: 조건 "IsWalking = true"
   - Walk → Idle: 조건 "IsWalking = false"

### 단계 6: 스크립트로 애니메이션 제어

새 스크립트 생성: `Human1AnimationController.cs`

```csharp
using UnityEngine;

public class Human1AnimationController : MonoBehaviour
{
    private Animator animator;
    
    void Start()
    {
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        // 이동 입력 감지 (예시)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        bool isMoving = Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f;
        
        if (animator != null)
        {
            animator.SetBool("IsWalking", isMoving);
            animator.SetFloat("Speed", Mathf.Abs(horizontal) + Mathf.Abs(vertical));
        }
    }
}
```

## 대안: Transform 기반 리깅 (간단한 방법)

2D Animation 패키지 없이도 Transform을 사용한 리깅 가능:

1. 각 부위를 GameObject로 생성 (단계 2 참고)
2. 부모-자식 관계 설정
3. Animation 창에서 Transform의 Position, Rotation, Scale 애니메이션
4. Bone 대신 Transform 계층 구조 사용

**장점:**
- 추가 패키지 불필요
- 설정이 간단
- 성능이 좋음

**단점:**
- 스프라이트 왜곡 불가 (회전/이동만 가능)
- Bone 기반 리깅보다 덜 유연

## 팁

1. **Pivot Point 설정**: 각 스프라이트의 Pivot을 회전 중심에 맞게 설정
2. **Sorting Order**: 앞/뒤 레이어를 정확히 설정
3. **프리팹화**: 완성된 캐릭터를 프리팹으로 저장
4. **애니메이션 이벤트**: 특정 프레임에 이벤트 추가 가능

## 문제 해결

- **스프라이트가 보이지 않음**: Sorting Layer와 Order in Layer 확인
- **Bone이 작동하지 않음**: Sprite Skin의 Root Bone 할당 확인
- **애니메이션이 재생되지 않음**: Animator Controller 할당 확인

