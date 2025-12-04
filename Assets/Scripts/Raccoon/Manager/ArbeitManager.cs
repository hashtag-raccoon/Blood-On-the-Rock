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
    public ArbeitPrefabReference arbeitPrefabReference; // 프리팹 레퍼런스 할당

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
        }
        else
        {
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
        if (arbeitPrefabReference == null)
        {
            Debug.LogError("[ArbeitManager] arbeitPrefabReference가 할당되지 않았습니다.");
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
        arbeiterObj.name = $"Arbeiter_{npcData.part_timer_name}";

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

        // ArbeitPoint 할당 및 대기열 추가
        controller.SetArbeitPoint(arbeitPoint);

        return arbeiterObj;
    }

    /// <summary>
    /// NPC의 초상화 스프라이트와 매칭되는 프리팹을 찾습니다.
    /// </summary>
    private GameObject GetMatchingPrefab(npc npcData)
    {
        if (npcData.portraitSprite == null)
        {
            Debug.LogWarning($"[ArbeitManager] '{npcData.part_timer_name}'의 portraitSprite가 null임");
            return null;
        }

        // 종족에 따라 리스트 선택
        List<ArbeitPrefabToSpritePair> pairs = null;
        switch (npcData.race)
        {
            case "Human":
                pairs = arbeitPrefabReference.Human_Pairs;
                break;
            case "Oak":
                pairs = arbeitPrefabReference.Oak_Pairs;
                break;
            case "Vampire":
                pairs = arbeitPrefabReference.Vampire_Pairs;
                break;
            default:
                Debug.LogWarning($"[ArbeitManager] 알 수 없는 종족: {npcData.race}");
                return null;
        }

        // 초상화 스프라이트로 매칭되는 프리팹 찾기
        foreach (var pair in pairs)
        {
            if (pair.PairPortrait == npcData.portraitSprite)
            {
                return pair.PairPrefab;
            }
        }

        Debug.LogWarning($"[ArbeitManager] '{npcData.part_timer_name}'의 초상화와 매칭되는 프리팹을 찾을 수 없음 (종족: {npcData.race})");
        return null;
    }

    /// <summary>
    /// 특정 알바 제거
    /// </summary>
    public void RemoveArbeiter(GameObject arbeiter)
    {
        if (arbeiter == null) return;

        if (deployedArbeiters.Contains(arbeiter))
        {
            deployedArbeiters.Remove(arbeiter);

            // NPC 데이터의 배치 상태 업데이트
            ArbeitController controller = arbeiter.GetComponent<ArbeitController>();
            if (controller != null && controller.myNpcData != null)
            {
                controller.myNpcData.is_deployed = false;
            }

            Destroy(arbeiter);
        }
    }

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