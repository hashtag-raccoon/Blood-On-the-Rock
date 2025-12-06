using System;
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
    private readonly string cocktailProgressPath;
    private readonly string glassProgressPath;

    public JsonDataHandler()
    {
        // Application.persistentDataPath를 사용하여 빌드 후에도 안전하게 파일을 읽고 쓸 수 있도록 경로를 설정합니다.
        arbeitDataPath = Path.Combine(Application.persistentDataPath, "ArbeitData.json");
        constructedBuildingProductionPath = Path.Combine(Application.persistentDataPath, "ConstructedBuildingProduction.json");
        BuildingPositonPath = Path.Combine(Application.persistentDataPath, "BuildingPosition.json");
        cocktailProgressPath = Path.Combine(Application.persistentDataPath, "CocktailProgress.json");
        glassProgressPath = Path.Combine(Application.persistentDataPath, "GlassProgress.json");
    }

    public void InitializeFiles()
    {
        CreateFileIfNotExists(arbeitDataPath);
        CreateFileIfNotExists(constructedBuildingProductionPath);
        CreateFileIfNotExists(BuildingPositonPath);
        CreateFileIfNotExists(cocktailProgressPath);
        CreateFileIfNotExists(glassProgressPath);
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
        var productions = LoadData<ConstructedBuildingProduction>(constructedBuildingProductionPath);

        // 기존 데이터 호환성 처리 (production_slots가 null인 경우)
        foreach (var production in productions)
        {
            if (production.production_slots == null)
            {
                production.production_slots = new List<ProductionSlotData>();
            }
        }

        return productions;
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

        // instance_id로 딕셔너리 생성 (중복 처리)
        var oldDict = new Dictionary<long, ConstructedBuildingProduction>();
        foreach (var item in oldData)
        {
            if (!oldDict.ContainsKey(item.instance_id))
            {
                oldDict.Add(item.instance_id, item);
            }
            else
            {
                Debug.LogWarning($"[HasProductionChanges] 중복된 instance_id '{item.instance_id}'가 oldData에 있습니다. 첫 번째 항목만 사용됩니다.");
            }
        }

        foreach (var newItem in newData)
        {
            // 새로운 건물이 추가되었는지 확인
            if (!oldDict.TryGetValue(newItem.instance_id, out var oldItem))
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

        // instance_id로 딕셔너리 생성 (중복 처리)
        var oldDict = new Dictionary<long, ConstructedBuildingPos>();
        foreach (var item in oldData)
        {
            if (!oldDict.ContainsKey(item.instance_id))
            {
                oldDict.Add(item.instance_id, item);
            }
            else
            {
                Debug.LogWarning($"[HasPositionChanges] 중복된 instance_id '{item.instance_id}'가 oldData에 있습니다. 첫 번째 항목만 사용됩니다.");
            }
        }

        foreach (var newItem in newData)
        {
            // 새로운 건물이 추가되었는지 확인
            if (!oldDict.TryGetValue(newItem.instance_id, out var oldItem))
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

    #region Cocktail Progress

    /// <summary>
    /// 칵테일 진행 정보를 저장
    /// </summary>
    public void SaveCocktailProgress(List<int> unlockedRecipeIds)
    {
        var progressData = new CocktailProgressData
        {
            unlockedRecipeIds = unlockedRecipeIds
        };

        string jsonData = JsonConvert.SerializeObject(progressData, Formatting.Indented);
        File.WriteAllText(cocktailProgressPath, jsonData);
        Debug.Log($"CocktailProgress 데이터를 저장했습니다. 해금 레시피: {unlockedRecipeIds.Count}개");
    }

    /// <summary>
    /// 칵테일 진행 정보를 로드
    /// </summary>
    public CocktailProgressData LoadCocktailProgress()
    {
        if (!File.Exists(cocktailProgressPath))
        {
            Debug.LogWarning($"{cocktailProgressPath} 파일이 존재하지 않습니다. 기본값을 반환합니다.");
            return new CocktailProgressData();
        }

        try
        {
            string jsonData = File.ReadAllText(cocktailProgressPath);

            // 빈 배열이거나 잘못된 형식인 경우 기본값 반환
            if (string.IsNullOrWhiteSpace(jsonData) || jsonData.Trim() == "[]")
            {
                Debug.LogWarning("CocktailProgress.json이 빈 배열이거나 잘못된 형식입니다. 기본값으로 초기화합니다.");
                return new CocktailProgressData();
            }

            CocktailProgressData progressData = JsonConvert.DeserializeObject<CocktailProgressData>(jsonData);
            return progressData ?? new CocktailProgressData();
        }
        catch (JsonException ex)
        {
            Debug.LogWarning($"CocktailProgress.json 역직렬화 실패: {ex.Message}. 기본값으로 초기화합니다.");
            return new CocktailProgressData();
        }
    }

    #endregion

    #region Glass Progress

    /// <summary>
    /// 잔 소유 정보를 저장
    /// </summary>
    public void SaveGlassProgress(List<int> ownedGlassIds)
    {
        var progressData = new GlassProgressData
        {
            ownedGlassIds = ownedGlassIds
        };

        string jsonData = JsonConvert.SerializeObject(progressData, Formatting.Indented);
        File.WriteAllText(glassProgressPath, jsonData);
        Debug.Log($"GlassProgress 데이터를 저장했습니다. 소유 잔: {ownedGlassIds.Count}개");
    }

    /// <summary>
    /// 잔 소유 정보를 로드
    /// </summary>
    public GlassProgressData LoadGlassProgress()
    {
        if (!File.Exists(glassProgressPath))
        {
            Debug.LogWarning($"{glassProgressPath} 파일이 존재하지 않습니다. 기본값을 반환합니다.");
            return new GlassProgressData();
        }

        try
        {
            string jsonData = File.ReadAllText(glassProgressPath);

            // 빈 배열이거나 잘못된 형식인 경우 기본값 반환
            if (string.IsNullOrWhiteSpace(jsonData) || jsonData.Trim() == "[]")
            {
                Debug.LogWarning("GlassProgress.json이 빈 배열이거나 잘못된 형식입니다. 기본값으로 초기화합니다.");
                return new GlassProgressData();
            }

            GlassProgressData progressData = JsonConvert.DeserializeObject<GlassProgressData>(jsonData);
            return progressData ?? new GlassProgressData();
        }
        catch (JsonException ex)
        {
            Debug.LogWarning($"GlassProgress.json 역직렬화 실패: {ex.Message}. 기본값으로 초기화합니다.");
            return new GlassProgressData();
        }
    }

    #endregion
}

/// <summary>
/// 칵테일 진행 정보를 저장하기 위한 데이터 클래스
/// </summary>
[Serializable]
public class CocktailProgressData
{
    public List<int> unlockedRecipeIds = new List<int>();
}

/// <summary>
/// 잔 소유 정보를 저장하기 위한 데이터 클래스
/// </summary>
[Serializable]
public class GlassProgressData
{
    public List<int> ownedGlassIds = new List<int>();
}
