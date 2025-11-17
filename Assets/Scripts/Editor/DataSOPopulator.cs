using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject 데이터를 자동으로 채워주는 에디터 유틸리티
/// </summary>
public class DataSOPopulator : EditorWindow
{
    [MenuItem("Tools/Populate ScriptableObject Data")]
    public static void ShowWindow()
    {
        GetWindow<DataSOPopulator>("SO Data Populator");
    }

    [MenuItem("Tools/Populate ALL Data (Auto)")]
    public static void PopulateAllData()
    {
        PopulatePersonalityData();
        PopulateBuildingData();
        PopulateBuildingProductionInfo();
        
        Debug.Log("=== 모든 ScriptableObject 데이터 자동 채우기 완료! ===");
    }

    private void OnGUI()
    {
        GUILayout.Label("ScriptableObject Data Populator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Populate PersonalityDataSO", GUILayout.Height(30)))
        {
            PopulatePersonalityData();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Populate BuildingDataSO", GUILayout.Height(30)))
        {
            PopulateBuildingData();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Populate BuildingProductionInfoSO", GUILayout.Height(30)))
        {
            PopulateBuildingProductionInfo();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Populate ALL", GUILayout.Height(40)))
        {
            PopulateAllData();
        }
    }

    private static void PopulatePersonalityData()
    {
        // PersonalityDataSO 로드
        string soPath = "Assets/Resources/Data/NPC/Personality/PersonalityData.asset";
        PersonalityDataSO personalityDataSO = AssetDatabase.LoadAssetAtPath<PersonalityDataSO>(soPath);

        if (personalityDataSO == null)
        {
            Debug.LogError($"PersonalityDataSO를 찾을 수 없습니다: {soPath}");
            return;
        }

        // 모든 Personality 에셋 로드
        string[] guids = AssetDatabase.FindAssets("t:Personality", new[] { "Assets/Resources/Data/NPC/Personality" });
        List<Personality> personalities = new List<Personality>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Personality personality = AssetDatabase.LoadAssetAtPath<Personality>(path);
            if (personality != null)
            {
                personalities.Add(personality);
            }
        }

        // 리스트에 추가
        personalityDataSO.personalities = personalities;
        
        // 저장
        EditorUtility.SetDirty(personalityDataSO);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ PersonalityDataSO에 {personalities.Count}개의 Personality를 추가했습니다.");
    }

    private static void PopulateBuildingData()
    {
        // BuildingDataSO 로드
        string soPath = "Assets/Resources/Data/Building/BuildingDataSO.asset";
        BuildingDataSO buildingDataSO = AssetDatabase.LoadAssetAtPath<BuildingDataSO>(soPath);

        if (buildingDataSO == null)
        {
            Debug.LogWarning($"BuildingDataSO를 찾을 수 없습니다: {soPath}");
            Debug.Log("BuildingDataSO를 생성합니다...");
            
            buildingDataSO = ScriptableObject.CreateInstance<BuildingDataSO>();
            AssetDatabase.CreateAsset(buildingDataSO, soPath);
        }

        // 모든 BuildingData 에셋 로드
        string[] guids = AssetDatabase.FindAssets("t:BuildingData", new[] { "Assets/Resources/Data/Building" });
        List<BuildingData> buildings = new List<BuildingData>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildingData building = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (building != null)
            {
                buildings.Add(building);
            }
        }

        // 리스트에 추가
        buildingDataSO.buildings = buildings;
        
        // 저장
        EditorUtility.SetDirty(buildingDataSO);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ BuildingDataSO에 {buildings.Count}개의 BuildingData를 추가했습니다.");
    }

    private static void PopulateBuildingProductionInfo()
    {
        // BuildingProductionInfoSO 로드
        string soPath = "Assets/Resources/Data/Building/BuildingProductionInfoSO.asset";
        BuildingProductionInfoSO productionInfoSO = AssetDatabase.LoadAssetAtPath<BuildingProductionInfoSO>(soPath);

        if (productionInfoSO == null)
        {
            Debug.LogWarning($"BuildingProductionInfoSO를 찾을 수 없습니다: {soPath}");
            Debug.Log("BuildingProductionInfoSO를 생성합니다...");
            
            productionInfoSO = ScriptableObject.CreateInstance<BuildingProductionInfoSO>();
            AssetDatabase.CreateAsset(productionInfoSO, soPath);
        }

        // 모든 BuildingProductionInfo 에셋 로드
        string[] guids = AssetDatabase.FindAssets("t:BuildingProductionInfo", new[] { "Assets/Resources/Data/Building" });
        List<BuildingProductionInfo> productionInfos = new List<BuildingProductionInfo>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildingProductionInfo info = AssetDatabase.LoadAssetAtPath<BuildingProductionInfo>(path);
            if (info != null)
            {
                productionInfos.Add(info);
            }
        }

        // 리스트에 추가
        productionInfoSO.productionInfos = productionInfos;
        
        // 저장
        EditorUtility.SetDirty(productionInfoSO);
        AssetDatabase.SaveAssets();
        
        Debug.Log($"✅ BuildingProductionInfoSO에 {productionInfos.Count}개의 ProductionInfo를 추가했습니다.");
    }
}
