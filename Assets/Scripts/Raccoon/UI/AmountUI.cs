using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AmountUI : MonoBehaviour
{
    // 재료 표시할 때 필요한 UI 스크립트
    [Header("재료 데이터")]
    [SerializeField]  private goodsData goodsdata;
    private int Amount;
    [Header("UI 오브젝트/아이콘 스프라이트")]
    [SerializeField] private Image goodsSprite;
    [Header("UI 오브젝트/텍스트")]
    [SerializeField] private TextMeshProUGUI Text;

    void Start()
    {
        goodsSprite.sprite = goodsdata.icon;
        Amount = goodsdata.amount;
        this.transform.SetAsFirstSibling();
    }

    void Update()
    {
        Text.text = Amount.ToString();
    }
}
