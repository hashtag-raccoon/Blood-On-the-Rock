using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueData
{
    public int ID; // 대화의 고유 ID
    public int Index; // 현재 대화의 인덱스
    public string Name;
    public string Context;
    public string Portrait;
    public int NextIndex; // 다음 대화의 인덱스
    public string EventName;

    public string ChoiceA_Text;
    public int ChoiceA_Next;
    public string ChoiceB_Text;
    public int ChoiceB_Next;
    public string ChoiceC_Text;
    public int ChoiceC_Next;

    /// <summary>
    /// DialogueData 클래스의 생성자
    /// </summary>
    /// <param name="index">현재 대화의 인덱스</param>
    /// <param name="name">화자의 이름</param>
    /// <param name="context">대화 내용</param>
    /// <param name="portrait">화자의 초상화</param>
    /// <param name="nextIndex">다음 대화의 인덱스</param>
    /// <param name="eventName">이벤트로 호출될 메소드 이름</param>
    /// <param name="choiceA_Text">선택지 A 내용</param>
    /// <param name="choiceA_Next">선택지 A 다음 인덱스</param>
    /// <param name="choiceB_Text">선택지 B 내용</param>
    /// <param name="choiceB_Next">선택지 B 다음 인덱스</param>
    /// <param name="choiceC_Text">선택지 C 내용</param>
    /// <param name="choiceC_Next">선택지 C 다음 인덱스</param>
    public DialogueData(int index, string name, string context, string portrait, int nextIndex, string eventName,
                        string choiceA_Text, int choiceA_Next, string choiceB_Text, int choiceB_Next, string choiceC_Text, int choiceC_Next)
    {
        Index = index;
        Name = name;
        Context = context;
        Portrait = portrait;
        NextIndex = nextIndex;
        EventName = eventName;
        ChoiceA_Text = choiceA_Text;
        ChoiceA_Next = choiceA_Next;
        ChoiceB_Text = choiceB_Text;
        ChoiceB_Next = choiceB_Next;
        ChoiceC_Text = choiceC_Text;
        ChoiceC_Next = choiceC_Next;
    }
}
