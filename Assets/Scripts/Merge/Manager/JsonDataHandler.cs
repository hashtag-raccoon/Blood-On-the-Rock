using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class JsonDataHandler
{
    private readonly string arbeitDataPath;
    private readonly string constructedBuildingProductionPath;
    private readonly string BuildingPositonPath;

    public JsonDataHandler()
    {
        // Application.persistentDataPath를 사용하여 빌드 후에도 안전하게 파일을 읽고 쓸 수 있도록 경로를 설정합니다.
        arbeitDataPath = Path.Combine(Application.persistentDataPath, "ArbeitData.json");
        constructedBuildingProductionPath = Path.Combine(Application.persistentDataPath, "ConstructedBuildingProduction.json");
        BuildingPositonPath = Path.Combine(Application.persistentDataPath, "BuildingPosition.json");
    }

    public void InitializeFiles()
    {
        CreateFileIfNotExists(arbeitDataPath);
        CreateFileIfNotExists(constructedBuildingProductionPath);
        CreateFileIfNotExists(BuildingPositonPath);
    }

    private void CreateFileIfNotExists(string path)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, "[]");
        }
    }

    // ... (save/load 메서드들) ...
    public void SaveData<T>(List<T> data, string path)
    {
        string jsonData = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(path, jsonData);
        Debug.Log($"{typeof(T).Name} 데이터 {data.Count}개를 {path}에 저장했습니다.");
    }

    public List<T> LoadData<T>(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning($"{path} 파일이 존재하지 않습니다. 빈 리스트를 반환합니다.");
            return new List<T>();
        }

        string jsonData = File.ReadAllText(path);
        List<T> dataList = JsonConvert.DeserializeObject<List<T>>(jsonData);
        return dataList ?? new List<T>();
    }

    public void SaveArbeitData(List<ArbeitData> arbeitData)
    {
        SaveData(arbeitData, arbeitDataPath);
    }

    public List<ArbeitData> LoadArbeitData()
    {
        return LoadData<ArbeitData>(arbeitDataPath);
    }

    public void SaveConstructedBuildingProductions(List<ConstructedBuildingProduction> productions)
    {
        // 기존 데이터 로드
        var existingData = LoadConstructedBuildingProductions();

        // 변경사항이 있는지 확인
        if (HasProductionChanges(existingData, productions))
        {
            SaveData(productions, constructedBuildingProductionPath);
            Debug.Log($"ConstructedBuildingProduction 데이터 변경사항이 감지되어 저장했습니다.");
        }
        else
        {
            Debug.Log("ConstructedBuildingProduction 데이터에 변경사항이 없어 저장을 건너뜁니다.");
        }
    }

    public List<ConstructedBuildingProduction> LoadConstructedBuildingProductions()
    {
        return LoadData<ConstructedBuildingProduction>(constructedBuildingProductionPath);
    }

    public void SaveBuildingPosition(List<ConstructedBuildingPos> positions)
    {
        // 기존 데이터 로드
        var existingData = LoadBuildingPositions();

        // 변경사항이 있는지 확인
        if (HasPositionChanges(existingData, positions))
        {
            SaveData(positions, BuildingPositonPath);
            Debug.Log($"BuildingPosition 데이터 변경사항이 감지되어 저장했습니다.");
        }
        else
        {
            Debug.Log("BuildingPosition 데이터에 변경사항이 없어 저장을 건너뜁니다.");
        }
    }

    public List<ConstructedBuildingPos> LoadBuildingPositions()
    {
        return LoadData<ConstructedBuildingPos>(BuildingPositonPath);
    }

    /// <summary>
    /// ConstructedBuildingProduction 데이터의 변경사항을 확인합니다.
    /// </summary>
    private bool HasProductionChanges(List<ConstructedBuildingProduction> oldData, List<ConstructedBuildingProduction> newData)
    {
        if (oldData == null && newData == null) return false;
        if (oldData == null || newData == null) return true;
        if (oldData.Count != newData.Count) return true;

        // building_id로 딕셔너리 생성
        var oldDict = oldData.ToDictionary(p => p.building_id);

        foreach (var newItem in newData)
        {
            // 새로운 건물이 추가되었는지 확인
            if (!oldDict.TryGetValue(newItem.building_id, out var oldItem))
            {
                return true;
            }

            // 각 필드 비교
            if (oldItem.is_producing != newItem.is_producing ||
                oldItem.last_production_time != newItem.last_production_time ||
                oldItem.next_production_time != newItem.next_production_time)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ConstructedBuildingPos 데이터의 변경사항을 확인합니다.
    /// </summary>
    private bool HasPositionChanges(List<ConstructedBuildingPos> oldData, List<ConstructedBuildingPos> newData)
    {
        if (oldData == null && newData == null) return false;
        if (oldData == null || newData == null) return true;
        if (oldData.Count != newData.Count) return true;

        // building_id로 딕셔너리 생성
        var oldDict = oldData.ToDictionary(p => p.building_id);

        foreach (var newItem in newData)
        {
            // 새로운 건물이 추가되었는지 확인
            if (!oldDict.TryGetValue(newItem.building_id, out var oldItem))
            {
                return true;
            }

            // 각 필드 비교
            if (oldItem.pos != newItem.pos || oldItem.rotation != newItem.rotation)
            {
                return true;
            }
        }

        return false;
    }
}
