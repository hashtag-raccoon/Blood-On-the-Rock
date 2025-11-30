using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class JobCenterButtonUI : MonoBehaviour, IScrollItemUI
{
    [Header("UI 설정")]
    [SerializeField] private Image ArbeitImage;
    [SerializeField] private TextMeshProUGUI ArbeitNameText;
    [SerializeField] private TextMeshProUGUI ArbeitPersonalityText;
    [SerializeField] private TextMeshProUGUI ArbeitMoneyText;
    [SerializeField] private Image ServingAbilityBar;
    [SerializeField] private Image CookAbilityBar;
    [SerializeField] private Image CleaningAbilityBar;
    [SerializeField] private Button OfferButton;

    private object npc;

    public void SetData<T>(T data, Action<IScrollItemUI> onClickCallback) where T : IScrollItemData
    {
        npc = data;

        var npcData = data as npc;

        ArbeitNameText.text = npcData.part_timer_name;
        ArbeitMoneyText.text = npcData.daily_wage.ToString();
        //TODO : 여러가지 컴포넌트들 연결

        OfferButton.onClick.AddListener(() =>
        {
            onClickCallback?.Invoke(this);
        });
    }

    public T GetData<T>() where T : IScrollItemData
    {
        return (T)npc;
    }

}
