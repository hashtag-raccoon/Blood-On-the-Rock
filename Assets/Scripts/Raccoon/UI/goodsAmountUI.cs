using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class goodsAmountUI : MonoBehaviour
{
    [Header("재료 데이터")]
    [SerializeField]  private goodsData goodsdata;
    private int goodsAmount;
    [Header("UI 오브젝트/아이콘 스프라이트")]
    [SerializeField] private Image goodsSprite;
    [Header("UI 오브젝트/텍스트")]
    [SerializeField] private TextMeshProUGUI goodsText;
    // Start is called before the first frame update
    void Start()
    {
        goodsSprite.sprite = goodsdata.icon;
        goodsAmount = goodsdata.amount;
    }

    // Update is called once per frame
    void Update()
    {
        goodsText.text = "" + goodsAmount;
    }
}
