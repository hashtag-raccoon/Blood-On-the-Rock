using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// 아르바이트 관리 매니저
/// 후보자 생성, (고용된 인원만)필터링 담당
/// </summary>
public class ArbeitManager : MonoBehaviour
{
    private static ArbeitManager _instance;
    public ArbeitSpriteReference arbeitSpriteReference; // 프리팹 레퍼런스 할당

    [Header("알바 배치 설정")]
    [Tooltip("바에서 사용할 알바의 최대 인원")]
    public int maxDeployedArbeiters = 3;
    [Tooltip("현재 배치된 알바 리스트")]
    public List<GameObject> deployedArbeiters = new List<GameObject>();
    [Space(2)]
    [Header("알바 생성 설정")]
    [Tooltip("알바가 대기할 ArbeitPoint")]
    [SerializeField] private ArbeitPoint arbeitPoint;
    [Tooltip("알바들이 사용할 Pathfinder")]
    [SerializeField] private IsometricPathfinder pathfinder;

    [Header("알바 클릭 감지 설정")]
    [Tooltip("클릭 전용 BoxCollider2D 프리팹 (크기/오프셋을 복사해 각 알바에 부착)")]
    [SerializeField] private BoxCollider2D clickColliderTemplate;
    [Tooltip("클릭 전용 레이어 마스크 (비워두면 레이어 이름으로 계산)")]
    [SerializeField] private LayerMask clickLayerMask;
    [Tooltip("클릭 전용 레이어 이름")]
    [SerializeField] private string clickColliderLayerName = "FeetClick";

    public String BarSceneName = "BarScene_Raccoon"; // 바 씬 이름

    public static ArbeitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ArbeitManager>();
            }
            return _instance;
        }
    }

    private void Awake()
    {
        InitializeSingleton();
        GenerateInitialCandidates();

        // 씬 로드 이벤트 등록
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // 씬 로드 이벤트 해제
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void InitializeSingleton()
    {
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // arbeitSpriteReference 확인, 없을 시 디버그 로그 출력
            if (arbeitSpriteReference == null)
            {
                Debug.LogError("[ArbeitManager] arbeitSpriteReference가 할당되지 않았습니다!");
            }
        }
        else
        {
            Debug.LogWarning($"[ArbeitManager] 중복된 인스턴스 제거: {gameObject.name}");
            Destroy(gameObject);
        }
    }
    #region 알바 후보자 관리
    /// <summary>
    /// 구인소에 대기중인 알바생 3명 생성 (알바생 후보 리스트가 비어있을 경우)
    /// </summary>
    private void GenerateInitialCandidates()
    {
        if (ArbeitRepository.Instance.tempCandidateList.Count == 0)
        {
            List<TempNpcData> candidates = ArbeitRepository.Instance.CreateRandomTempCandidates(3);
            ArbeitRepository.Instance.tempCandidateList.AddRange(candidates);
        }
    }

    /// <summary>
    /// 고용되지 않은 후보자 목록 반환
    /// </summary>
    public List<TempNpcData> GetAvailableCandidates()
    {
        var available = ArbeitRepository.Instance.tempCandidateList
            .FindAll(candidate => !candidate.is_hired);
        return available;
    }

    /// <summary>
    /// 후보자 목록 리롤 (고용된 인원 제거 후 재생성)
    /// </summary>
    public void RefreshCandidates()
    {
        ArbeitRepository.Instance.tempCandidateList.Clear();
        GenerateInitialCandidates();
    }
    #endregion

    #region 씬 로드 및 알바 생성
    /// <summary>
    /// 씬이 로드될 때 호출되는 메서드
    /// 바 씬일 경우 알바 생성 로직 실행
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 바 씬인지 확인 (씬 이름으로 판단)
        if (scene.name.Contains(BarSceneName))
        {
            StartCoroutine(InitializeBarScene());
        }
    }

    /// <summary>
    /// 바 씬 초기화
    /// </summary>
    private System.Collections.IEnumerator InitializeBarScene()
    {
        // 씬의 오브젝트들이 완전히 로드될 때까지 대기
        yield return new WaitForSeconds(0.1f);

        // ArbeitPoint와 Pathfinder 찾기
        if (arbeitPoint == null)
        {
            arbeitPoint = FindObjectOfType<ArbeitPoint>();
        }

        if (pathfinder == null)
        {
            pathfinder = FindObjectOfType<IsometricPathfinder>();
        }

        // 배치된 알바가 없다면 알바 배치시킴
        if (deployedArbeiters.Count == 0)
        {
            DeployArbeiters();
        }

        //TODO : 딜레이가 있다보니, 로딩창 필수, 그래서 후에 추가해야함
    }

    /// <summary>
    /// 알바 생성 및 배치
    /// </summary>
    private void DeployArbeiters()
    {
        if (arbeitSpriteReference == null)
        {
            Debug.LogError("[ArbeitManager] arbeitSpriteReference가 할당되지 않았습니다.");
            return;
        }

        if (arbeitPoint == null)
        {
            Debug.LogError("[ArbeitManager] ArbeitPoint를 찾을 수 없습니다.");
            return;
        }

        if (pathfinder == null)
        {
            Debug.LogError("[ArbeitManager] IsometricPathfinder를 찾을 수 없습니다.");
            return;
        }

        // 고용된 NPC 데이터 가져오기
        List<npc> hiredNpcs = ArbeitRepository.Instance.GethiredNpcs();

        // 생성할 알바 수 결정 (최대 maxDeployedArbeiters, 최소 고용된 NPC 수)
        int countToCreate = Mathf.Min(maxDeployedArbeiters, hiredNpcs.Count);

        if (countToCreate == 0)
        {
            Debug.LogWarning("[ArbeitManager] 배치할 알바가 없음 (고용된 NPC가 없음)");
            return;
        }

        // 알바 생성
        for (int i = 0; i < countToCreate; i++)
        {
            // 배치되지 않은 NPC 중에서 선택
            npc npcData = hiredNpcs.Find(n => !n.is_deployed);

            if (npcData == null)
            {
                Debug.LogWarning($"[ArbeitManager] 배치 가능한 NPC를 찾을 수 없음 (현재 {i}명 배치됨)");
                break;
            }

            // 알바 생성
            GameObject arbeiterObj = CreateArbeiter(npcData, i);

            if (arbeiterObj != null)
            {
                deployedArbeiters.Add(arbeiterObj);
                npcData.is_deployed = true;
            }
        }
    }

    /// <summary>
    /// 알바 스폰 메소드
    /// - npcData: 스폰할 알바의 npc 데이터
    /// - waitingPosition: ArbeitPoint에서의 대기 위치 인덱스
    /// - 반환: 생성된 알바 오브젝트
    /// - 알바 오브젝트에 ArbeitController 컴포넌트 추가 및 초기화
    /// - Pathfinder 할당
    /// - 클릭 전용 콜라이더 부착 및 클릭 레이어 전달
    /// - ArbeitPoint 할당 및 대기열 추가
    /// 등등의 기능을 포함
    /// </summary>
    private GameObject CreateArbeiter(npc npcData, int waitingPosition)
    {
        // ArbeitPoint의 대기 위치 계산
        Vector3 spawnPosition = arbeitPoint.CalculateWaitingPosition(waitingPosition);

        // 초상화 스프라이트로 매칭되는 프리팹 찾기
        GameObject prefabToSpawn = GetMatchingPrefab(npcData);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[ArbeitManager] '{npcData.part_timer_name}'의 초상화와 매칭되는 프리팹을 찾을 수 없음");
            return null;
        }

        // 프리팹 인스턴스화
        GameObject arbeiterObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        arbeiterObj.name = $"알바생_{npcData.part_timer_name}";

        // ArbeitController 설정
        ArbeitController controller = arbeiterObj.GetComponent<ArbeitController>();
        if (controller == null)
        {
            controller = arbeiterObj.AddComponent<ArbeitController>();
        }

        // Pathfinder 할당
        controller.pathfinder = pathfinder;

        // NPC 데이터 초기화
        controller.Initialize(npcData);

        // 클릭 전용 콜라이더 부착 및 클릭 레이어 전달
        AttachClickCollider(arbeiterObj, controller);

        // ArbeitPoint 할당 및 대기열 추가
        controller.SetArbeitPoint(arbeitPoint);

        return arbeiterObj;
    }

    /// <summary>
    /// NPC의 prefab_name을 사용하여 매칭되는 프리팹을 찾습니다.
    /// </summary>
    private GameObject GetMatchingPrefab(npc npcData)
    {
        if (string.IsNullOrEmpty(npcData.prefab_name))
        {
            Debug.LogWarning($"[ArbeitManager] '{npcData.part_timer_name}'의 prefab_name이 null이거나 비어있음");
            return null;
        }
        // 종족에 따라 리스트 선택
        List<ArbeitPrefabToSpritePair> pairs = null;
        switch (npcData.race)
        {
            case "Human":
                pairs = arbeitSpriteReference.Human_Pairs;
                break;
            case "Oak":
                pairs = arbeitSpriteReference.Oak_Pairs;
                break;
            case "Vampire":
                pairs = arbeitSpriteReference.Vampire_Pairs;
                break;
            default:
                Debug.LogWarning($"[ArbeitManager] 알 수 없는 종족: {npcData.race}");
                return null;
        }
        // prefab_name으로 매칭되는 프리팹 찾기
        if (pairs != null && pairs.Count > 0)
        {
            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                string pairPrefabName = pair.PairPrefab?.name ?? "null";

                if (pair.PairPrefab != null && pair.PairPrefab.name == npcData.prefab_name)
                {
                    return pair.PairPrefab;
                }
            }
        }

        Debug.LogWarning($"[ArbeitManager] '{npcData.part_timer_name}'의 prefab_name '{npcData.prefab_name}'과 매칭되는 프리팹을 찾을 수 없음 (종족: {npcData.race})");
        return null;
    }
    #region 클릭 콜라이더 및 레이어 설정
    /// <summary>
    /// 클릭 전용 BoxCollider2D를 알바 오브젝트의 자식으로 부착하고, 컨트롤러에 클릭 레이어를 전달
    /// </summary>
    private void AttachClickCollider(GameObject arbeiterObj, ArbeitController controller)
    {
        if (arbeiterObj == null || controller == null) return;

        if (clickColliderTemplate == null)
        {
            Debug.LogWarning("[ArbeitManager] clickColliderTemplate이 할당되지 않아 클릭 콜라이더를 부착하지 않습니다.");
            return;
        }

        GameObject holder = new GameObject("ClickCollider");
        holder.transform.SetParent(arbeiterObj.transform, false);
        holder.layer = ResolveClickLayerIndex(holder.layer);

        BoxCollider2D targetCollider = holder.AddComponent<BoxCollider2D>();
        CopyColliderSettings(clickColliderTemplate, targetCollider);

        // 클릭 레이어 전달 (Raycast에만 사용, 물리 충돌 없음, 참고로 물리 충돌 설정은 프로젝트 설정으로 되어 있음)
        controller.ClickLayerMask = GetClickLayerMask();
    }
    /// <summary>
    /// 클릭한 레이어 마스크를 반환함
    /// </summary>
    private LayerMask GetClickLayerMask()
    {
        if (clickLayerMask != 0) return clickLayerMask;

        int layerIndex = LayerMask.NameToLayer(clickColliderLayerName);
        if (layerIndex >= 0)
        {
            return 1 << layerIndex;
        }

        return 0;
    }
    /// <summary>
    /// 클릭 레이어 인덱스를 반환함, 유효하지 않으면 fallbackLayer 반환
    /// - clickColliderLayerName이 설정되지 않았을 경우 fallbackLayer 반환
    /// - clickColliderLayerName이 유효하지 않은 레이어 이름일 경우 fallbackLayer 반환
    /// - 유효한 레이어 이름일 경우 해당 레이어 인덱스 반환
    /// - 유효하지 않은 레이어 이름일 경우 fallbackLayer 반환
    /// 등등의 기능 포함
    /// </summary>
    private int ResolveClickLayerIndex(int fallbackLayer)
    {
        int layerIndex = LayerMask.NameToLayer(clickColliderLayerName);
        return layerIndex >= 0 ? layerIndex : fallbackLayer;
    }
    /// <summary>
    /// BoxCollider2D 설정 복사
    /// </summary>
    private void CopyColliderSettings(BoxCollider2D source, BoxCollider2D target)
    {
        if (source == null || target == null) return;

        target.offset = source.offset;
        target.size = source.size;
        target.usedByComposite = source.usedByComposite;
        target.usedByEffector = source.usedByEffector;
        target.edgeRadius = source.edgeRadius;
        target.isTrigger = true; // 클릭 감지용, 물리 충돌 방지
        target.enabled = true;
    }
    #endregion

    #region 알바 배치 관리
    /// <summary>
    /// 특정 알바 제거
    /// </summary>
    public void RemoveArbeiter(GameObject arbeiter)
    {
        if (arbeiter == null) return;

        if (deployedArbeiters.Contains(arbeiter))
        {
            deployedArbeiters.Remove(arbeiter);

            // NPC 데이터(알바생)의 배치 상태 업데이트
            ArbeitController controller = arbeiter.GetComponent<ArbeitController>();
            if (controller != null && controller.myNpcData != null)
            {
                controller.myNpcData.is_deployed = false;
            }

            Destroy(arbeiter);
        }
    }
    #endregion

    #region UI 연동 배치 메서드
    /// <summary>
    /// UI에서 알바 배치 확인 시 호출
    /// 새 알바를 추가하거나 기존 알바를 교체합니다.
    /// - npcData: 배치할 알바의 npc 데이터
    /// - slotIndex: 교체할 슬롯 인덱스 (-1이면 새로 추가하고 기존 인덱스면 교체 진행)
    /// - 성공 시 true, 실패 시 false 반환
    /// - 배치 성공 시 npcData.is_deployed = true로 설정
    /// - 배치 실패 시 npcData.is_deployed 상태 변경 없음
    /// - 기존 알바 교체 시 기존 알바 오브젝트 제거 후 새 알바 생성
    /// - 새 알바 추가 시 최대 배치 수 제한 적용
    /// - 기타 예외 상황 처리 포함
    /// 등의 기능이 포함됨
    public bool DeployArbeiterFromUI(npc npcData, int slotIndex = -1)
    {
        if (npcData == null)
        {
            Debug.LogWarning("[ArbeitManager] 배치할 NPC 데이터가 null입니다.");
            return false;
        }

        // 이미 배치된 알바인지 확인
        if (npcData.is_deployed)
        {
            Debug.LogWarning($"[ArbeitManager] '{npcData.part_timer_name}'는 이미 배치되어 있습니다.");
            return false;
        }

        // 슬롯 인덱스가 유효하고 해당 슬롯에 기존 알바가 있으면 교체
        if (slotIndex >= 0 && slotIndex < deployedArbeiters.Count && deployedArbeiters[slotIndex] != null)
        {
            // 기존 알바 교체
            return ReplaceArbeiter(slotIndex, npcData);
        }
        else
        {
            // 새 알바 추가
            if (deployedArbeiters.Count >= maxDeployedArbeiters)
            {
                Debug.LogWarning($"[ArbeitManager] 최대 배치 수({maxDeployedArbeiters})에 도달했습니다.");
                return false;
            }

            // 알바 생성
            int waitingPosition = deployedArbeiters.Count;
            GameObject arbeiterObj = CreateArbeiter(npcData, waitingPosition);

            if (arbeiterObj != null)
            {
                deployedArbeiters.Add(arbeiterObj);
                npcData.is_deployed = true;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 특정 슬롯의 알바를 새 알바로 교체
    /// - 기존 알바 제거 후 새 알바 생성
    /// - 슬롯 인덱스가 유효하지 않으면 실패
    /// - 새 알바 데이터가 null이면 실패
    /// - 교체 성공 시 true 반환
    /// - 교체 실패 시 false 반환
    /// - 기존 알바 교체할때만 최대 배치 수 제한 무시
    /// 등의 기능이 포함됨
    /// </summary>
    public bool ReplaceArbeiter(int slotIndex, npc newNpcData)
    {
        if (slotIndex < 0 || slotIndex >= deployedArbeiters.Count)
        {
            Debug.LogWarning($"[ArbeitManager] 유효하지 않은 슬롯 인덱스: {slotIndex}");
            return false;
        }

        if (newNpcData == null)
        {
            Debug.LogWarning("[ArbeitManager] 새 NPC 데이터가 null입니다.");
            return false;
        }

        // 기존 알바 제거
        GameObject oldArbeiter = deployedArbeiters[slotIndex];
        if (oldArbeiter != null)
        {
            ArbeitController controller = oldArbeiter.GetComponent<ArbeitController>();
            if (controller != null && controller.myNpcData != null)
            {
                controller.myNpcData.is_deployed = false;
            }
            Destroy(oldArbeiter);
        }

        // 새 알바 생성
        GameObject newArbeiter = CreateArbeiter(newNpcData, slotIndex);
        if (newArbeiter != null)
        {
            deployedArbeiters[slotIndex] = newArbeiter;
            newNpcData.is_deployed = true;
            Debug.Log($"[ArbeitManager] '{newNpcData.part_timer_name}' 교체 배치 완료 (슬롯 {slotIndex})");
            return true;
        }

        return false;
    }

    /// <summary>
    /// 특정 슬롯의 알바 배치 해제
    /// - slotIndex: 해제할 슬롯 인덱스
    /// - 성공 시 true, 실패 시 false 반환
    /// - 배치 해제 시 npcData.is_deployed = false로 설정
    /// - 해제 성공 시 해당 슬롯의 알바 오브젝트 제거 및 리스트에서 삭제
    /// - 남은 알바들의 대기 위치 재조정
    /// - 기타 예외 상황 처리 포함
    /// 등의 기능 포함
    /// </summary>
    public bool UndeployArbeiter(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= deployedArbeiters.Count)
        {
            Debug.LogWarning($"[ArbeitManager] 유효하지 않은 슬롯 인덱스: {slotIndex}");
            return false;
        }

        GameObject arbeiter = deployedArbeiters[slotIndex];
        if (arbeiter != null)
        {
            ArbeitController controller = arbeiter.GetComponent<ArbeitController>();
            if (controller != null && controller.myNpcData != null)
            {
                controller.myNpcData.is_deployed = false;
            }
            Destroy(arbeiter);
        }

        deployedArbeiters.RemoveAt(slotIndex);

        // 남은 알바들의 대기 위치 재조정
        UpdateWaitingPositions();

        return true;
    }

    /// <summary>
    /// 특정 슬롯에 배치된 NPC 데이터 반환
    /// </summary>
    public npc GetDeployedNpcBySlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= deployedArbeiters.Count)
            return null;

        GameObject arbeiter = deployedArbeiters[slotIndex];
        if (arbeiter == null)
            return null;

        ArbeitController controller = arbeiter.GetComponent<ArbeitController>();
        return controller?.myNpcData;
    }

    /// <summary>
    /// 알바들의 대기 위치 재조정
    /// - 배치된 알바들의 대기 위치를 ArbeitPoint 기준으로 재설정
    /// - 각 알바의 대기 위치 인덱스에 따라 위치 업데이트
    /// - ArbeitController의 UpdateWaitingPosition 메서드 호출
    /// - arbeitPoint가 null일 경우 동작하지 않음
    /// - 배치된 알바 리스트 순서에 따라 대기 위치 결정
    /// 등의 기능 포함
    /// </summary>
    private void UpdateWaitingPositions()
    {
        if (arbeitPoint == null) return;

        for (int i = 0; i < deployedArbeiters.Count; i++)
        {
            if (deployedArbeiters[i] == null) continue;

            ArbeitController controller = deployedArbeiters[i].GetComponent<ArbeitController>();
            if (controller != null)
            {
                controller.UpdateWaitingPosition(i);
            }
        }
    }

    /// <summary>
    /// 배치 상태 동기화 (디버그 및 데이터 정합성용, 데이터 불일치 방지)
    /// </summary>
    public void SyncDeployedState()
    {
        // deployedArbeiters 리스트에 있는 NPC들은 is_deployed = true
        foreach (var arbeiter in deployedArbeiters)
        {
            if (arbeiter == null) continue;

            ArbeitController controller = arbeiter.GetComponent<ArbeitController>();
            if (controller != null && controller.myNpcData != null)
            {
                controller.myNpcData.is_deployed = true;
            }
        }

        // 초과된 오브젝트 제거
        while (deployedArbeiters.Count > maxDeployedArbeiters)
        {
            int lastIndex = deployedArbeiters.Count - 1;
            GameObject lastArbeiter = deployedArbeiters[lastIndex];

            if (lastArbeiter != null)
            {
                ArbeitController controller = lastArbeiter.GetComponent<ArbeitController>();
                if (controller != null && controller.myNpcData != null)
                {
                    controller.myNpcData.is_deployed = false;
                }
                Destroy(lastArbeiter);
            }

            deployedArbeiters.RemoveAt(lastIndex);
            Debug.LogWarning($"[ArbeitManager] 초과된 알바 오브젝트 제거 (현재: {deployedArbeiters.Count}/{maxDeployedArbeiters})");
        }
    }
    #endregion

    /// <summary>
    /// 모든 알바 제거
    /// </summary>
    public void ClearAllArbeiters()
    {
        foreach (var arbeiter in deployedArbeiters)
        {
            if (arbeiter != null)
            {
                // NPC 데이터의 배치 상태 업데이트
                ArbeitController controller = arbeiter.GetComponent<ArbeitController>();
                if (controller != null && controller.myNpcData != null)
                {
                    controller.myNpcData.is_deployed = false;
                }

                Destroy(arbeiter);
            }
        }
        deployedArbeiters.Clear();
    }
    #endregion

    /*
    #region NPC 스폰 메서드
    /// <summary>
    /// Island 씬에서 고용된 모든 NPC를 스폰함
    /// ArbeitPrefabReference를 사용하여 초상화와 매칭되는 프리팹으로 생성
    /// </summary>
    public List<GameObject> SpawnAllNpcs(Transform spawnPoint = null)
    {
        List<GameObject> spawnedNpcs = new List<GameObject>();

        if (arbeitPrefabReference == null)
        {
            Debug.LogError("[ArbeitManager] arbeitPrefabReference가 null입니다.");
            return spawnedNpcs;
        }

        // 고용된 NPC 목록 가져오기
        var hiredNpcs = ArbeitRepository.Instance.GethiredNpcs();
        if (hiredNpcs == null || hiredNpcs.Count == 0)
        {
            Debug.LogWarning("[ArbeitManager] 고용된 NPC가 없습니다.");
            return spawnedNpcs;
        }

        Vector3 basePosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        for (int i = 0; i < hiredNpcs.Count; i++)
        {
            npc npcData = hiredNpcs[i];
            GameObject spawnedNpc = SpawnSingleNpc(npcData, basePosition + new Vector3(i * 1.5f, 0, 0));

            if (spawnedNpc != null)
            {
                spawnedNpcs.Add(spawnedNpc);
            }
        }
        return spawnedNpcs;
    }

    /// <summary>
    /// NPC를 1명 스폰
    /// </summary>
    /// <param name="npcData">스폰할 NPC의 데이터</param>
    /// <param name="spawnPosition">스폰 위치</param>
    /// <returns>스폰된 GameObject (실패 시 null)</returns>
    public GameObject SpawnSingleNpc(npc npcData, Vector3 spawnPosition)
    {
        if (npcData == null)
        {
            Debug.LogWarning("[ArbeitManager] NPC 데이터가 null입니다.");
            return null;
        }

        if (npcData.portraitSprite == null)
        {
            Debug.LogWarning($"[ArbeitManager] '{npcData.part_timer_name}'의 portraitSprite가 null입니다.");
            return null;
        }

        // 초상화 스프라이트로 매칭되는 프리팹 찾기
        GameObject prefab = ArbeitRepository.Instance.GetMatchingPrefabByPortrait(npcData);
        if (prefab == null)
        {
            Debug.LogWarning($"[ArbeitManager] '{npcData.part_timer_name}'의 초상화와 매칭되는 프리팹을 찾을 수 없습니다.");
            return null;
        }

        // 프리팹 인스턴스화
        GameObject spawnedNpc = Instantiate(prefab, spawnPosition, Quaternion.identity);
        spawnedNpc.name = $"{npcData.part_timer_name}_{npcData.part_timer_id}";

        // ArbeitController 자동 할당 및 초기화
        ArbeitController controller = spawnedNpc.GetComponent<ArbeitController>();
        if (controller == null)
        {
            controller = spawnedNpc.AddComponent<ArbeitController>();
        }

        // NPC 데이터 초기화
        controller.Initialize(npcData);

        // 할당되지 않은 필드 자동 할당 (예: pathfinder 등)
        if (controller.pathfinder == null)
        {
            IsometricPathfinder pathfinderInScene = FindObjectOfType<IsometricPathfinder>();
            if (pathfinderInScene != null)
            {
                controller.pathfinder = pathfinderInScene;
            }
        }

        return spawnedNpc;
    }
    #endregion
    */
}