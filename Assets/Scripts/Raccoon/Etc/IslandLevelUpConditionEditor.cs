using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

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
    public string buildingName; // ScriptableObject 이름 저장용
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

/// <summary>
/// 인스펙터에서 조건 데이터를 입력하고 JSON으로 저장하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "IslandLevelUP", menuName = "Condition")]
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

/// <summary>
/// 커스텀 에디터
/// </summary>
[CustomEditor(typeof(IslandConditionEditorData))]
public class IslandConditionEditor : Editor
{
    private const string SAVE_PATH = "Assets/Resources/Data/island_conditions.json";
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
                        AssetDatabase.LoadAssetAtPath<BuildingData>($"Assets/Resources/{cond.buildingName}.asset"),
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
        // 기존 데이터 불러오기
        if (File.Exists(SAVE_PATH))
        {
            string json = File.ReadAllText(SAVE_PATH);
            database = JsonUtility.FromJson<IslandConditionDatabase>(json);
            if (database == null || database.IslandLevels == null)
                database = new IslandConditionDatabase();
        }

        // 새 데이터 생성
        string key = data.islandLevel.ToString();
        IslandLevelData levelData = new IslandLevelData
        {
            ConditionCount = data.conditionCount,
            Conditions = new List<ConditionData>(data.conditions)
        };

        database.IslandLevels[key] = levelData;

        // JSON 저장
        string outputJson = JsonUtility.ToJson(database, true);
        Directory.CreateDirectory(Path.GetDirectoryName(SAVE_PATH));
        File.WriteAllText(SAVE_PATH, outputJson);

        Debug.Log($"JSON 저장 완료: {SAVE_PATH}");
        AssetDatabase.Refresh();
    }
}
