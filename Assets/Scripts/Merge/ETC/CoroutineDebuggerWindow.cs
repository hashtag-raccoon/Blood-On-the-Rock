using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

public class CoroutineDebuggerWindow : EditorWindow
{
    private Vector2 scrollPosition;
    private bool autoRefresh = true;
    private double lastUpdateTime;
    private float refreshInterval = 0.5f;

    private List<CoroutineInfo> coroutineInfos = new List<CoroutineInfo>();

    [System.Serializable]
    private class CoroutineInfo
    {
        public string objectName;
        public string coroutineName;
        public MonoBehaviour owner;
        public int instanceId;
        public Coroutine coroutine;
        public float elapsedTime;
        public float totalWaitTime;
        public bool isWaiting;
    }

    [MenuItem("Tools/Coroutine Debugger")]
    public static void ShowWindow()
    {
        GetWindow<CoroutineDebuggerWindow>("Coroutine Debugger");
    }

    void OnEnable()
    {
        lastUpdateTime = EditorApplication.timeSinceStartup;
        EditorApplication.update += OnEditorUpdate;
    }

    void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
    }

    void OnEditorUpdate()
    {
        if (autoRefresh && EditorApplication.timeSinceStartup - lastUpdateTime > refreshInterval)
        {
            ScanCoroutines();
            UpdateElapsedTimes();
            lastUpdateTime = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    void OnGUI()
    {
        DrawToolbar();
        DrawCoroutineList();
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        if (GUILayout.Button("새로고침", EditorStyles.toolbarButton, GUILayout.Width(80)))
        {
            ScanCoroutines();
        }

        autoRefresh = GUILayout.Toggle(autoRefresh, "자동 새로고침", EditorStyles.toolbarButton, GUILayout.Width(100));

        GUILayout.Label("갱신 주기:", EditorStyles.miniLabel, GUILayout.Width(60));
        refreshInterval = EditorGUILayout.Slider(refreshInterval, 0.1f, 2f, GUILayout.Width(150));

        GUILayout.FlexibleSpace();

        GUILayout.Label($"실행 중인 코루틴: {coroutineInfos.Count}", EditorStyles.miniLabel);

        EditorGUILayout.EndHorizontal();
    }

    void DrawCoroutineList()
    {
        if (coroutineInfos.Count == 0)
        {
            EditorGUILayout.HelpBox("실행 중인 코루틴이 없습니다.", MessageType.Info);
            return;
        }

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.BeginVertical();

        foreach (var info in coroutineInfos)
        {
            EditorGUILayout.BeginHorizontal("box");

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("GameObject:", info.objectName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Coroutine:", info.coroutineName);
            EditorGUILayout.LabelField("Instance ID:", info.instanceId.ToString());

            if (info.isWaiting && info.totalWaitTime > 0)
            {
                EditorGUILayout.LabelField("대기 시간:", $"{info.elapsedTime:F1}초 / {info.totalWaitTime:F1}초");

                // 진행률 바
                Rect rect = EditorGUILayout.GetControlRect(false, 20);
                float progress = Mathf.Clamp01(info.elapsedTime / info.totalWaitTime);
                EditorGUI.ProgressBar(rect, progress, $"{(progress * 100):F0}%");
            }
            else
            {
                EditorGUILayout.LabelField("상태:", "실행 중");
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUILayout.Width(80));

            if (info.owner != null)
            {
                if (GUILayout.Button("선택", GUILayout.Height(30)))
                {
                    Selection.activeGameObject = info.owner.gameObject;
                    EditorGUIUtility.PingObject(info.owner.gameObject);
                }

                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Stop", GUILayout.Height(30)))
                {
                    StopCoroutine(info);
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    void UpdateElapsedTimes()
    {
        foreach (var info in coroutineInfos)
        {
            if (info.isWaiting)
            {
                info.elapsedTime += refreshInterval;

                // 대기 시간 초과 시 리셋
                if (info.elapsedTime >= info.totalWaitTime)
                {
                    info.isWaiting = false;
                    info.elapsedTime = 0;
                }
            }
        }
    }

    void StopCoroutine(CoroutineInfo info)
    {
        if (info.owner != null && info.coroutine != null)
        {
            info.owner.StopCoroutine(info.coroutine);

            // 필드 값도 null로 설정
            FieldInfo field = info.owner.GetType().GetField(
                info.coroutineName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (field != null)
            {
                field.SetValue(info.owner, null);
            }

            Debug.Log($"코루틴 '{info.coroutineName}' (GameObject: {info.objectName})이 중지되었습니다.");

            // 리스트에서 제거
            coroutineInfos.Remove(info);
            Repaint();
        }
    }

    void ScanCoroutines()
    {
        // 기존 정보 유지를 위한 딕셔너리
        Dictionary<string, CoroutineInfo> existingInfos = new Dictionary<string, CoroutineInfo>();
        foreach (var info in coroutineInfos)
        {
            string key = $"{info.instanceId}_{info.coroutineName}";
            existingInfos[key] = info;
        }

        coroutineInfos.Clear();

        if (!Application.isPlaying)
        {
            return;
        }

        MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();

        foreach (MonoBehaviour mb in allMonoBehaviours)
        {
            if (mb == null) continue;

            FieldInfo[] fields = mb.GetType().GetFields(
                BindingFlags.Instance |
                BindingFlags.NonPublic |
                BindingFlags.Public
            );

            foreach (FieldInfo field in fields)
            {
                if (field.FieldType == typeof(Coroutine))
                {
                    Coroutine coroutine = field.GetValue(mb) as Coroutine;

                    if (coroutine != null)
                    {
                        string key = $"{mb.GetInstanceID()}_{field.Name}";
                        CoroutineInfo info;

                        // 기존 정보가 있으면 재사용
                        if (existingInfos.ContainsKey(key))
                        {
                            info = existingInfos[key];
                        }
                        else
                        {
                            info = new CoroutineInfo
                            {
                                objectName = mb.gameObject.name,
                                coroutineName = field.Name,
                                owner = mb,
                                instanceId = mb.GetInstanceID(),
                                coroutine = coroutine,
                                elapsedTime = 0,
                                totalWaitTime = 0,
                                isWaiting = false
                            };

                            // WaitForSeconds 정보 추출 시도
                            ExtractWaitTime(mb, field.Name, info);
                        }

                        coroutineInfos.Add(info);
                    }
                }
                else if (field.FieldType == typeof(List<Coroutine>))
                {
                    List<Coroutine> coroutineList = field.GetValue(mb) as List<Coroutine>;

                    if (coroutineList != null)
                    {
                        for (int i = 0; i < coroutineList.Count; i++)
                        {
                            if (coroutineList[i] != null)
                            {
                                string coroutineName = $"{field.Name}[{i}]";
                                string key = $"{mb.GetInstanceID()}_{coroutineName}";
                                CoroutineInfo info;

                                if (existingInfos.ContainsKey(key))
                                {
                                    info = existingInfos[key];
                                }
                                else
                                {
                                    info = new CoroutineInfo
                                    {
                                        objectName = mb.gameObject.name,
                                        coroutineName = coroutineName,
                                        owner = mb,
                                        instanceId = mb.GetInstanceID(),
                                        coroutine = coroutineList[i],
                                        elapsedTime = 0,
                                        totalWaitTime = 0,
                                        isWaiting = false
                                    };
                                }

                                coroutineInfos.Add(info);
                            }
                        }
                    }
                }
            }
        }
    }

    void ExtractWaitTime(MonoBehaviour mb, string coroutineName, CoroutineInfo info)
    {
        // 모든 float 필드 검색
        FieldInfo[] allFields = mb.GetType().GetFields(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        // "wait_" 로 시작하는 float 필드만 검색
        foreach (var timeField in allFields)
        {
            if (timeField.FieldType == typeof(float))
            {
                // wait_로 시작하는지 확인 (대소문자 구분 없이)
                if (timeField.Name.ToLower().StartsWith("wait_"))
                {
                    float waitTime = (float)timeField.GetValue(mb);
                    if (waitTime > 0)
                    {
                        info.totalWaitTime = waitTime;
                        info.isWaiting = true;
                        return;
                    }
                }
            }
        }
    }
}