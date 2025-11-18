using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

public class JsonDataHandler
{
    private readonly string arbeitDataPath;
    private readonly string constructedBuildingProductionPath;

    public JsonDataHandler()
    {
        // Application.persistentDataPath를 사용하여 빌드 후에도 안전하게 파일을 읽고 쓸 수 있도록 경로를 설정합니다.
        arbeitDataPath = Path.Combine(Application.persistentDataPath, "ArbeitData.json");
        constructedBuildingProductionPath = Path.Combine(Application.persistentDataPath, "ConstructedBuildingProduction.json");
    }

    public void InitializeFiles()
    {
        CreateFileIfNotExists(arbeitDataPath);
        CreateFileIfNotExists(constructedBuildingProductionPath);
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
        SaveData(productions, constructedBuildingProductionPath);
    }

    public List<ConstructedBuildingProduction> LoadConstructedBuildingProductions()
    {
        return LoadData<ConstructedBuildingProduction>(constructedBuildingProductionPath);
    }
}
