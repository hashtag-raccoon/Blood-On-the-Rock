using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class IsometricObjectAligner : EditorWindow
{
    private Tilemap referenceTilemap;
    private GameObject targetObject;
    private bool adjustSortingOrder = false;

    [MenuItem("Tools/Isometric Object Aligner")]
    public static void ShowWindow()
    {
        GetWindow<IsometricObjectAligner>("Isometric Aligner");
    }

    void OnGUI()
    {
        GUILayout.Label("Isometric Z as Y Object Aligner", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        referenceTilemap = (Tilemap)EditorGUILayout.ObjectField(
            "Reference Tilemap",
            referenceTilemap,
            typeof(Tilemap),
            true
        );

        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target Object",
            targetObject,
            typeof(GameObject),
            true
        );

        EditorGUILayout.Space();

        adjustSortingOrder = EditorGUILayout.Toggle("Auto Sorting Order", adjustSortingOrder);

        if (adjustSortingOrder)
        {
            EditorGUILayout.HelpBox("Sorting Order를 Z 좌표 기반으로 자동 조정합니다.", MessageType.Info);
        }

        EditorGUILayout.Space();

        GUI.enabled = targetObject != null && referenceTilemap != null;
        if (GUILayout.Button("Align Selected Object", GUILayout.Height(30)))
        {
            AlignObject(targetObject);
        }

        EditorGUILayout.Space();

        GUI.enabled = Selection.gameObjects.Length > 0 && referenceTilemap != null;
        if (GUILayout.Button("Align All Selected Objects", GUILayout.Height(30)))
        {
            AlignMultipleObjects(Selection.gameObjects);
        }

        GUI.enabled = true;

        EditorGUILayout.Space();

        if (referenceTilemap != null)
        {
            Grid grid = referenceTilemap.layoutGrid;
            EditorGUILayout.HelpBox(
                $"타일맵 정보:\n" +
                $"Cell Size: {grid.cellSize}\n" +
                $"Cell Layout: {grid.cellLayout}\n" +
                $"Tilemap Y: {referenceTilemap.transform.position.y}",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "Reference Tilemap을 먼저 설정해주세요!",
                MessageType.Warning
            );
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "사용법:\n" +
            "1. Reference Tilemap 설정 (필수)\n" +
            "2. 정렬할 오브젝트 선택\n" +
            "3. Align 버튼 클릭\n\n" +
            "단축키: Alt+A",
            MessageType.Info
        );
    }

    void AlignObject(GameObject obj)
    {
        if (obj == null)
        {
            EditorUtility.DisplayDialog("Error", "오브젝트를 선택해주세요.", "OK");
            return;
        }

        if (referenceTilemap == null)
        {
            EditorUtility.DisplayDialog("Error", "Reference Tilemap을 설정해주세요.", "OK");
            return;
        }

        Undo.RecordObject(obj.transform, "Align to Isometric Grid");

        Vector3 worldPos = obj.transform.position;
        Grid grid = referenceTilemap.layoutGrid;
        Transform tilemapTransform = referenceTilemap.transform;

        // 월드 좌표를 그리드의 로컬 좌표로 변환
        Vector3 localPos = tilemapTransform.InverseTransformPoint(worldPos);

        // 로컬 좌표를 셀 좌표로 변환
        Vector3Int cellPos = grid.LocalToCell(localPos);

        // 셀 좌표를 다시 로컬 좌표로 변환 (정확한 그리드 위치)
        Vector3 snappedLocalPos = grid.CellToLocal(cellPos);

        // 로컬 좌표를 월드 좌표로 변환
        Vector3 snappedWorldPos = tilemapTransform.TransformPoint(snappedLocalPos);

        obj.transform.position = snappedWorldPos;

        // Sorting Order 조정 (옵션)
        if (adjustSortingOrder)
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // Y 좌표 기반 정렬 (Isometric Z as Y에서는 Y가 깊이를 나타냄)
                int newOrder = Mathf.RoundToInt(-snappedWorldPos.y * 100);
                Undo.RecordObject(sr, "Adjust Sorting Order");
                sr.sortingOrder = newOrder;
            }
        }

        Debug.Log($"Aligned {obj.name} to cell {cellPos} at world position {snappedWorldPos}");
        EditorUtility.SetDirty(obj);
    }

    void AlignMultipleObjects(GameObject[] objects)
    {
        if (objects == null || objects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "오브젝트를 선택해주세요.", "OK");
            return;
        }

        if (referenceTilemap == null)
        {
            EditorUtility.DisplayDialog("Error", "Reference Tilemap을 설정해주세요.", "OK");
            return;
        }

        foreach (GameObject obj in objects)
        {
            AlignObject(obj);
        }

        Debug.Log($"{objects.Length}개의 오브젝트가 정렬되었습니다.");
    }

    void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            targetObject = Selection.activeGameObject;
            Repaint();
        }
    }
}

// Scene View에서 직접 정렬할 수 있는 단축키
public class IsometricObjectAlignerShortcut
{
    [MenuItem("Edit/Align to Isometric Grid &a")]
    static void AlignSelectedObjects()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("정렬할 오브젝트를 선택해주세요.");
            return;
        }

        Tilemap tilemap = Object.FindObjectOfType<Tilemap>();

        if (tilemap == null)
        {
            EditorUtility.DisplayDialog("Error", "씬에 Tilemap이 없습니다.", "OK");
            return;
        }

        Grid grid = tilemap.layoutGrid;
        Transform tilemapTransform = tilemap.transform;

        foreach (GameObject obj in Selection.gameObjects)
        {
            Undo.RecordObject(obj.transform, "Align to Isometric Grid");

            Vector3 worldPos = obj.transform.position;

            // 월드 → 로컬 → 셀 → 로컬 → 월드 변환
            Vector3 localPos = tilemapTransform.InverseTransformPoint(worldPos);
            Vector3Int cellPos = grid.LocalToCell(localPos);
            Vector3 snappedLocalPos = grid.CellToLocal(cellPos);
            Vector3 snappedWorldPos = tilemapTransform.TransformPoint(snappedLocalPos);

            obj.transform.position = snappedWorldPos;
        }

        Debug.Log($"{Selection.gameObjects.Length}개의 오브젝트가 정렬되었습니다.");
    }
}