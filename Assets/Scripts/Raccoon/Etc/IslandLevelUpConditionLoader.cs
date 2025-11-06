using UnityEngine;
using System.Collections.Generic;

public class IslandConditionLoader : MonoBehaviour
{
    private const string JSON_PATH = "Data/LevelupCondition";
    private IslandConditionDatabase database;

    private void Awake()
    {
        LoadJson();
    }

    private void LoadJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>(JSON_PATH);
        if (jsonFile == null)
        {
            Debug.LogError($"JSON 파일을 찾을 수 없습니다: {JSON_PATH}");
            return;
        }

        database = JsonUtility.FromJson<IslandConditionDatabase>(jsonFile.text);
    }

    public IslandLevelData GetLevelData(int islandLevel)
    {
        if (database == null || !database.IslandLevels.ContainsKey(islandLevel.ToString()))
            return null;
        return database.IslandLevels[islandLevel.ToString()];
    }

    public int GetConditionCount(int islandLevel)
    {
        var data = GetLevelData(islandLevel);
        return data != null ? data.ConditionCount : 0;
    }

    public List<ConditionData> GetConditions(int islandLevel)
    {
        var data = GetLevelData(islandLevel);
        return data != null ? data.Conditions : new List<ConditionData>();
    }
}
