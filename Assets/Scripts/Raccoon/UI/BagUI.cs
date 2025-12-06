using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
/// <summary>
/// 가방 UI 관리 클래스
/// 해당 UI에서 인벤토리, 레시피, 알바편집, 퀘스트, 손님도감 UI를 키고 끌 수 있음
/// UI들 종합관리하는 가방 UI 클래스
/// </summary>
public class BagUI : MonoBehaviour, IPointerDownHandler
{
    [Header("가방 UI 이미지")]
    [Tooltip("가방 오픈 시 이미지")]
    public Sprite BagOpenUIImage;
    [Tooltip("가방 닫을 시 이미지")]
    public Sprite BagCloseUIImage;

    [Header("가방 UI 패널")]
    public GameObject BagUIPanel;

    [Header("가방 UI 버튼")]
    public Button InventoryButton;
    public Button RecipeButton;
    [Header("알바 버튼 / 해당 UI")]
    public Button ArbeitBookButton;
    public GameObject ArbeitBookUI;

    [Header("버튼 애니메이션 설정")]
    [SerializeField] private float buttonMoveDistance = 20f; // 버튼이 이동할 거리

    public Button QuestButton;
    public Button CustomerBookButton;

    private bool isBagOpen = false;
    private bool isArbeitEdit = false;

    private void Awake()
    {
        // 초기에는 가방 UI 패널 비활성화
        if (BagUIPanel.activeSelf == true)
        {
            BagUIPanel.SetActive(false);
        }
    }

    private void Start()
    {
        // 버튼 클릭 이벤트 등록
        InventoryButton.onClick.AddListener(OnInventoryButtonClicked);
        RecipeButton.onClick.AddListener(OnRecipeButtonClicked);
        ArbeitBookButton.onClick.AddListener(OnArbeitBookToggle);
        QuestButton.onClick.AddListener(OnQuestButtonClicked);
        CustomerBookButton.onClick.AddListener(OnCustomerBookButtonClicked);

        // 프로토타입 전 쓰지않을 버튼들 비활성화
        InventoryButton.interactable = false;
        RecipeButton.interactable = false;
        QuestButton.interactable = false;
        CustomerBookButton.interactable = false;

        if (ArbeitBookUI.activeSelf == true)
        {
            ArbeitBookUI.SetActive(false);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isBagOpen)
        {
            CloseBagUI();
        }
        else
        {
            OpenBagUI();
        }
    }

    private void OpenBagUI()
    {
        isBagOpen = true;
        BagUIPanel.SetActive(true);
        this.gameObject.GetComponent<Image>().sprite = BagOpenUIImage;
    }

    private void CloseBagUI()
    {
        isBagOpen = false;
        BagUIPanel.SetActive(false);
        this.gameObject.GetComponent<Image>().sprite = BagCloseUIImage;

        if (isArbeitEdit)
        {
            // 알바편집 모드 종료
            isArbeitEdit = false;
        }
        if (ArbeitBookUI.activeSelf == true)
        {
            ArbeitBookUI.SetActive(false);
        }
    }

    private void OnInventoryButtonClicked()
    {
        // 인벤토리 UI 열기 로직 추가
    }

    private void OnRecipeButtonClicked()
    {
        // 레시피 UI 열기 로직 추가
    }

    private void OnArbeitBookToggle()
    {
        if (ArbeitBookUI != null)
        {
            // PageUI 오픈 (도감 모드로 시작하게함)
            PageUI pageUI = ArbeitBookUI.GetComponent<PageUI>();
            if (pageUI.gameObject.activeSelf == false)
            {
                if (pageUI.pageUIObject.activeSelf == false)
                {
                    pageUI.pageUIObject.SetActive(true);
                }
                pageUI.OpenPageUI();
            }
            else
            {
                pageUI.ClosePageUI();
            }
        }
    }

    private void OnQuestButtonClicked()
    {
        // 퀘스트 UI 열기 로직 추가
    }

    private void OnCustomerBookButtonClicked()
    {
        // 손님도감 UI 열기 로직 추가
    }
}