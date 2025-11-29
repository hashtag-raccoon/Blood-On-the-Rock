using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("대화 데이터")]
    public Dictionary<int, DialogueData> dialogueDic = new Dictionary<int, DialogueData>();

    [Header("대화창 UI 할당")]
    public DialogueUI dialogueUI; // UI 프리팹 할당

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        //LoadAllDialogues(); // 모든 대화 데이터 로드
    }

    /*
    /// <summary>
    /// Assets/Dialogue 폴더 내의 모든 CSV 파일을 로드함
    /// 모든 대화 데이터를 한 번에 불러올 수 있음
    /// </summary>
    private void LoadAllDialogues()
    {
        string dialogueFolderPath = Path.Combine(Application.dataPath, "Dialogue");

        if (!Directory.Exists(dialogueFolderPath))
        {
            Debug.LogError($"Dialogue 폴더를 찾을 수 없음: {dialogueFolderPath}");
            return;
        }

        // Dialogue 폴더 내의 모든 CSV 파일 검색
        string[] csvFiles = Directory.GetFiles(dialogueFolderPath, "*.csv");

        if (csvFiles.Length == 0)
        {
            Debug.LogWarning("Dialogue 폴더에 CSV 파일이 없습니다.");
            return;
        }

        Debug.Log($"{csvFiles.Length}개의 CSV 파일 발견, 로드 시작...");

        // 각 CSV 파일을 순차적으로 로드
        foreach (string csvFilePath in csvFiles)
        {
            string fileName = Path.GetFileName(csvFilePath);
            LoadDialogue(fileName);
        }

        Debug.Log($"총 {dialogueDic.Count}개의 대화 데이터가 로드됨");
    }
    */

    /// <summary>
    /// CSV 파일에서 대화 데이터를 로드함
    /// 처음에 한 번 호출하여 모든 대화 데이터를 불러옴
    /// </summary>
    /// <param name="csvFileName">대화 들어있는 csv 파일 이름</param>
    public void LoadDialogue(string csvFileName)
    {
        dialogueDic.Clear();

        // 대화 CSV 파일 경로를 반환함
        // 경로는 지금 Assets/Dialogue/ 폴더 안에 있다고 가정함
        // CSV 확장자가 없으면 자동으로 추가
        if (!csvFileName.EndsWith(".csv"))
        {
            csvFileName += ".csv";
        }

        string path = Path.Combine(Application.dataPath, "Dialogue", csvFileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"[DialogueSystem] CSV 파일이 없음: {path}"); // 디버깅용, 당분간 없으면 곤란함
            return;
        }

        /// <summary>
        /// CSV 파일에서 모든 대화 데이터를 읽어옴
        /// </summary>
        /// <returns></returns>
        string[] lines = File.ReadAllLines(path);

        /// <summary>
        /// 각 행을 DialogueData 객체로 파싱하여 딕셔너리에 추가함
        /// 헤더, 설명, 주석 행은 자동으로 건너뜀 (첫 번째 열이 숫자가 아닌 경우)
        /// </summary>
        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] data = line.Split(',');

            // 총 13개의 열이 있는데, 해당 열이 부족할 경우 건너뜀
            if (data.Length < 13)
            {
                continue; // 데이터 부족한 행은 건너뜀 (헤더나 빈 행일 가능성)
            }

            // 첫 번째 열(ID)이 숫자가 아니면 헤더나 설명 행이므로 건너뜀
            if (!int.TryParse(data[0], out int id))
            {
                continue;
            }

            // 데이터 찾아서 불러옴, 편의를 위해 빈 데이터는 -1로 처리함
            try
            {
                int index = int.Parse(data[1]);
                string name = data[2];
                string context = data[3];
                string portrait = data[4];
                int nextIndex = string.IsNullOrWhiteSpace(data[5]) ? -1 : int.Parse(data[5]);

                string eventName = data[6];
                string choiceA_Text = data[7];
                int choiceA_Next = string.IsNullOrWhiteSpace(data[8]) ? -1 : int.Parse(data[8]);

                string choiceB_Text = data[9];
                int choiceB_Next = string.IsNullOrWhiteSpace(data[10]) ? -1 : int.Parse(data[10]);

                string choiceC_Text = data[11];
                int choiceC_Next = string.IsNullOrWhiteSpace(data[12]) ? -1 : int.Parse(data[12]);

                // DialogueData 객체 생성 및 딕셔너리에 추가
                DialogueData dialogue = new DialogueData(id, index, name, context, portrait, nextIndex, eventName,
                    choiceA_Text, choiceA_Next, choiceB_Text, choiceB_Next, choiceC_Text, choiceC_Next);

                if (!dialogueDic.ContainsKey(index))
                {
                    dialogueDic.Add(index, dialogue);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"잘못된 데이터 형식: Line{i}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Index에 해당하는 대화 데이터를 반환함
    /// </summary>
    /// <param name="id">대화 딕셔너리 중 n번째의 키(Index)</param>
    public DialogueData GetDialogue(int id)
    {
        if (dialogueDic.ContainsKey(id))
        {
            return dialogueDic[id];
        }
        return null;
    }
    /// <summary>
    /// Index에 해당하는 대화를 시작함
    /// </summary>
    /// <param name="id">대화 인덱스</param>
    public void StartDialogue(int id)
    {
        Debug.Log("대화를 시작합니다. ID: " + id);
        if (dialogueUI != null)
        {
            if (dialogueDic.Count == 0)
            {
                Debug.LogError("대화 데이터를 먼저 로드해야 합니다. LoadDialogue()를 호출하세요."); // 디버깅용
                // 혹시라도 이 로그가 호출 시, 대화 데이터가 정상적으로 로드되지 않은 상태임을 의미함
                // 즉 LoadDialouge()가 제대로 되었는지 확인할 것
                return;
            }
            dialogueUI.StartDialogue(id); // 대화 UI에 대화 시작 요청
        }
        else
        {
            Debug.LogError("대화창 UI가 현재 할당되지 않았음");
        }
    }

    public void StartDialogue(int id, Vector2? panelSize = null)
    {
        Debug.Log("대화를 시작합니다. ID: " + id);
        if (dialogueUI != null)
        {
            if (dialogueDic.Count == 0)
            {
                Debug.LogError("대화 데이터를 먼저 로드해야 합니다. LoadDialogue()를 호출하세요."); // 디버깅용
                // 혹시라도 이 로그가 호출 시, 대화 데이터가 정상적으로 로드되지 않은 상태임을 의미함
                // 즉 LoadDialouge()가 제대로 되었는지 확인할 것
                return;
            }
            dialogueUI.StartDialogue(id, panelSize); // 대화 UI에 대화 시작 요청
        }
        else
        {
            Debug.LogError("대화창 UI가 현재 할당되지 않았음");
        }
    }
}
