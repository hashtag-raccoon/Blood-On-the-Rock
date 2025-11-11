using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

#region 데이터 구조

public enum ConditionType
{
    None,
    BuildingLevelGreaterThan,
    GuestCountGreaterThan,
}

[System.Serializable]
public class ConditionData
{
    public ConditionType conditionType;
    public string buildingName; 
    public int value;
}

[System.Serializable]
public class IslandLevelData
{
    public int ConditionCount;
    public List<ConditionData> Conditions = new List<ConditionData>();
}

[System.Serializable]
public class IslandConditionDatabase
{
    public Dictionary<string, IslandLevelData> IslandLevels = new Dictionary<string, IslandLevelData>();
}

#endregion

// 조건 데이터 입력 -> JSON 저장용 데이터 오브젝트
[CreateAssetMenu(menuName = "IslandConditionData")]
public class IslandConditionEditorData : ScriptableObject
{
    public int islandLevel = 1;
    public int conditionCount = 1;
    public List<ConditionData> conditions = new List<ConditionData>();

    public void ResetConditions()
    {
        conditions = new List<ConditionData>();
        for (int i = 0; i < conditionCount; i++)
        {
            conditions.Add(new ConditionData());
        }
    }
}

// 커스텀 에디터
[CustomEditor(typeof(IslandConditionEditorData))]
public class IslandConditionEditor : Editor
{
    private const string SAVE_PATH = "Assets/Resources/Data/LevelupCondition/Json/island_conditions.json";
    private IslandConditionDatabase database = new IslandConditionDatabase();

    public override void OnInspectorGUI()
    {
        IslandConditionEditorData data = (IslandConditionEditorData)target;

        EditorGUILayout.LabelField("섬 레벨 조건 설정", EditorStyles.boldLabel);
        data.islandLevel = EditorGUILayout.IntField("섬 레벨", data.islandLevel);
        data.conditionCount = EditorGUILayout.IntField("조건 개수", data.conditionCount);

        if (data.conditions.Count != data.conditionCount)
            data.ResetConditions();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("조건 설정", EditorStyles.boldLabel);

        for (int i = 0; i < data.conditionCount; i++)
        {
            var cond = data.conditions[i];

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"조건 {i + 1}", EditorStyles.boldLabel);

            cond.conditionType = (ConditionType)EditorGUILayout.EnumPopup("조건 타입", cond.conditionType);

            switch (cond.conditionType)
            {
                case ConditionType.BuildingLevelGreaterThan:
                    BuildingData building = (BuildingData)EditorGUILayout.ObjectField("건물", 
                        AssetDatabase.LoadAssetAtPath<BuildingData>($"Assets/Resources/Data/Building/{cond.buildingName}.asset"), 
                        typeof(BuildingData), false);
                    
                    if (building != null)
                        cond.buildingName = building.name;
                    
                    cond.value = EditorGUILayout.IntField("레벨 이상", cond.value);
                    break;

                case ConditionType.GuestCountGreaterThan:
                    cond.value = EditorGUILayout.IntField("손님 수 이상", cond.value);
                    break;
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("JSON 저장하기"))
        {
            SaveToJson(data);
        }
    }

    private void SaveToJson(IslandConditionEditorData data)
    {
        // JSON -> 스크립트로 데이터 복사
        if (File.Exists(SAVE_PATH))
        {
            string json = File.ReadAllText(SAVE_PATH);
            database = JsonConvert.DeserializeObject<IslandConditionDatabase>(json);
            if (database == null || database.IslandLevels == null)
                database = new IslandConditionDatabase();
        }

        // 스크립트에서 새 데이터 생성
        string key = data.islandLevel.ToString();
        IslandLevelData levelData = new IslandLevelData
        {
            ConditionCount = data.conditionCount,
            Conditions = new List<ConditionData>(data.conditions)
        };

        database.IslandLevels[key] = levelData;

        // JSON 파일에 저장
        string outputJson = JsonConvert.SerializeObject(database, Formatting.Indented);
        Directory.CreateDirectory(Path.GetDirectoryName(SAVE_PATH));
        File.WriteAllText(SAVE_PATH, outputJson);

        Debug.Log($"JSON 저장 완료: {SAVE_PATH}");
        AssetDatabase.Refresh();
    }
}
