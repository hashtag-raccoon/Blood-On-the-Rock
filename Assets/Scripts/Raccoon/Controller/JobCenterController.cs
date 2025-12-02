using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 구인소 건물 전용 컨트롤러
/// BuildingBase의 일반 UI 시스템 대신 JobCenterScrollUI를 직접 제어합니다.
/// </summary>
public class JobCenterController : BuildingBase
{
    [Header("구인소 UI 할당")]
    public JobCenterScrollUI jobCenterScrollUI;

    protected override void Start()
    {
        base.Start();

        // JobCenterScrollUI 자동 할당
        if (jobCenterScrollUI == null)
        {
            jobCenterScrollUI = FindObjectOfType<JobCenterScrollUI>();
        }

        if (jobCenterScrollUI == null)
        {
            Debug.LogWarning("[JobCenterController] JobCenterScrollUI를 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// OnPointerDown을 오버라이드하여 구인소 UI 토글을 직접 처리함
    /// </summary>
    public override void OnPointerDown(PointerEventData eventData)
    {
        // 왼쪽 버튼이 아닐 경우 무시
        if (eventData.button != PointerEventData.InputButton.Left) return;

        // 편집 모드 체크
        var dd = DragDropController.Instance ?? FindObjectOfType<DragDropController>();
        if (dd == null || dd.onEdit) return;

        // JobCenterScrollUI가 열려있는지 확인
        if (jobCenterScrollUI != null)
        {
            // scrollUI 자식 오브젝트를 안전하게 찾기
            Transform scrollUITransform = null;
            if (jobCenterScrollUI.transform.childCount > 0)
            {
                scrollUITransform = jobCenterScrollUI.transform.GetChild(0);
            }

            // scrollUI가 없거나 비활성화 상태면 UI 열기
            bool isUIActive = scrollUITransform != null && scrollUITransform.gameObject.activeSelf;

            if (isUIActive)
            {
                jobCenterScrollUI.CloseUI();
                //AnimateCamera(false);
                DragDropController.Instance.isUI = false;
                CameraManager.instance.isBuildingUIActive = false;
            }
            else
            {
                jobCenterScrollUI.OpenUI();
                //AnimateCamera(true);
                DragDropController.Instance.isUI = true;
                CameraManager.instance.isBuildingUIActive = true;
            }
        }
        else
        {
            Debug.LogWarning("[JobCenterController] JobCenterScrollUI가 할당되지 않았습니다.");
        }
    }

    protected override void Update()
    {
        // ESC로 구인소 UI 닫기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (jobCenterScrollUI != null)
            {
                //Transform scrollUITransform = jobCenterScrollUI.transform.GetChild(0);
                bool isUIActive = jobCenterScrollUI.scrollUI.activeSelf;
                if (isUIActive)
                {
                    jobCenterScrollUI.CloseUI();
                    AnimateCamera(false);
                    DragDropController.Instance.isUI = false;
                    CameraManager.instance.isBuildingUIActive = false;
                }
            }
        }
    }
}
