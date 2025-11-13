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
    [SerializeField] protected int constructedBuildingId; // ConstructedBuilding ID
    protected ConstructedBuilding constructedBuilding; // 런타임 건물 데이터
    [SerializeField] protected Sprite BuildingSprite;
    [SerializeField] protected GameObject BuildingUI;
    [SerializeField] protected Button BuildingUpgradeButton;
    [SerializeField] protected GameObject UpgradeUIPrefab;
    [SerializeField] protected GameObject UpgradeBlurUI;
    
    protected static GameObject activeUpgradeUI;
    protected static BuildingBase currentActiveBuilding; // 현재 활성화된 건물
    private static bool upgradeButtonInitialized = false; // 버튼 리스너 초기화 여부
    
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
        StartCoroutine(WaitForDataAndInitialize());
    }

    protected virtual IEnumerator WaitForDataAndInitialize()
    {
        // DataManager가 데이터를 로드할 때까지 대기
        yield return new WaitUntil(() =>
            DataManager.Instance != null &&
            DataManager.Instance.ConstructedBuildings != null &&
            DataManager.Instance.ConstructedBuildings.Count > 0
        );

        // ConstructedBuilding 로드
        constructedBuilding = DataManager.Instance.GetConstructedBuildingById(constructedBuildingId);
        if (constructedBuilding == null)
        {
            Debug.LogError($"ID {constructedBuildingId}에 해당하는 ConstructedBuilding을 찾을 수 없습니다.");
        }

        InitializeCamera();

        if (BuildingUpgradeButton != null && !upgradeButtonInitialized)
        {
            BuildingUpgradeButton.onClick.AddListener(() =>
            {
                // 현재 활성화된 건물의 OnUpgradeUI만 호출
                if (currentActiveBuilding != null)
                {
                    currentActiveBuilding.OnUpgradeUI();
                }
            });
            upgradeButtonInitialized = true;
        }
    }

    protected virtual void Update()
    {
        
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
        if(Input.GetMouseButtonDown(0))
        {
            if (virtualCamera == null)
            {
                InitializeCamera();
            }

            // 건물 UI 열기, 닫기 토글 방식 => 건물 UI가 열려 있지 않다면 열기, 아니라면 닫기
            bool isOpening = !BuildingUI.activeSelf;
            
            if (isOpening)
            {
                CameraManager.instance.isBuildingUIActive = true;
                OpenBuildingUI();
                AnimateCamera(true); // 열기 애니메이션
            }
            else
            {
                CameraManager.instance.isBuildingUIActive = false;
                CloseBuildingUI();
                AnimateCamera(false); // 닫기 애니메이션
            }
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
        currentActiveBuilding = this; // 현재 건물을 활성 건물로 설정
    }

    public virtual void CloseBuildingUI()
    {
        BuildingUI?.SetActive(false);
        CameraManager.instance.isBuildingUIActive = false;
        if (currentActiveBuilding == this)
        {
            currentActiveBuilding = null; // 활성 건물 해제
        }
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
            Destroy(activeUpgradeUI);
            activeUpgradeUI = null;
        }

        activeUpgradeUI = Instantiate(UpgradeUIPrefab);

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            activeUpgradeUI.transform.SetParent(canvas.transform, false);
        }
        BlurOnOff();
        UpgradeUIUpdate();
    }
    
    public void UpgradeUIUpdate()
    {
        if (constructedBuilding == null) return;

        UpgradeUIScripts upgradeScript = activeUpgradeUI.GetComponent<UpgradeUIScripts>();
        upgradeScript.MyBuilding = this;
        
        if (upgradeScript != null)
        {
            upgradeScript.SetData(constructedBuilding);
            
            // 다음 레벨의 업그레이드 데이터 찾기
            BuildingUpgradeData upgradeData = DataManager.Instance.GetBuildingUpgradeDataByLevel(
                DataManager.Instance.GetBuildingUpgradeDataByType(constructedBuilding.Name),
                constructedBuilding.Level + 1
            );
            
            if (upgradeData != null)
            {
                upgradeScript.SetUpgradeData(upgradeData);
            }
        }
    }

    public void BlurOnOff()
    {
        UpgradeBlurUI.SetActive(!UpgradeBlurUI.activeSelf);
    }

    public void UpgradeBuildingLevel()
    {
        if (constructedBuilding != null)
        {
            DataManager.Instance.UpgradeBuildingLevel(constructedBuilding.Id);
        }
    }
}
