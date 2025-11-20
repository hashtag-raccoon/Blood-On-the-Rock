using UnityEngine;

/// <summary>
/// 건물 동적 생성 시 필요한 공통 프리팹 및 리소스를 관리하는 싱글턴 매니저
/// Inspector에서 할당된 프리팹/오브젝트를 다른 스크립트에서 참조할 수 있도록 제공
/// </summary>
public class BuildingReferenceManager : MonoBehaviour
{
    #region Singleton
    private static BuildingReferenceManager _instance;
    public static BuildingReferenceManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<BuildingReferenceManager>();
            }
            return _instance;
        }
    }
    #endregion

    #region Common Building UI References
    [Header("공통 건물 UI 프리팹 (장식/유틸리티)")]
    [Tooltip("비생산형 건물에 사용되는 건물 UI 프리팹")]
    [SerializeField] private GameObject commonBuildingUIPrefab;

    [Tooltip("공통 건물 업그레이드 버튼 프리팹")]
    [SerializeField] private GameObject commonBuildingUpgradeButtonPrefab;
    #endregion

    #region Production Building UI References
    [Header("생산형 건물 UI 프리팹")]
    [Tooltip("생산형 건물에 사용되는 건물 UI 프리팹")]
    [SerializeField] private GameObject productionBuildingUIPrefab;

    [Tooltip("생산형 건물 업그레이드 버튼 프리팹")]
    [SerializeField] private GameObject productionBuildingUpgradeButtonPrefab;
    #endregion

    #region Shared UI References
    [Header("공유 UI 프리팹")]
    [Tooltip("업그레이드 UI 프리팹 (모든 건물 공통)")]
    [SerializeField] private GameObject upgradeUIPrefab;

    [Tooltip("업그레이드 시 블러 효과 UI (모든 건물 공통)")]
    [SerializeField] private GameObject upgradeBlurUI;
    #endregion

    #region Production Building Specific References
    [Header("생산형 건물 전용 설정")]
    [Tooltip("생산형 건물의 최대 생산 슬롯 수")]
    [SerializeField] private int maxProductionSlots = 4;

    [Tooltip("생산 완료 리소스 UI 프리팹 (Canvas 포함)")]
    [SerializeField] private GameObject completeResourceUIPrefab;

    [Tooltip("업그레이드 제한 UI 오브젝트")]
    [SerializeField] private GameObject limitUpgradeUIObject;

    [Tooltip("업그레이드 제한 시 건물 이미지 크기")]
    [SerializeField] private Vector2 limitBuildingImageSize = new Vector2(100, 100);
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// 싱글턴 인스턴스 초기화
    /// </summary>
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
    #endregion

    #region Public Getters - Building Type Specific UI
    /// <summary>
    /// 건물 타입에 따라 적절한 UI 프리팹 참조 반환 (Instantiate 안함)
    /// </summary>
    /// <param name="buildingType">건물 타입</param>
    /// <returns>건물 UI 프리팹 참조</returns>
    public GameObject GetBuildingUIPrefab(BuildingType buildingType)
    {
        return buildingType == BuildingType.Production
            ? productionBuildingUIPrefab
            : commonBuildingUIPrefab;
    }

    /// <summary>
    /// 건물 타입에 따라 적절한 업그레이드 버튼 프리팹 참조 반환 (Instantiate 안함)
    /// </summary>
    /// <param name="buildingType">건물 타입</param>
    /// <returns>업그레이드 버튼 프리팹 참조</returns>
    public GameObject GetBuildingUpgradeButtonPrefab(BuildingType buildingType)
    {
        return buildingType == BuildingType.Production
            ? productionBuildingUpgradeButtonPrefab
            : commonBuildingUpgradeButtonPrefab;
    }

    /// <summary>
    /// 업그레이드 UI 프리팹 반환 (Instantiate는 호출자가 처리)
    /// </summary>
    public GameObject GetUpgradeUIPrefab()
    {
        return upgradeUIPrefab;
    }

    /// <summary>
    /// 업그레이드 블러 UI 반환 (공유 오브젝트)
    /// </summary>
    public GameObject GetUpgradeBlurUI()
    {
        return upgradeBlurUI;
    }
    #endregion

    #region Public Getters - Production Building
    /// <summary>
    /// 생산형 건물의 최대 생산 슬롯 수 반환
    /// </summary>
    public int GetMaxProductionSlots()
    {
        return maxProductionSlots;
    }

    /// <summary>
    /// 생산 완료 리소스 UI 프리팹 반환
    /// </summary>
    public GameObject GetCompleteResourceUIPrefab()
    {
        return completeResourceUIPrefab;
    }

    /// <summary>
    /// 업그레이드 제한 UI 오브젝트 반환
    /// </summary>
    public GameObject GetLimitUpgradeUIObject()
    {
        return limitUpgradeUIObject;
    }

    /// <summary>
    /// 업그레이드 제한 시 건물 이미지 크기 반환
    /// </summary>
    public Vector2 GetLimitBuildingImageSize()
    {
        return limitBuildingImageSize;
    }
    #endregion
}
