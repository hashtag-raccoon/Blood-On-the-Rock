## 핵심 목표
이 저장소는 Unity(2022.3.62f2) 기반 게임 프로젝트입니다. AI 코딩 에이전트는 아래 핵심 패턴과 통합 지점을 빠르게 이해하면 생산적으로 작업할 수 있습니다.

## 환경(중요)
- Unity Editor: 2022.3.62f2 (ProjectSettings/ProjectVersion.txt 참고)
- Scripting: C# (Assembly-CSharp.csproj -> TargetFramework netstandard2.1)

## 아키텍처(요약)
- 규칙적 디렉터리: `Assets/Scripts/{Merge,Raccoon,Hyunjae,Yoon}` — 팀/기능별 네임스페이스 구분이 암묵적으로 존재.
- 데이터 소유자: MonoBehaviour 싱글턴 매니저들이 런타임 데이터와 상태를 관리함. 예: `Assets/Scripts/Merge/Manager/DataManager.cs`, `Assets/Scripts/Merge/Datable/ItemDatabase.cs`.
- 리소스 정의: ScriptableObject로 레시피/데이터 정의 (예: `Assets/Scripts/Raccoon/Class/CocktailScriptable.cs`).
- 인스펙터 직렬화: 커스텀 제네릭 직렬화기 사용 (`Assets/Scripts/Yoon/SerializableDictionary.cs`) — 딕셔너리 데이터를 인스펙터에 노출할 때 이 패턴을 따르세요.

## 코드/컨벤션 포인트 (작업 시 참고)
- 싱글턴 패턴: 여러 Manager 클래스가 `public static instance` 패턴을 사용합니다. (Awake에서 instance 설정 및 DontDestroyOnLoad)
- ScriptableObject: 리소스(레시피, 정적 데이터)는 CreateAssetMenu로 정의됩니다. 새 데이터 타입을 추가할 때 같은 방식으로 만드세요.
- 임시 파일: `Assets/Scripts/Raccoon/(임시파일)`처럼 임시 폴더가 존재합니다. 무심코 대규모 리팩터링 시 이 폴더를 건드리지 마세요.
- 외부 패키지: TextMesh Pro 예제/스크립트가 포함되어 있습니다. UI 관련 코드는 TMP를 중심으로 작성되어 있습니다.

## 빌드·실행·디버깅(발견 가능한 방식)
- 로컬 개발: 이 프로젝트는 Unity Editor에서 열어 플레이/디버깅합니다. (권장: 에디터 버전 2022.3.62f2)
- 에디터 외부 빌드(참고): Unity CLI를 사용해 빌드할 수 있지만, 로컬에서 씬/에셋 의존성 검증을 먼저 권장합니다.

## 변경 시 안전 수칙
- Library, Temp, ProjectSettings를 직접 편집하거나 커밋하지 마세요.
- 에셋 GUID/메타 변경은 씬/프리팹 참조 끊김을 야기할 수 있으니 신중.

## 예시 작업 요청에 대한 가이드라인(에이전트용)
- 새로운 데이터 타입 추가: ScriptableObject로 만들고, 에디터에서 샘플 에셋을 추가해 인스펙터 동작을 검증하세요. 관련 파일: `Assets/Scripts/Raccoon/Class/*`.
- 런타임 로직 변경: Manager 싱글턴(`DataManager`, `ItemDatabase`)의 `instance` 사용과 `DontDestroyOnLoad` 동작을 고려해 변경하세요.
- 인스펙터 직렬화 문제: 복잡한 딕셔너리는 `SerializableDictionary<TKey,TValue>`를 사용해 검사하세요 (`Assets/Scripts/Yoon/SerializableDictionary.cs`).

## 참고 파일(핵심 예시)
- `ProjectSettings/ProjectVersion.txt` — 권장 에디터 버전
- `Assembly-CSharp.csproj` — C# 타겟/네임스페이스/컴파일 목록
- `Assets/Scripts/Yoon/SerializableDictionary.cs` — 인스펙터용 딕셔너리 직렬화
- `Assets/Scripts/Merge/Manager/DataManager.cs` — 싱글턴 매니저 패턴 예시
- `Assets/Scripts/Raccoon/Class/CocktailScriptable.cs` — ScriptableObject 예시

## 요청할 추가 정보 (피드백 요청)
- CI 빌드/테스트 명령이 있다면 알려주세요(현재 저장소에서 감지되지 않음).
- 팀 규약(브랜칭/풀리퀘/릴리즈 프로세스)이 있다면 간단히 공유해 주세요.

간단한 검토를 부탁드립니다 — 누락되거나 모호한 점을 알려주시면 바로 반영하겠습니다.
