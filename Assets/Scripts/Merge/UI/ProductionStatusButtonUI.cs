using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProductionStatusButtonUI : MonoBehaviour
{
    [Header("UI 설정")]
    public Button button;
    public Image iconImage;
    public TextMeshProUGUI timeText;
    
    [Header("데이터")]
    public BuildingProductionInfo productionData;
    public goodsData resourceData;
    public bool isActive;

    protected virtual void Awake()
    {
        button = GetComponent<Button>();
    }

    // 데이터 설정
    public virtual void SetData(BuildingProductionInfo data, goodsData resource)
    {
        productionData = data;
        resourceData = resource;
        // 활성화 상태 설정, data가 null이 아니면 활성화
        isActive = data != null;
    }
    
    // 자원 생산 진행도 업데이트
    public virtual void UpdateProgress(float progress, float remainingTime)
    {
        if (timeText != null)
        {
            timeText.text = FormatTime(remainingTime);
        }
    }

    // 시간 설정 (분:초)
    private string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public virtual void ClearData()
    {
        productionData = null;
        resourceData = null;
        isActive = false;
        
        if (button != null)
        {
            button.interactable = false;
        }
        
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        
        if (timeText != null)
        {
            timeText.text = "";
        }
    }
}