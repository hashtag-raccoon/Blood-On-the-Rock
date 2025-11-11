using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUIScripts : MonoBehaviour
{
    [Header("UI 컴포넌트")]
    public GameObject upgradeUIBuildingImage;
    public GameObject upgradeButtonCostImage;
    public TextMeshProUGUI  upgradeButtonCostText;
    public TextMeshProUGUI  upgradeRequiredTimeText;
    public GameObject upgradeRequiredTimePanel;
    public GameObject upgradeRequiredPanel;
    public TextMeshProUGUI  BuildingName;
    public TextMeshProUGUI  CurrentBuildingLevel;
    public TextMeshProUGUI  NextBuildingLevel;
    public Button UpgradeButton;
    public GameObject upgradeButtonCostIcon;
    public TextMeshProUGUI  UpgradeConsumeText;
    public Button upgradeUICloseButton;

    [Header("동적 생성 설정")]
    public Transform requirementContainer;

    [Header("레이아웃 설정")]
    public float panelSpacing = 10f; // 패널 간 간격
    public float gapBeforeTimePanel = 50f; // 요구조건이 2개 이하일때
    public Vector2 panelSize = new Vector2(100, 120); // 패널 크기
    public Vector2 iconSize = new Vector2(60, 60); // 아이콘 크기
    public float iconYOffset = 20f; // 아이콘 Y 오프셋 (패널 내 상단 배치)
    public float textYOffset = 10f; // 텍스트 Y 오프셋 (패널 내 하단 배치)
    public int textFontSize = 20; // 텍스트 폰트 크기
    
    [Header("패널 이미지 설정")]
    public Sprite panelBackgroundSprite; // 패널 배경 이미지,당분간은 없어서 투명하게 나올 거임
    public Color panelColor = new Color(1f, 1f, 1f, 1f); // 패널 이미지 색상, 임시
    
    [Header("텍스트 폰트 설정")]
    public TMP_FontAsset textFont;
    public Color textColor = Color.black;
    private const int MAX_REQUIREMENTS = 3; // 최대 요구조건 개수
    
    public BuildingData buildingData;
    public BuildingUpgradeData buildingUpgradeData;
    private List<GameObject> createdRequirementPanels = new List<GameObject>();
    private List<Image> createdPanelImages = new List<Image>(); 
    private List<Image> createdIconImages = new List<Image>(); 
    private List<TextMeshProUGUI> createdTexts = new List<TextMeshProUGUI>();
    void Start()
    {
        upgradeUICloseButton.onClick.AddListener(() =>
        {
            upgradeClick();
        });

        // 초기 데이터 설정 시 requirements UI 생성
        if (buildingData != null && buildingUpgradeData != null)
        {
            CreateRequirementPanels();
        }
    }

    void Update()
    {
        
    }
    
    public void SetData(BuildingData buildingData)
    {
        this.buildingData = buildingData;
        
        // UI 업데이트
        UpdateBuildingUI();
    }
    
    private void UpdateBuildingUI()
    {
        if (buildingData == null)
        {
            return;
        }

        if (upgradeUIBuildingImage != null)
        {
            Image img = upgradeUIBuildingImage.GetComponent<Image>();
            if (img != null && buildingData.icon != null)
            {
                img.sprite = buildingData.icon;
            }
        }
        
        if (BuildingName != null)
        {
            BuildingName.text = buildingData.BuildingName;
        }
        
        if (CurrentBuildingLevel != null)
        {
            CurrentBuildingLevel.text = "레벨" + buildingData.level.ToString();
        }
        
        if (NextBuildingLevel != null)
        {
            NextBuildingLevel.text = "레벨" + (buildingData.level + 1).ToString();
        }

        if (UpgradeButton != null)
        {
            if (upgradeButtonCostIcon != null && buildingUpgradeData != null)
            {
                Image costIconImage = upgradeButtonCostIcon.GetComponent<Image>();
                goodsData moneyData = DataManager.Instance?.GetResourceByName("Money");
                
                if (costIconImage != null && moneyData != null && moneyData.icon != null)
                {
                    costIconImage.sprite = moneyData.icon;
                }
                
                if (upgradeButtonCostText != null)
                {
                    upgradeButtonCostText.text = buildingUpgradeData.upgrade_price.ToString();
                }
            }
            
            UpgradeButton.onClick.RemoveAllListeners();
            UpgradeButton.onClick.AddListener(() =>
            {
                upgradeClick();
            });
        }
    }
    
    public void SetUpgradeData(BuildingUpgradeData upgradeData)
    {
        this.buildingUpgradeData = upgradeData;
        
        if (upgradeData != null && upgradeData.requirements != null && upgradeData.requirements.Count > 0)
        {
            CreateRequirementPanels();
        }
        
        UpdateBuildingUI();
    }

    private void CreateRequirementPanels()
    {
        
        ClearRequirementPanels();

        int requirementCount = buildingUpgradeData.requirements.Count;

        // 패널 크기 설정
        RectTransform timePanelRect = upgradeRequiredTimePanel?.GetComponent<RectTransform>();
        if (timePanelRect != null)
        {
            panelSize = timePanelRect.sizeDelta;
        }
        
        float totalMinutes = buildingUpgradeData.base_upgrade_time_minutes;
        
        int minutes = Mathf.FloorToInt(totalMinutes);
        int seconds = Mathf.FloorToInt((totalMinutes - minutes) * 60f);
        upgradeRequiredTimeText.text = $"{minutes}분 {seconds}초";

        // 요구조건이 3개 미만일 때의 오프셋 계산 (가운데 정렬용)
        float totalWidth = 0f;
        float startXOffset = 0f;
        
        // 전체 너비 계산
        totalWidth = (panelSize.x * requirementCount) + (panelSpacing * (requirementCount - 1));
        
        // 가운데 정렬용 오프셋
        startXOffset = -totalWidth / 2f;
        
        // requirements 리스트만큼 UI 생성
        for (int i = 0; i < requirementCount; i++)
        {
            upgrade_requirements requirement = buildingUpgradeData.requirements[i];
            
            GameObject panelObj = new GameObject($"RequirementPanel_{i}");
            panelObj.transform.SetParent(requirementContainer, false);
            
            // RectTransform 추가 및 설정
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.sizeDelta = panelSize;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f); // 중앙 기준
            panelRect.anchorMax = new Vector2(0.5f, 0.5f); // 중앙 기준
            panelRect.pivot = new Vector2(0, 0.5f); // 왼쪽 중앙 피벗
            
            // 위치 설정(가운데 정렬)
            float xPos = startXOffset + (i * (panelSize.x + panelSpacing));
            panelRect.anchoredPosition = new Vector2(xPos, 0);
            
            // Image 컴포넌트 추가
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.sprite = panelBackgroundSprite; // 인스펙터에서 할당한 이미지 사용
            panelImage.color = panelColor;
            panelImage.type = Image.Type.Sliced; // 9-slice 적용
            
            GameObject iconObj = new GameObject($"RequirementIcon_{i}");
            iconObj.transform.SetParent(panelObj.transform, false);
            
            // RectTransform 설정
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = iconSize;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0, iconYOffset);
            
            // 아이콘 추가
            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.color = Color.white;
            
            // 자원 데이터로 아이콘 설정
            if (requirement != null)
            {
                goodsData resourceData = DataManager.Instance.GetResourceByName(requirement.requirement_type);
                
                if (resourceData != null && resourceData.icon != null)
                {
                    iconImage.sprite = resourceData.icon;
                }
            }

            GameObject textObj = new GameObject($"RequirementText_{i}");
            textObj.transform.SetParent(panelObj.transform, false);
            
            // RectTransform 설정
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0);
            textRect.pivot = new Vector2(0.5f, 0);
            textRect.anchoredPosition = new Vector2(0, textYOffset);
            textRect.sizeDelta = new Vector2(0, 30);
            
            TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
            tmpText.text = requirement != null ? requirement.requirement_value.ToString() : "0";
            tmpText.font = textFont;
            tmpText.fontSize = textFontSize;
            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.color = textColor;
            
            createdRequirementPanels.Add(panelObj);
            createdPanelImages.Add(panelImage);
            createdIconImages.Add(iconImage);
            createdTexts.Add(tmpText);
        }
    }
    
    private void ClearRequirementPanels()
    {
        foreach (GameObject panel in createdRequirementPanels)
        {
            if (panel != null)
            {
                Destroy(panel);
            }
        }
        createdRequirementPanels.Clear();
    }

    private void OnDestroy()
    {
        ClearRequirementPanels();
    }
    
    private void upgradeClick()
    {
        goodsData consumeResource = DataManager.Instance.GetResourceByName("Money");
        if (buildingUpgradeData.upgrade_price <= consumeResource.amount)
        {
            for(int i = 0; i < buildingUpgradeData.requirements.Count; i++)
            {
                upgrade_requirements req = buildingUpgradeData.requirements[i];
                goodsData reqResource = DataManager.Instance.GetResourceByName(req.requirement_type);
                if (reqResource != null)
                {
                    reqResource.amount -= req.requirement_value;
                }
            }
            consumeResource.amount -= buildingUpgradeData.upgrade_price;
            buildingData.level += 1;
            Destroy(this.gameObject);
        }
        else
        {
            Debug.Log("돈이 부족합니다.");
        }
    }
}
