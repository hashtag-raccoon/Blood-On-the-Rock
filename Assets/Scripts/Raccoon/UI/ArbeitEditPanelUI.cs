using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 알바 배치 모드의 단일 패널 UI
/// 배치된 알바생, 편집 중인 알바생, 빈 패널 상태를 처리함
/// - 배치된 알바생: 초상화 + 배치 버튼 (교체 가능)
/// - 배치 중인 알바생: 초상화 + 확인/취소 버튼
/// - 빈 패널: 배치 버튼만 표시
/// - 비활성화된 패널: 최대 배치 수 초과 시 모든 버튼 비활성화
/// - 각 상태에 따라 UI 요소 활성화/비활성화 처리
/// 등의 기능 포함한 클래스
/// </summary>
public class ArbeitEditPanelUI : MonoBehaviour
{
    [Header("NPC 초상화")]
    [SerializeField] private Image portraitImage; // 초상화

    [Header("버튼")]
    [SerializeField] private Button deployButton; // 배치 버튼 (빈 패널일때 나옴)
    [SerializeField] private Button confirmButton; // 확인 버튼 (배치모드일때 나옴)
    [SerializeField] private Button cancelButton; // 취소 버튼 (배치모드일때 나옴)

    [Header("배치된 알바생 정보 (선택사항)")]
    [SerializeField] private GameObject deployedNpcInfoPanel; // 배치된 알바생 정보 패널
    [SerializeField] private TextMeshProUGUI deployedNameText; // 이름

    private npc currentNpc; // 현재 편집 중인 알바생
    private PageUI parentPageUI;
    private PanelState currentState; // 현재 패널 상태
    private int panelIndex = -1; // 이 패널의 슬롯 인덱스

    private enum PanelState // 패널 상태
    {
        Empty,      // 빈 패널
        Editing,    // 편집 중 (CurrentEditingNPC가 있는 경우)
        Deployed,   // 배치됨 (is_deployed = true 인 경우)
        Disabled    // 비활성화 (최대 배치 수 초과한 경우)
    }

    private void Awake()
    {
        // 버튼 리스너 등록
        if (deployButton != null)
            deployButton.onClick.AddListener(OnDeployButtonClicked);
        if (confirmButton != null)
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    #region 패널 상태 설정
    /// <summary>
    /// 빈 패널 상태로 설정
    /// </summary>
    public void SetEmptyPanel(PageUI pageUI)
    {
        parentPageUI = pageUI;
        currentNpc = null;
        currentState = PanelState.Empty;
        panelIndex = -1;

        UpdatePanelUI();
    }

    /// <summary>
    /// 비활성화된 패널 상태로 설정 (최대 배치 수 초과)
    /// </summary>
    public void SetDisabledPanel(PageUI pageUI)
    {
        parentPageUI = pageUI;
        currentNpc = null;
        currentState = PanelState.Disabled;
        panelIndex = -1;

        UpdatePanelUI();
    }

    /// <summary>
    /// 편집 중인 NPC 설정
    /// </summary>
    public void SetEditingNpc(npc npcData, PageUI pageUI)
    {
        parentPageUI = pageUI;
        currentNpc = npcData;
        currentState = PanelState.Editing;

        UpdatePanelUI();
    }

    /// <summary>
    /// 배치된 NPC 설정
    /// </summary>
    public void SetDeployedNpc(npc npcData, PageUI pageUI, int index = -1)
    {
        parentPageUI = pageUI;
        currentNpc = npcData;
        currentState = PanelState.Deployed;
        panelIndex = index;

        UpdatePanelUI();
    }
    #endregion

    /// <summary>
    /// 패널 UI 업데이트
    /// </summary>
    private void UpdatePanelUI()
    {
        switch (currentState)
        {
            case PanelState.Empty:
                ShowEmptyPanel();
                break;
            case PanelState.Editing:
                ShowEditingPanel();
                break;
            case PanelState.Deployed:
                ShowDeployedPanel();
                break;
            case PanelState.Disabled:
                ShowDisabledPanel();
                break;
        }
    }

    /// <summary>
    /// 빈 패널 표시
    /// </summary>
    private void ShowEmptyPanel()
    {
        // 초상화 비활성화
        if (portraitImage != null)
        {
            portraitImage.gameObject.SetActive(false);
        }

        // 배치 버튼만 활성화
        if (deployButton != null)
            deployButton.gameObject.SetActive(true);
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 알바 배치 중 패널 표시
    /// </summary>
    private void ShowEditingPanel()
    {
        if (currentNpc == null)
        {
            ShowEmptyPanel();
            return;
        }

        // 초상화 활성화
        if (portraitImage != null)
        {
            portraitImage.gameObject.SetActive(true);
            // 초상화 이미지 설정 (현재 편집중인 NPC의 초상화 표시)
            portraitImage.sprite = ArbeitRepository.Instance.GetPortraitByPrefabName(currentNpc);
        }

        // 확인/취소 버튼 활성화
        if (deployButton != null)
            deployButton.gameObject.SetActive(false);
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// 배치된 패널 표시
    /// </summary>
    private void ShowDeployedPanel()
    {
        if (currentNpc == null)
        {
            ShowEmptyPanel();
            return;
        }

        // 초상화 활성화
        if (portraitImage != null)
        {
            if (currentNpc.portraitSprite != null)
            {
                portraitImage.sprite = currentNpc.portraitSprite;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {
                portraitImage.gameObject.SetActive(false);
                Debug.LogWarning($"[ArbeitEditPanelUI] '상태: Deployed, '{currentNpc.part_timer_name}'의 portraitSprite가 null입니다.");
            }
        }

        // 배치 버튼 활성화
        if (deployButton != null)
            deployButton.gameObject.SetActive(true);
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// 비활성화된 패널 표시 (최대 배치 수 초과한 경우)
    /// </summary>
    private void ShowDisabledPanel()
    {
        // 초상화 비활성화
        if (portraitImage != null)
        {
            portraitImage.gameObject.SetActive(false);
        }

        // 모든 버튼 비활성화
        if (deployButton != null)
            deployButton.gameObject.SetActive(false);
        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);
        if (cancelButton != null)
            cancelButton.gameObject.SetActive(false);
    }

    #region 버튼 이벤트
    /// <summary>
    /// 배치 버튼 클릭 (빈 패널)
    /// </summary>
    private void OnDeployButtonClicked()
    {
        if (parentPageUI != null)
        {
            // 슬롯 인덱스를 전달하여 기존 알바 교체 가능하도록 함
            parentPageUI.OnEditPanelDeployClicked(panelIndex);
        }
    }

    /// <summary>
    /// 확인 버튼 클릭 (편집 중)
    /// </summary>
    private void OnConfirmButtonClicked()
    {
        if (parentPageUI != null && currentNpc != null)
        {
            parentPageUI.OnEditPanelConfirmClicked(currentNpc);
        }
    }

    /// <summary>
    /// 취소 버튼 클릭 (편집 중)
    /// </summary>
    private void OnCancelButtonClicked()
    {
        if (parentPageUI != null)
        {
            parentPageUI.OnEditPanelCancelClicked();
        }
    }
    #endregion
}
