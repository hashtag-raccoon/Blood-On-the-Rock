using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CocktailManager : MonoBehaviour
{
    [Header("쉐이커 오브젝트")]
    public GameObject _shaker; // 쉐이커 오브젝트

    [Header("진행률 오브젝트")]
    public GameObject ProgressBar; // 진행바 오브젝트

    [Header("진행률_불꽃 오브젝트와 이미지")]
    public GameObject FireUI;
    public Sprite[] FireImage;

    [HideInInspector]
    public int ShakingNumber = 0; // 쉐이킹 횟수
    [Header("최대 쉐이킹 횟수")]
    [Range(0, 100)]
    public int ShakingMax = 100; // 최대 쉐이킹 횟수

    private void Update()
    {
        /*
        if (ShakingNumber / ShakingMax < 0.3f)
        {
            FireUI.SetActive(false);
        }
        else if (ShakingNumber / ShakingMax >= 0.3f)
        {
            FireUI.GetComponent<Image>().sprite = FireImage[0];
            FireUI.SetActive(true);
        }
        else if (ShakingNumber / ShakingMax >= 0.7f)
        {
            FireUI.GetComponent<Image>().sprite = FireImage[1];
        }
        */

        if (ShakingNumber == 0)
        {
            ProgressBar.SetActive(false);
            ProgressBar.GetComponent<Image>().fillAmount = 0f;
        }
        else
        {
            ProgressBar.SetActive(true);
            ProgressBar.GetComponent<Image>().fillAmount = (float)ShakingNumber / ShakingMax;
        }
    }
}
