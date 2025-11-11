using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Cinemachine;

public enum PositionData
{
    Left,
    Center,
    Right
}   

public abstract class BuildingBase : MonoBehaviour, IPointerDownHandler
{
    [Header("건물 데이터/UI")]
    [SerializeField] protected BuildingData Buildingdata;
    [SerializeField] protected Sprite BuildingSprite;
    [SerializeField] protected GameObject BuildingUI;
    [SerializeField] protected Button BuildingUpgradeButton;
    [SerializeField] protected GameObject UpgradeUIPrefab; 
    
    private GameObject activeUpgradeUI; 
    
    [Header("카메라 세팅")]
    [SerializeField] protected PositionData CameraPositionOffset;
    [SerializeField] protected float AnimationSpeed = 5f;
    [SerializeField] protected float TargetOrthographicSize = 6f;
    [SerializeField] protected float HorizontalOffset = 5f; 
    
    private CinemachineVirtualCamera virtualCamera;
    private Coroutine cameraCoroutine;

    private float Origin_cameraOrthographicSize;
    private bool cameraInitialized = false;

    protected virtual void Start()
    {
        InitializeCamera();
        
        if (BuildingUpgradeButton != null)
        {
            BuildingUpgradeButton.onClick.AddListener(() =>
            {
                Debug.Log("[BuildingBase] 업그레이드 버튼 클릭됨!");
                OnUpgradeUI();
            });
        }
    }
    
    private void InitializeCamera()
    {
        if (cameraInitialized) return;
        
        if (CameraManager.instance != null)
        {
            virtualCamera = CameraManager.instance.virtualCamera;
            cameraInitialized = true;
            Origin_cameraOrthographicSize = CameraManager.instance.OriginSize;
        }
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (virtualCamera == null)
        {
            InitializeCamera();
        }

        // 건물 UI 열기, 닫기 토글 방식 => 건물 UI가 열려 있지 않다면 열기, 아니라면 닫기
        bool isOpening = !BuildingUI.activeSelf;
        
        if (isOpening)
        {
            OpenBuildingUI();
            AnimateCamera(true); // 열기 애니메이션
        }
        else
        {
            CloseBuildingUI();
            AnimateCamera(false); // 닫기 애니메이션
        }
    }
    
    public virtual void AnimateCamera(bool isOpening)
    {
         if (cameraCoroutine != null)
        {
            StopCoroutine(cameraCoroutine);
        }
        cameraCoroutine = StartCoroutine(AnimateCameraCoroutine(isOpening));
    }

    public virtual void OpenBuildingUI()
    {
        BuildingUI?.SetActive(true);
        CameraManager.instance.isBuildingUIActive = true;
    }

    public virtual void CloseBuildingUI()
    {
        BuildingUI?.SetActive(false);
        CameraManager.instance.isBuildingUIActive = false;
    }

    protected virtual IEnumerator AnimateCameraCoroutine(bool isOpening)
    {
        GameObject mouseFollowingObj = CameraManager.instance.mouseFollowingObj;

        if (!isOpening) // 닫기 애니메이션
        {
            float initialSize = virtualCamera.m_Lens.OrthographicSize;

            float elapsed = 0f;
            float duration = 1f / AnimationSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(
                    initialSize,
                    Origin_cameraOrthographicSize,
                    t
                );

                yield return null;
            }

            virtualCamera.m_Lens.OrthographicSize = Origin_cameraOrthographicSize;

            cameraCoroutine = null;
            yield break;
        }
        else // 열기 애니메이션
        {
            Vector3 targetPos = GetTargetCameraPosition(CameraPositionOffset);
            float initialSize = virtualCamera.m_Lens.OrthographicSize;

            Vector3 initialMousePos = mouseFollowingObj.transform.position;

            float elapsed = 0f;
            float duration = 1f / AnimationSpeed;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                virtualCamera.m_Lens.OrthographicSize = Mathf.Lerp(
                    initialSize,
                    TargetOrthographicSize,
                    t
                );

                mouseFollowingObj.transform.position = Vector3.Lerp(
                    initialMousePos,
                    new Vector3(targetPos.x, targetPos.y, mouseFollowingObj.transform.position.z),
                    t
                );

                yield return null;
            }

            virtualCamera.m_Lens.OrthographicSize = TargetOrthographicSize;
            mouseFollowingObj.transform.position = new Vector3(targetPos.x, targetPos.y, mouseFollowingObj.transform.position.z);
        }
        cameraCoroutine = null;
    }

    protected Vector3 GetTargetCameraPosition(PositionData pos)
    {
        Vector3 buildingPos = transform.position;
        float xOffset = 0f;

        switch (pos)
        {
            case PositionData.Left:
                xOffset = -HorizontalOffset;
                break;
            case PositionData.Center:
                xOffset = 0f;
                break;
            case PositionData.Right:
                xOffset = HorizontalOffset;
                break;
        }

        return new Vector3(
            buildingPos.x + xOffset,
            buildingPos.y,
            0f
        );
    }

    protected virtual void OnUpgradeUI()
    {
        if (activeUpgradeUI != null)
        {
            return;
        }

        activeUpgradeUI = Instantiate(UpgradeUIPrefab);

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            activeUpgradeUI.transform.SetParent(canvas.transform, false);
        }
        UpgradeUIUpdate();
    }
    
    protected void UpgradeUIUpdate()
    {
        //Buildingdata
        //activeUpgradeUI.GetComponent<BuildingUpgradeUI>().;

    }
}
