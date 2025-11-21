using UnityEngine;
using System;

/// <summary>
/// 건물 오브젝트를 동적으로 생성하는 클래스
/// BuildingData를 기반으로 필요한 컴포넌트를 자동으로 추가함
/// ex) 만약 해당 BuildingData의 buildingType이 Production이면 ResourceBuildingController 컴포넌트를 추가 및 필드 할당
///     비생산형 건물(장식, 유틸리티 등)의 경우 BuildingBase만 추가
/// </summary>
public static class BuildingFactory
{
    #region Building Creation
    /// <summary>
    /// BuildingData로부터 완전한 건물 GameObject를 생성합니다.
    /// 건물 타입에 따라 적절한 컴포넌트를 자동으로 추가합니다.
    /// </summary>
    /// <param name="buildingData">건물 데이터</param>
    /// <param name="position">생성 위치</param>
    /// <param name="parent">부모 Transform (선택)</param>
    /// <param name="isLoadingExisting">true이면 저장된 건물을 불러오는 것, false이면 새 건물 생성</param>
    /// <returns>생성된 건물 GameObject</returns>
    public static GameObject CreateBuilding(BuildingData buildingData, Vector3 position, Transform parent = null, bool isLoadingExisting = false)
    {
        if (buildingData == null || buildingData.building_sprite == null)
        {
            return null;
        }

        GameObject buildingObj = new GameObject($"{buildingData.Building_Name}_{buildingData.building_id}");
        buildingObj.transform.position = position;
        
        if (parent != null)
        {
            buildingObj.transform.SetParent(parent);
        }

        // 기본 컴포넌트 추가
        AddBasicComponents(buildingObj, buildingData);

        // 건물 타입에 따라 컴포넌트 추가시킴
        switch(buildingData.buildingType)
        {
            case BuildingType.Production: // 생산형 건물일 경우
                AddProductionBuildingComponents(buildingObj, buildingData, position, isLoadingExisting);
                break;
            // 추후 비생산형 건물 타입 추가 예정
            default: // 그 외의 건물일 경우(꾸미는 용도의 건물 등등)
                AddNonProductionBuildingComponents(buildingObj, buildingData);
                break;
        }

        return buildingObj;
    }
    #endregion

    #region Component Addition
    /// <summary>
    /// 모든 건물에 공통으로 필요한 기본 컴포넌트 추가
    /// </summary>
    private static void AddBasicComponents(GameObject buildingObj, BuildingData buildingData)
    {
        // SpriteRenderer 추가, 스프라이트 세팅
        SpriteRenderer spriteRenderer = buildingObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = buildingData.building_sprite;
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 2;

        // PolygonCollider2D 추가
        // 고려사항 : 최적화가 걱정되어, 후에는 어떻게든 Collider2D를 개선할 필요가 있음
        PolygonCollider2D collider = buildingObj.AddComponent<PolygonCollider2D>();
    }

    /// <summary>
    /// 생산형 건물에 필요한 컴포넌트 추가
    /// ResourceBuildingController 스크립트를 추가하고 필요한 필드를 자동 할당시킴
    /// </summary>
    private static void AddProductionBuildingComponents(GameObject buildingObj, BuildingData buildingData, Vector3 position, bool isLoadingExisting)
    {
        ResourceBuildingController controller = buildingObj.AddComponent<ResourceBuildingController>();

        // BuildingBase 필드 자동 할당
        AssignBuildingBaseFields(controller, buildingData, position, isLoadingExisting);

        // ResourceBuildingController 전용 필드 할당
        AssignProductionBuildingFields(controller);
    }

    /// <summary>
    /// 비생산형 건물에 BuildingBase만 추가
    /// </summary>
    private static void AddNonProductionBuildingComponents(GameObject buildingObj, BuildingData buildingData)
    {
        // 비생산형 건물은 BuildingBase 추상 클래스를 상속받은 구체 클래스가 필요하므로, 현재로서는 제대로 구현이 안돼있음
        // 추후 구현예정인 메소드
        // 임시로 BuildingReference만 추가됨
    }

    /// <summary>
    /// BuildingBase의 필드들을 자동으로 할당시키는 메소드
    /// </summary>
    private static void AssignBuildingBaseFields(BuildingBase buildingBase, BuildingData buildingData, Vector3 position, bool isLoadingExisting)
    {
        if (buildingBase == null || buildingData == null) return;

        // Reflection을 사용하여 private/protected 필드 할당
        var type = typeof(BuildingBase);

        // 새 건물 생성 시에만 ConstructedBuilding 추가 (저장된 건물 불러올 때는 이미 존재함)
        if (!isLoadingExisting)
        {
            BuildingRepository.Instance.AddConstructedBuilding(buildingData.building_id, position);
        }

        // constructedBuildingId를 조회하여 할당
        int constructedBuildingId = GetConstructedBuildingId(buildingData.building_id);
        SetField(type, buildingBase, "constructedBuildingId", constructedBuildingId);

        // BuildingSprite 할당
        SetField(type, buildingBase, "BuildingSprite", buildingData.building_sprite);

        // tileSize 할당
        SetField(type, buildingBase, "tileSize", buildingData.tileSize);

        // BuildingReferenceManager에서 건물 타입에 따라 UI 할당
        if (BuildingReferenceManager.Instance != null)
        {
            BuildingType buildingType = buildingData.buildingType;

            // 건물 타입에 맞는 UI 할당 
            GameObject buildingUIPrefab = BuildingReferenceManager.Instance.GetBuildingUIPrefab(buildingType);
            SetField(type, buildingBase, "BuildingUI", buildingUIPrefab);

            // 건물 타입에 맞는 업그레이드 버튼  할당
            GameObject upgradeButtonPrefab = BuildingReferenceManager.Instance.GetBuildingUpgradeButtonPrefab(buildingType);
            if (upgradeButtonPrefab != null)
            {
                UnityEngine.UI.Button buttonComponent = upgradeButtonPrefab.GetComponent<UnityEngine.UI.Button>();
                SetField(type, buildingBase, "BuildingUpgradeButton", buttonComponent);
            }

            // 공통 UI할당
            GameObject upgradeUIPrefab = BuildingReferenceManager.Instance.GetUpgradeUIPrefab();
            SetField(type, buildingBase, "UpgradeUIPrefab", upgradeUIPrefab);

            GameObject upgradeBlurUI = BuildingReferenceManager.Instance.GetUpgradeBlurUI();
            SetField(type, buildingBase, "UpgradeBlurUI", upgradeBlurUI);
        }

        // DragDropController 할당하여 배치 모드 진입 가능하게 설정
        DragDropController dragDropController = UnityEngine.Object.FindObjectOfType<DragDropController>();
        SetField(type, buildingBase, "dragDropController", dragDropController);
    }

    /// <summary>
    /// ResourceBuildingController의 필드들을 자동으로 할당시키는 메소드
    /// </summary>
    private static void AssignProductionBuildingFields(ResourceBuildingController controller)
    {
        if (controller == null || BuildingReferenceManager.Instance == null) return;

        var type = typeof(ResourceBuildingController);

        // 생산형 건물 전용 필드 할당
        int maxSlots = BuildingReferenceManager.Instance.GetMaxProductionSlots();
        SetField(type, controller, "maxProductionSlots", maxSlots);

        GameObject completeResourceUI = BuildingReferenceManager.Instance.GetCompleteResourceUIPrefab();
        SetField(type, controller, "completeResourceUIPrefab", completeResourceUI);

        GameObject limitUpgradeUI = BuildingReferenceManager.Instance.GetLimitUpgradeUIObject();
        SetField(type, controller, "LimitUpgradeUIObject", limitUpgradeUI);

        Vector2 limitImageSize = BuildingReferenceManager.Instance.GetLimitBuildingImageSize();
        SetField(type, controller, "limitBuildingImageSize", limitImageSize);

        Vector2 imageSize = BuildingReferenceManager.Instance.GetLimitBuildingImageSize();
        SetField(type, controller, "limitBuildingImageSize", imageSize);
    }
    #endregion

    #region Reflection Utilities
    /// <summary>
    /// Reflection을 사용하여 private/protected 필드에 값 할당
    /// </summary>
    private static void SetField(Type type, object instance, string fieldName, object value)
    {
        var field = type.GetField(fieldName, 
            System.Reflection.BindingFlags.NonPublic | 
            System.Reflection.BindingFlags.Public | 
            System.Reflection.BindingFlags.Instance)
            ;
        
        if (field != null)
        {
            field.SetValue(instance, value);
        }
        else
        {
            // 디버그 로그 필수 !! 왠만하면 싫어하는데 얘는 없으면 조금 골치아픔
            Debug.LogError($"[BuildingFactory.SetField] FAILED - Field '{fieldName}' not found in {type.Name}");
        }
    }

    /// <summary>
    /// DataManager에서 BuildingData의 building_id를 이용해 ConstructedBuildingProduction이 존재하는지 조회
    /// 존재하면 building_id를 constructedBuildingId로 사용
    /// </summary>
    private static int GetConstructedBuildingId(int buildingId)
    {
        if (DataManager.Instance == null || DataManager.Instance.ConstructedBuildingProductions == null)
        {
            return buildingId; // ConstructedBuildingProduction이 없으면 building_id를 그대로 사용
        }

        // building_id로 ConstructedBuilding 조회
        var Constructed = DataManager.Instance.ConstructedBuildings
            .Find(p => p.Id == buildingId);

        // 찾았으면 해당 building_id 사용, 못 찾으면 기본값 사용
        return Constructed != null ? Constructed.Id : buildingId;
    }
    #endregion
}
