using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Merge;
using UnityEngine.UI;

/// <summary>
/// 오브젝트를 타일맵 위에 드래그 앤 드롭하여 배치하는 기능
/// 오브젝트에 Collider2D(중요!)와 SpriteRenderer가 있어야 함
/// 타일맵에는 Grid 컴포넌트가 있어야 함, 타일맵에는 ground 라는 이름이 포함되어야 함
/// 편집모드(onEdit = True)일 때 드래그 앤 드롭기능 활성화, 만약 아닐 경우 비활성화됨
/// 편집모드가 아닐때에는 건물 위에 마우스 우클릭을 꾹 누를경우(3초 정도 && 마우스 이동이 거의 없을때) 편집모드 활성화
///                     또는 EditButton을 눌러 편집모드 활성화 가능
/// 건물을 샀을때에도 바로 배치모드로 들어가, 배치 시에 건물 건설을 시작함
/// 건물 인벤토리와도 연결이 되어, 건물에 ESC + 우클릭을 하여 건물을 건물 인벤토리 내로 집어 넣을 수 있고,
/// 건물 인벤토리(EditScroll)에서 건물을 꺼내어 배치할 수도 있음
/// </summary>
public class DragDropController : MonoBehaviour
{
    private static DragDropController _instance;
    public static DragDropController Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<DragDropController>();
            }
            return _instance;
        }
    }

    [Header("타일맵 설정")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap groundTilemap; // 땅 타일맵, 건물 배치 시, 배치될 땅 타일
    // 건물 타일맵 배열, 그려진 타일 없는 타일맵들
    [SerializeField] private Tilemap previewTilemap;
    [SerializeField] private Tilemap banTilemap; // 건물 배치 불가능한 타일맵
    [SerializeField] private Tilemap ExistingTilemap; // 기존 건물들의 타일맵
    [SerializeField] private TileBase markerTile; // 건물 배치 시 나오는 프리뷰 타일
    
    // 인테리어 타일맵 (추가)
    [SerializeField] private Tilemap ExistingInteriorTilemap; // 기존 인테리어들의 타일맵
    [SerializeField] private TileBase interiorMarkerTile; // 인테리어 배치 시 나오는 프리뷰 타일 (옵션, markerTile 재사용 가능)

    [Header("호감도 조건")]
    [SerializeField] private Tilemap favorCheckTilemap; // 호감도 체크에 사용할 타일맵 (미지정 시 groundTilemap 사용)
    [SerializeField] private TileBase[] favorRewardTiles; // 호감도 상승이 허용된 타일 목록

    [Header("드래그 설정")]
    [SerializeField] private Camera mainCamera;
    
    [Header("편집 모드(스크롤 및 진행바 UI)")]
    [SerializeField] private CircularProgressBar editModeProgressBar; // 편집 모드 활성화 진행바
    [SerializeField] private EditScrollUI editScrollUI; // 편집 모드 활성화 진행바
    [SerializeField] private float EditMode_Time = 3f; // 편집 모드 활성화 시간(우클릭 꾹 누르면 활성화)
    [SerializeField] private float maxPositionDrift = 0.3f; // 편집 모드 활성화 중 허용되는 최대 마우스 이동 거리, 혹시 모를 손떨림을 대비하기 위함
    public bool onEdit = false; // 편집 모드 활성화 여부
    [Header("건물 마커 설정/마커 타일맵의 Y 오프셋")]
    [SerializeField] private float markerOffset;
    [Header("새 건물 건설 완료 UI")]
    [SerializeField] private GameObject newBuildingCompleteUI;
    [SerializeField] private Vector2 newBuildingCompleteSize = new Vector2(100,100);
    private float buildingConstructionDelay; // 건물 건설 완료 대기시간

    private bool isDraggingSprite = false; 
    private GameObject draggedSpriteObject = null;
    private Vector3Int originalSpriteCell; //오브젝트의 원래 셀 위치
    private Vector3 originalSpritePosition; // 오브젝트의 원래 월드 위치
    private SpriteRenderer draggedSpriteRenderer = null; // 드래그 중인 오브젝트의 스프라이트 렌더러
    private Color originalSpriteColor; // 원래 스프라이트 색상 (프리뷰용)

    [HideInInspector]
    public bool isUI = false;
    
    // 편집 모드 or 스프라이트 드래그 중일 경우 => IsEditMode true 반환
    // BuildingBase.cs 스크립트에서 편집모드일때 클릭되는걸 방지하기 위해 사용됨
    public bool IsEditMode => onEdit || isDraggingSprite;
    
    private GameObject editTargetObject = null; // 편집대상인 오브젝트
    private float rightClickHoldTime = 0f; // 우클릭 유지 시간
    
    private bool isHoldingRightClick = false; // 우클릭을 누르고 있는지(꾹 누르고 있는지, 홀딩 중인지)
    private Vector2Int editBuildingTileSize = Vector2Int.one; // 편집 중인 건물의 타일 크기, 자동 변경될 예정
    private Vector3 rightClickStartPosition = Vector3.zero; // 우클릭 시작 위치
    
    // 마커 타일 관리
    private List<Vector3Int> currentMarkerPositions = new List<Vector3Int>(); // 드래그 중 프리뷰 마커 위치
    private Vector3Int originalBuildingCell; // 드래그 시작 시 건물의 원래 셀 위치
    private Vector2Int originalBuildingTileSize; // 드래그 시작 시 건물의 원래 타일 크기
    // 건물 배치 이동 시 잠시 활성화, 반대로 배치 종료 시 비활성화
    private TilemapRenderer previewTilemapRenderer;
    private TilemapRenderer ExistingTilemapRenderer;
    private TilemapRenderer ExistingInteriorTilemapRenderer;

    #region Initialization

    private void Awake()
    {
        // 싱글톤 패턴 초기화, 추후 싱글톤이 아닌 방법으로 변경할 예정
        if (_instance == null)
        {
            _instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 초기화 및 타일맵 설정
    /// </summary>
    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Grid 자동 찾기
        if (grid == null)
        {
            grid = FindObjectOfType<Grid>();
            if (grid == null)
            {
                Debug.LogError("[DragDropController] Scene에서 Grid를 찾을 수 없습니다!");
            }
        }

        // 카메라 투명도 정렬축을 Y 축으로 설정
        // 필요한 이유 = 2D 타일맵에서 Y 축 기준으로 오브젝트가 앞뒤로 겹쳐질 때 올바르게 정렬하기 위함
        if (mainCamera != null)
        {
            mainCamera.transparencySortMode = TransparencySortMode.CustomAxis;
            mainCamera.transparencySortAxis = Vector3.up; // Up_Vector
        }

        // groundTilemap이 비어있을 경우를 대비, 되도록이면 인스펙터에서 할당할 것
        // 타일맵들을 자동으로 찾기, ground 라는 이름이 포함되어 있으면 그라운드 타일맵으로 설정
        if (groundTilemap == null)
        {
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.name.ToLower().Contains("ground")) // 땅 타일맵 찾기, 이름에 ground 포함된 타일맵
                {
                    groundTilemap = tilemap;
                    break;
                }
            }
        }

        if (previewTilemap != null)
        {
            previewTilemapRenderer = previewTilemap.GetComponent<TilemapRenderer>();
            if (previewTilemapRenderer != null)
            {
                previewTilemapRenderer.enabled = false;
            }
        }

        if (ExistingTilemap != null)
        {
            ExistingTilemapRenderer = ExistingTilemap.GetComponent<TilemapRenderer>();
            if (ExistingTilemapRenderer != null)
            {
                ExistingTilemapRenderer.enabled = false;
            }
        }

        if (ExistingInteriorTilemap != null)
        {
            ExistingInteriorTilemapRenderer = ExistingInteriorTilemap.GetComponent<TilemapRenderer>();
            if (ExistingInteriorTilemapRenderer != null)
            {
                ExistingInteriorTilemapRenderer.enabled = false;
            }
        }
    }
    #endregion

    #region Update, Mouse Input
    void Update()
    {
        if (!isUI)
        {
            HandleMouseInput();
        }
        
        if (isDraggingSprite)
        {
            // 편집 모드일 때는 편집 모드 전용 프리뷰 사용 아닐 경우 오브젝트 드래그 프리뷰를 사용
            if (onEdit)
            {
                UpdateEditModePreview();
            }
            else
            {
                UpdateSpritePreview();
            }
        }
    }
    #endregion

    #region Object Drag & Drop & Placement
    /// <summary>
    /// 마우스 입력 처리, 우클릭 홀딩으로 편집 모드 활성화 후 오브젝트 드래그 처리
    /// </summary>
    private void HandleMouseInput()
    {
        // 우클릭 꾹 누르기 -> 편집 모드 활성화
        if (Input.GetMouseButtonDown(1))
        {
            // 편집 모드에서 드래그 중이면 우클릭으로 배치 확정
            if (onEdit && isDraggingSprite)
            {
                EndEditModePlacement();
                return;
            }
            
            // EditButton으로 이미 편집 모드가 활성화되었지만 드래그 중이 아닌 경우
            // 건물/인테리어 선택을 위한 우클릭으로 처리 (편집 모드 해제하지 않음)
            if (onEdit && !isDraggingSprite)
            {
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;
                
                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
                if (hit.collider != null)
                {
                    BuildingBase buildingBase = hit.collider.GetComponent<BuildingBase>();
                    TempBuildingData tempData = hit.collider.GetComponent<TempBuildingData>();
                    
                    // TempBuilding은 편집 모드에서 선택 불가
                    InteriorBase interiorBase = hit.collider.GetComponent<InteriorBase>();
                    
                    if (buildingBase != null && tempData == null)
                    {
                        // 편집 모드에서 새로운 건물 선택 - 드래그 시작
                        editTargetObject = hit.collider.gameObject;
                        StartEditModeDrag();
                        return;
                    }
                    else if (interiorBase != null)
                    {
                        // 편집 모드에서 새로운 인테리어 선택 - 드래그 시작
                        editTargetObject = hit.collider.gameObject;
                        StartEditModeDrag();
                        return;
                    }
                }
                // 건물/인테리어가 아닌 곳을 클릭하면 리턴 => 편집모드는 그대로 유지
                return;
            }
            
            // if) 이미 편집모드 -> 우클릭 홀드 금지
            if (!onEdit)
            {
                isHoldingRightClick = true;
                rightClickHoldTime = 0f;
                
                // 우클릭 시작 위치 저장 (편집 모드 활성화 위치 고정 체크용)
                Vector3 mousePos2 = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos2.z = 0;
                rightClickStartPosition = mousePos2;
                
                // 편집 대상인 오브젝트 감지 (BuildingBase 또는 InteriorBase를 가진 오브젝트)
                RaycastHit2D hit2 = Physics2D.Raycast(mousePos2, Vector2.zero);
                if (hit2.collider != null)
                {
                    BuildingBase buildingBase = hit2.collider.GetComponent<BuildingBase>();
                    TempBuildingData tempData = hit2.collider.GetComponent<TempBuildingData>();
                    
                    // TempBuilding은 우클릭 홀드로 편집 모드 활성화 불가
                    InteriorBase interiorBase = hit2.collider.GetComponent<InteriorBase>();
                
                if (buildingBase != null || interiorBase != null && tempData == null)
                    {
                        editTargetObject = hit2.collider.gameObject;
                        
                        // 진행바 표시 (건물/인테리어 위에서 우클릭 시작했을 때만)
                        if (editModeProgressBar != null)
                        {
                            editModeProgressBar.Show();
                            editModeProgressBar.SetWorldPosition(hit2.collider.transform.position, mainCamera);
                        }
                    }
                }
            }
        }
        
        // 우클릭 유지 시간 체크, 우클릭을 꾹 누를 시 편집 모드 활성화
        if (isHoldingRightClick && Input.GetMouseButton(1))
        {
            editModeProgressBar.Show();
            // 마우스 위치 변화 체크 (편집 모드 활성화 중에만)
            if (!onEdit && editTargetObject != null)
            {
                Vector3 currentMousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                currentMousePos.z = 0;
                float positionDrift = Vector3.Distance(rightClickStartPosition, currentMousePos);
                
                // 마우스가 너무 많이 움직이면 편집 모드 활성화 취소
                if (positionDrift > maxPositionDrift)
                {
                    isHoldingRightClick = false;
                    rightClickHoldTime = 0f;
                    editTargetObject = null;
                    
                    // 진행바 숨기기
                    if (editModeProgressBar != null)
                    {
                        editModeProgressBar.Hide();
                    }
                    return;
                }
                
                // 진행바 업데이트 (0 ~ 1)
                if (editModeProgressBar != null)
                {
                    float progress = rightClickHoldTime / EditMode_Time;
                    editModeProgressBar.UpdateProgress(progress);
                    
                    // 진행바 위치를 건물 위치로 유지
                    editModeProgressBar.SetWorldPosition(editTargetObject.transform.position, mainCamera);
                }
            }
            
            rightClickHoldTime += Time.deltaTime;
            
            // n초 이상 유지 시 편집 모드 활성화
            if (rightClickHoldTime >= EditMode_Time && !onEdit && editTargetObject != null)
            {
                ActivateEditMode();
                
                // 진행바 숨기기 (편집 모드 활성화되면)
                if (editModeProgressBar != null)
                {
                    editModeProgressBar.Hide();
                }
            }
        }
        
        // 우클릭 해제 시 처리
        if (Input.GetMouseButtonUp(1))
        {
            // 편집 모드가 아닐 때만 우클릭 해제 처리
            if (isDraggingSprite && !onEdit)
            {
                EndSpriteDrag();
            }
            
            // 우클릭 홀드 상태 초기화
            isHoldingRightClick = false;
            rightClickHoldTime = 0f;
            editTargetObject = null;
            
            // 진행바 숨기기
            if (editModeProgressBar != null)
            {
                editModeProgressBar.Hide();
            }
        }

        // ESC 키로 드래그 취소 및 편집 모드 종료
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 드래그 중이면 먼저 취소
            if (isDraggingSprite)
            {
                CancelSpriteDrag();
            }
            
            // 편집 모드 활성화 상태면 종료
            if (onEdit)
            {
                onEdit = false;
                editScrollUI.ToggleScrollUI();
            }
            
            // 진행바 숨기기
            if (editModeProgressBar != null)
            {
                editModeProgressBar.Hide();
            }

            // 현재 배치되어 있는 preview 마커 삭제
            ClearMarkers();
        }

        // Ctrl + 우클릭으로 건물을 인벤토리에 넣기
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetMouseButtonDown(1))
        {
            TryMoveToInventory();
        }
    }

    

    /// <summary>
    /// 마우스 위치(마우스의 월드 좌표)를(을) 그리드 셀 좌표를 반환
    /// </summary>
    private Vector3Int GetMouseCell()
    {

        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 그리드 좌표로 변환
        Vector3Int cell = grid.WorldToCell(mouseWorldPos);
        cell.z = 0;

        return cell;
    }

    /// <summary>
    /// 해당 셀에 오브젝트를 배치할 수 있는지 여부를 반환하는 함수
    /// </summary>
    private bool CanPlaceAt(Vector3Int cell)
    {
        // 오브젝트를 배치할 수 있는 Ground 타일의 좌표 확인
        Vector3Int groundCell = new Vector3Int(cell.x, cell.y, 0);
        bool hasGround = groundTilemap != null && groundTilemap.HasTile(groundCell);

        // previewTilemap에서 건물 타일이 있는지 확인, 건물 타일이 겹치지 않을 경우에만 배치 가능
        Vector3Int buildingCell = new Vector3Int(cell.x, cell.y, 0);
        bool emptyBuilding = true;
        if (previewTilemap != null && previewTilemap.HasTile(buildingCell))
        {
            emptyBuilding = false; // 이미 건물이 있을경우
        }

        return hasGround && emptyBuilding;
    }
    
    /*
    /// <summary>
    /// 마우스를 클릭한 위치에서 오브젝트 드래그 시도
    /// </summary>
    private bool TryStartSpriteDrag()
    {
        // 1. 마우스 위치에서 레이캐스트로 오브젝트 찾기
        // 마우스 위치 받아옴
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 2D 레이캐스트 사용
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
        
        if (hit.collider != null)
        {
            GameObject hitObject = hit.collider.gameObject;
            SpriteRenderer spriteRenderer = hitObject.GetComponent<SpriteRenderer>();
            
            // 레이캐스트로 확인한 오브젝트에 SpriteRenderer가 있고, TilemapRenderer가 없을 경우에 드래그를 시작함
            if (spriteRenderer != null && hitObject.GetComponent<TilemapRenderer>() == null)
            {
                // 레이캐스트로 확인한 오브젝트를 드래그 대상으로 설정함
                draggedSpriteObject = hitObject;
                draggedSpriteRenderer = spriteRenderer;
                originalSpritePosition = hitObject.transform.position;
                originalSpriteCell = grid.WorldToCell(originalSpritePosition);
                originalSpriteColor = spriteRenderer.color;
                
                isDraggingSprite = true;

                return true;
            }
        }
        
        return false;
    }
    */
    
    /// <summary>
    /// 프리뷰 업데이트
    /// </summary>
    private void UpdateSpritePreview()
    {
        if (!isDraggingSprite || draggedSpriteObject == null || draggedSpriteRenderer == null)
            return;
        
        Vector3Int currentCell = GetMouseCell();
        Vector3 worldPos = grid.CellToWorld(currentCell);
        worldPos.z = originalSpritePosition.z;
        
        // 오브젝트 위치 업데이트
        draggedSpriteObject.transform.position = worldPos;
        
        // 설치 가능 여부 체크
        bool canPlace = CanPlaceAt(currentCell);
        
        // 프리뷰 색상 적용 (설치 가능하면 반투명, 불가능하면 빨간색 반투명)
        Color previewColor = canPlace ?
            new Color(1f, 1f, 1f, 0.6f) :  // 설치 가능 시 반투명 이미지 적용됨
            new Color(1f, 0.3f, 0.3f, 0.6f); // 설치 불가 시 빨간색의 반투명 이미지 적용됨
        
        draggedSpriteRenderer.color = previewColor;
    }
    
    /// <summary>
    /// 오브젝트 드래그 종료 및 배치, ESC 누를 시 호출됨
    /// </summary>
    private void EndSpriteDrag()
    {
        if (!isDraggingSprite || draggedSpriteObject == null || draggedSpriteRenderer == null)
            return;
        
        // 마우스 위치의 그리드 셀 좌표
        Vector3Int dropCell = GetMouseCell();
        
        // 마우스 위치에서 오브젝트가 배치가 가능한지
        if (CanPlaceAt(dropCell))
        {
            // 그리드 셀 위치로 오브젝트 배치(월드 좌표로 변환 후)
            Vector3 worldPos = grid.CellToWorld(dropCell);
            worldPos.z = originalSpritePosition.z;
            draggedSpriteObject.transform.position = worldPos;
            
            // 색상 복원
            draggedSpriteRenderer.color = originalSpriteColor; // 프리뷰 색상에서 원래 색상으로

        }
        else
        {
            // 설치할 수 없으면 원래 위치로 롤백
            draggedSpriteObject.transform.position = originalSpritePosition;
            draggedSpriteRenderer.color = originalSpriteColor;
        }
        
        // 드래그 상태 초기화
        isDraggingSprite = false;
        draggedSpriteObject = null;
        draggedSpriteRenderer = null;
    }
    
    /// <summary>
    /// 오브젝트 드래그 취소
    /// </summary>
    private void CancelSpriteDrag()
    {
        if (!isDraggingSprite || draggedSpriteObject == null || draggedSpriteRenderer == null)
            return;

        // 건물/인테리어를 사서, 배치를 진행하고 있던 상태 Vs 이미 있는 오브젝트를 배치하는 상태
        // TempBuildingData가 있는 경우(건물을 사서 배치하는 상태) = 새 건물 프리뷰 오브젝트 => 삭제
        TempBuildingData tempData = draggedSpriteObject.GetComponent<TempBuildingData>();
        if (tempData != null)
        {
            // 프리뷰 오브젝트 삭제
            GameObject previewObj = draggedSpriteObject;
            draggedSpriteObject = null;
            draggedSpriteRenderer = null;
            isDraggingSprite = false;
            
            // 인벤토리 건물인 경우: 인벤토리로 복구
            if (tempData.isFromInventory)
            {
                DataManager.Instance.UpdateBuildingInventoryStatus(tempData.constructedBuildingId, true);
            }
            else
            {
                // 새 건물인 경우: 비용 환불
                var refundMoneyData = ResourceRepository.Instance.GetResourceByName("Money");
                var refundWoodData = ResourceRepository.Instance.GetResourceByName("Wood");
                if (refundMoneyData != null)
                {
                    refundMoneyData.current_amount -= tempData.buildingData.construction_cost_gold;
                    refundMoneyData.current_amount -= tempData.buildingData.construction_cost_wood;
                }
            }
            
            Destroy(previewObj);
        }
        else
        {
            // 기존 건물인 경우 => 배치 전 원래 위치로 되돌리기
            draggedSpriteObject.transform.position = originalSpritePosition;
            draggedSpriteRenderer.color = originalSpriteColor;
            
            // 프리뷰 마커 삭제
            ClearMarkers();
            
            // 원래 위치에 마커 복구
            PlaceTilemapMarkers(originalBuildingCell, originalBuildingTileSize, markerOffset);
            // 드래그 모드 취소
            isDraggingSprite = false;
            draggedSpriteObject = null;
            draggedSpriteRenderer = null;
        }
    }
    #endregion
    
    #region Edit Mode
    /// <summary>
    /// 편집 모드 - 활성화
    /// </summary>
    private void ActivateEditMode()
    {
        if (editTargetObject == null) return;
        
        onEdit = true;
        
        // Edit와 관련된 UI 전체 열기 (IsEditModeUI + EditScrollUI)
        if (editScrollUI != null)
        {
            editScrollUI.ToggleScrollUI();
        }
        
        // BuildingBase 컴포넌트가 있는지 확인
        BuildingBase buildingBase = editTargetObject.GetComponent<BuildingBase>();
        InteriorBase interiorBase = editTargetObject.GetComponent<InteriorBase>();
        
        if (buildingBase != null)
        {
            editBuildingTileSize = buildingBase.TileSize;
        }
        else if (interiorBase != null)
        {
            editBuildingTileSize = interiorBase.TileSize;
        }
        else
        {
            editBuildingTileSize = Vector2Int.one; // 기본값
        }
        
        // 편집 모드 - 오브젝트 드래그 시작
        StartEditModeDrag();
    }
    
    /// <summary>
    /// 편집 모드 - 오브젝트 드래그 시작
    /// </summary>
    private void StartEditModeDrag()
    {
        if (editTargetObject == null) return;

        // BuildingBase 또는 InteriorBase 컴포넌트에서 타일 크기 및 데이터 가져오기
        BuildingBase buildingBase = editTargetObject.GetComponent<BuildingBase>();
        InteriorBase interiorBase = editTargetObject.GetComponent<InteriorBase>();
        
        bool isInterior = interiorBase != null;
        
        if (buildingBase != null)
        {
            editBuildingTileSize = buildingBase.TileSize;
            
            // BuildingData에서 MarkerPositionOffset 가져오기
            if (DataManager.Instance != null && DataManager.Instance.ConstructedBuildings != null)
            {
                ConstructedBuilding constructedBuilding = DataManager.Instance.GetConstructedBuildingById(buildingBase.ConstructedBuildingId);
                if (constructedBuilding != null && BuildingRepository.Instance != null)
                {
                    BuildingData buildingData = BuildingRepository.Instance.GetAllBuildingData()
                        .Find(data => data.building_id == constructedBuilding.Id);
                    if (buildingData != null)
                    {
                        markerOffset = buildingData.MarkerPositionOffset;
                    }
                }
            }
        }
        else if (interiorBase != null)
        {
            editBuildingTileSize = interiorBase.TileSize;

            if (DataManager.Instance != null && DataManager.Instance.InteriorDatas != null)
            {
                InteriorData interiorData = DataManager.Instance.InteriorDatas.Find(data => data.interior_id == interiorBase.InteriorId);
                if (interiorData != null)
                {
                    markerOffset = interiorData.MarkerPositionOffset;
                }
            }
        }
        else
        {
            editBuildingTileSize = Vector2Int.one;
        }
        
        // 기존 건물의 원래 위치 저장 및 마커 제거
        if (editTargetObject != null)
        {
            Vector3 buildingPos = editTargetObject.transform.position;
            buildingPos.z = 0; // Z값을 0으로 고정
            originalBuildingCell = grid.WorldToCell(buildingPos);
            originalBuildingTileSize = editBuildingTileSize;
            
            // 기존 건물 마커만 제거
            RemoveBuildingMarkers(originalBuildingCell, editBuildingTileSize);
        }
        
        // 이전 프리뷰 마커만 삭제
        ClearMarkers();
        ShowMarkerRenderer();
        
        SpriteRenderer spriteRenderer = editTargetObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            draggedSpriteObject = editTargetObject;
            draggedSpriteRenderer = spriteRenderer;
            originalSpritePosition = editTargetObject.transform.position;
            originalSpriteCell = grid.WorldToCell(originalSpritePosition);
            originalSpriteColor = spriteRenderer.color;
            
            // 드래그 프리뷰 색상 설정 (반투명)
            spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
            
            isDraggingSprite = true;
        }
    }
    
    /// <summary>
    /// 편집 모드 - 프리뷰 업데이트
    /// </summary>
    private void UpdateEditModePreview()
    {
        if (!isDraggingSprite || draggedSpriteObject == null || draggedSpriteRenderer == null)
            return;
        
        Vector3Int currentCell = GetMouseCell();
        Vector3 worldPos = grid.CellToWorld(currentCell);
        worldPos.z = originalSpritePosition.z;
        
        draggedSpriteObject.transform.position = worldPos;
        
        // 타일 크기를 고려한 배치 가능 여부 체크
        bool canPlace = CanPlaceWithSize(currentCell, editBuildingTileSize);
        
        Color previewColor = canPlace ?
            new Color(originalSpriteColor.r, originalSpriteColor.g, originalSpriteColor.b, 0.7f) :
            new Color(1f, 0.3f, 0.3f, 0.7f);
        
        draggedSpriteRenderer.color = previewColor;
        
        // 드래그 중 마커 실시간 업데이트
        UpdateMarkerPreview(currentCell, editBuildingTileSize);
    }
    
    /// <summary>
    /// 타일 크기를 고려하여 배치 가능 여부 확인
    /// 건물의 TileSize 범위 내 모든 셀에서 Ground 타일이 있고 다른 건물/인테리어가 없어야 함
    /// markerTile = 배치 가능한 위치
    /// </summary>
    private bool CanPlaceWithSize(Vector3Int startCell, Vector2Int tileSize)
    {
        // 드래그 중인 오브젝트가 건물인지 인테리어인지 확인
        bool isInterior = draggedSpriteObject != null && 
                          (draggedSpriteObject.GetComponent<InteriorBase>() != null || 
                           draggedSpriteObject.GetComponent<TempInteriorData>() != null);

        // 타일 크기만큼 모든 셀 확인
        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                Vector3Int checkCell = new Vector3Int(startCell.x + x, startCell.y + y, startCell.z);

                Vector3 worldPos = grid.CellToWorld(checkCell);
                // 오프셋 적용
                worldPos.y += markerOffset;
                
                Vector3Int offsetCell = grid.WorldToCell(worldPos);
                offsetCell.z = 0;

                // Ground 타일이 있는지 확인
                if (groundTilemap != null)
                {
                    var groundTile = groundTilemap.GetTile(offsetCell);
                    if (groundTile == null)
                    {
                        return false; // Ground 타일이 없으면 배치 불가
                    }
                }

                // 겹침 검사
                if (isInterior)
                {
                    // 인테리어: 다른 인테리어와 겹치는지 체크
                    if (ExistingInteriorTilemap != null)
                    {
                        var existingInteriorTile = ExistingInteriorTilemap.GetTile(offsetCell);
                        if (existingInteriorTile != null)
                        {
                            // 현재 드래그 중인 오브젝트가 자신이 배치될 영역인지 확인
                            // 기존 건물 편집 중이면 원래 위치는 제외해야 하지만, 
                            // 새 인테리어 배치 중이면 무조건 겹침 불가
                            InteriorBase interiorBase = draggedSpriteObject.GetComponent<InteriorBase>();
                            if (interiorBase == null)
                            {
                                // 새 인테리어 배치 중이면 겹침 불가
                                return false;
                            }
                            // 기존 인테리어 편집 중이면 원래 위치는 허용 (추후 구현 가능)
                        }
                    }
                    
                    // 인테리어는 건물이 차지하는 영역 위에는 배치 불가 (선택 사항)
                    // 필요시 아래 주석 해제
                    /*
                    if (ExistingTilemap != null)
                    {
                        var existingBuildingTile = ExistingTilemap.GetTile(offsetCell);
                        if (existingBuildingTile != null)
                        {
                            return false; // 건물 위에는 인테리어 배치 불가
                        }
                    }
                    */
                }
                else // 건물
                {
                    // 건물: 다른 건물과 겹치는지 체크
                    if (ExistingTilemap != null)
                    {
                        var existingTile = ExistingTilemap.GetTile(offsetCell);
                        if (existingTile != null)
                        {
                            // 현재 드래그 중인 오브젝트가 자신이 배치될 영역인지 확인
                            BuildingBase buildingBase = draggedSpriteObject.GetComponent<BuildingBase>();
                            if (buildingBase == null)
                            {
                                // 새 건물 배치 중이면 겹침 불가
                                return false;
                            }
                            // 기존 건물 편집 중이면 원래 위치는 허용 (기존 로직 유지)
                        }
                    }
                }
            }
        }
        
        return true;
    }

    /// <summary>
    /// 인테리어 배치 영역 중 하나라도 호감도용 타일 위에 있는지 확인
    /// </summary>
    private bool IsInteriorPlacedOnFavorTile(Vector3Int startCell, Vector2Int tileSize)
    {
        Tilemap checkTilemap = favorCheckTilemap != null ? favorCheckTilemap : groundTilemap;

        if (grid == null || checkTilemap == null || favorRewardTiles == null || favorRewardTiles.Length == 0)
        {
            return false;
        }

        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                Vector3Int tilePos = new Vector3Int(startCell.x + x, startCell.y + y, startCell.z);
                Vector3 worldPos = grid.CellToWorld(tilePos);
                worldPos.y += markerOffset;

                Vector3Int targetCell = grid.WorldToCell(worldPos);
                targetCell.z = 0;

                TileBase tile = checkTilemap.GetTile(targetCell);
                if (tile != null && System.Array.IndexOf(favorRewardTiles, tile) >= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 편집 모드 - 배치 종료
    /// 새 건물 배치 시: 프리뷰 오브젝트를 삭제하고 BuildingFactory로 실제 건물 생성
    /// 기존 건물 편집 시: 위치만 변경
    /// </summary>
    private void EndEditModePlacement()
    {
        if (!onEdit || draggedSpriteObject == null) return;
        
        Vector3Int dropCell = GetMouseCell();
        
        // 타일 크기를 고려한 배치 가능 여부 확인
        if (CanPlaceWithSize(dropCell, editBuildingTileSize))
        {
            Vector3 worldPos = grid.CellToWorld(dropCell);
            worldPos.z = 0;
            
            // TempBuildingData가 있는 경우 = 새 건물 또는 인벤토리 건물 배치
            TempBuildingData tempData = draggedSpriteObject.GetComponent<TempBuildingData>();
            if (tempData != null && tempData.buildingData != null)
            {
                BuildingData buildingData = tempData.buildingData;
                
                // 프리뷰 오브젝트 위치 고정 및 상태 복원
                draggedSpriteObject.transform.position = worldPos;
                if (draggedSpriteRenderer != null)
                    draggedSpriteRenderer.color = originalSpriteColor;
                
                // 타일맵에 마커 배치
                PlaceTilemapMarkers(dropCell, editBuildingTileSize, markerOffset);
                
                // 인벤토리 건물 여부 확인
                bool isFromInventory = tempData.isFromInventory;
                
                // 드래그 상태 초기화 (TempBuilding은 유지)
                isDraggingSprite = false;
                draggedSpriteObject = null;
                draggedSpriteRenderer = null;
                
                // 새 건물은 편집 모드 종료 및 EditScroll OFF, 인벤토리 건물은 그대로 편집 모드 유지
                if (!isFromInventory)
                {
                    onEdit = false;
                    
                    // EditScroll UI 닫기
                    if (editScrollUI != null)
                    {
                        StartCoroutine(editScrollUI.CloseIsEditModeUI());
                    }
                }
                
                // 일정 시간 후 완료 UI 표시하는 코루틴 시작 (인벤토리 건물은 즉시 건설)
                StartCoroutine(ShowBuildingCompleteUI(tempData.gameObject, buildingData, worldPos, dropCell));
                
                return; // 배치 완료 후 바로 종료
            }
            
            // TempInteriorData가 있는 경우 = 새 인테리어 배치
            TempInteriorData interiorTempData = draggedSpriteObject.GetComponent<TempInteriorData>();
            if (interiorTempData != null && interiorTempData.interiorData != null)
            {
                InteriorData interiorData = interiorTempData.interiorData;

                // 프리뷰 오브젝트 참조 저장 후 즉시 삭제
                GameObject previewObj = draggedSpriteObject;
                Destroy(previewObj);

                // 상태 초기화 (프리뷰 삭제 후)
                draggedSpriteObject = null;
                draggedSpriteRenderer = null;
                isDraggingSprite = false;
                onEdit = false;

                // InteriorFactory를 통해 실제 인테리어 생성
                GameObject realInterior = InteriorFactory.CreateInterior(interiorData, worldPos);

                if (realInterior != null)
                {
                    // 타일맵에 마커 배치 (인테리어)
                    PlaceTilemapMarkers(dropCell, editBuildingTileSize, markerOffset, true);

                    // 호감도 상승 처리: 지정된 타일 위에 있을 때만 증가
                    if (IsInteriorPlacedOnFavorTile(dropCell, editBuildingTileSize))
                    {
                        InteriorFavorManager.AddFavorFromPlacement(5);
                    }
                    else
                    {
                        Debug.Log("[DragDropController] 호감도 조건을 충족하지 않아 보상이 지급되지 않습니다.");
                    }
                }

                return; // 배치 완료 후 바로 종료
            }
            else
            {
                // 기존 건물/인테리어 이동 (편집 모드)
                bool isInterior = draggedSpriteObject.GetComponent<InteriorBase>() != null;
                
                draggedSpriteObject.transform.position = worldPos;
                
                if (draggedSpriteRenderer != null)
                    draggedSpriteRenderer.color = originalSpriteColor;

                PlaceTilemapMarkers(dropCell, editBuildingTileSize, markerOffset);

                // ConstructedBuilding의 Position 업데이트
                BuildingBase buildingBase = draggedSpriteObject.GetComponent<BuildingBase>();
                if (buildingBase != null && DataManager.Instance != null)
                {
                    ConstructedBuilding building = DataManager.Instance.GetConstructedBuildingById(buildingBase.ConstructedBuildingId);
                    if (building != null)
                    {
                        building.Position = dropCell;
                        Debug.Log($"건물 ID {building.Id}의 위치를 {dropCell}로 업데이트했습니다.");
                    }
                }

                // 기존 오브젝트 배치 완료 - 드래그 상태만 초기화 (편집 모드는 유지)
                isDraggingSprite = false;
                draggedSpriteObject = null;
                draggedSpriteRenderer = null;
                editTargetObject = null;
            }
        }
        else
        {
            // 배치 불가능하면 원래 위치로
            draggedSpriteObject.transform.position = originalSpritePosition;
            if (draggedSpriteRenderer != null)
                draggedSpriteRenderer.color = originalSpriteColor;
        }
    }


    
    // 타일맵에 마커(건물이 차지하는 영역 표시) 배치
    // 기존 건물의 마커는 ExistingTilemap에 저장됨
    public void PlaceTilemapMarkers(Vector3Int startCell, Vector2Int tileSize, float customOffset, bool isInterior = false)
    {
        Tilemap targetTilemap = isInterior ? ExistingInteriorTilemap : ExistingTilemap;
        TileBase targetMarker = isInterior ? (interiorMarkerTile != null ? interiorMarkerTile : markerTile) : markerTile;
        
        if (targetTilemap != null && targetMarker != null)
        {
            // 드래그 중 프리뷰 마커만 삭제 (배치된 다른 오브젝트 마커는 유지)
            ClearMarkers();
            
            for (int x = 0; x < tileSize.x; x++)
            {
                for (int y = 0; y < tileSize.y; y++)
                {
                    // 타일 좌표 -> 월드 좌표 -> 오프셋 적용 -> 월드 좌표로 다시 변환
                    Vector3Int tilePos = new Vector3Int(startCell.x + x, startCell.y + y, startCell.z);
                    Vector3 worldPos = grid.CellToWorld(tilePos);
                    
                    // 오프셋 적용
                    worldPos.y += customOffset;

                    Vector3Int placeCell = grid.WorldToCell(worldPos);
                    placeCell.z = startCell.z; 
                    
                    ExistingTilemap.SetTile(placeCell, markerTile);
                    // 배치 완료된 마커는 currentMarkerPositions에 추가하지 않음 (다른 건물 드래그 시 유지되도록)
                }
            }
            
            // 배치 완료 후 currentMarkerPositions 클리어 (배치된 마커는 추적하지 않음)
            currentMarkerPositions.Clear();
            
            // 배치 완료 후 마커를 숨김
            HideMarkerRenderer();
        }
    }

    /// <summary>
    /// 드래그 중 마커 프리뷰 실시간 업데이트
    /// 프리뷰 마커는 기존 마커 위에 덮어쓰지만, ClearMarkers 시 프리뷰만 삭제됨
    /// </summary>
    private void UpdateMarkerPreview(Vector3Int startCell, Vector2Int tileSize)
    {
        if (previewTilemap == null || markerTile == null)
            return;

        // 이전 프리뷰 마커만 삭제
        ClearMarkers();
        
        // 새 위치에 프리뷰 마커 배치
        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                Vector3Int tilePos = new Vector3Int(startCell.x + x, startCell.y + y, 0);
                Vector3 worldPos = grid.CellToWorld(tilePos);
                
                // 오프셋 적용
                worldPos.y += markerOffset;
                
                Vector3Int placeCell = grid.WorldToCell(worldPos);
                placeCell.z = startCell.z;
                
                previewTilemap.SetTile(placeCell, markerTile);
                currentMarkerPositions.Add(placeCell);
            }
        }
    }
    
    /// <summary>
    /// 편집 모드 - 비활성화
    /// </summary>
    private void DeactivateEditMode()
    {
        // 편집 모드 종료
        onEdit = false;
        
        // EditScroll UI 닫기
        StartCoroutine(editScrollUI.CloseIsEditModeUI());
        
        // 원본 스프라이트 렌더러 색상 복원
        if (draggedSpriteRenderer != null)
        {
            draggedSpriteRenderer.color = originalSpriteColor;
        }
        
        // 마커 정리 및 렌더러 숨김
        ClearMarkers();
        HideMarkerRenderer();
        
        isDraggingSprite = false;
        draggedSpriteObject = null;
        draggedSpriteRenderer = null;
        editTargetObject = null;
        editBuildingTileSize = Vector2Int.one;
    }

    /// <summary>
    /// 프리뷰 마커만 삭제 (기존 건물의 마커는 유지)
    /// currentMarkerPositions에 있는 위치만 null로 설정
    /// </summary>
    private void ClearMarkers()
    {
        if (previewTilemap == null)
            return;

        foreach (Vector3Int pos in currentMarkerPositions)
        {
            previewTilemap.SetTile(pos, null);
        }
        
        currentMarkerPositions.Clear();
    }

    /// <summary>
    /// 특정 위치의 건물/인테리어 마커를 타일 크기만큼 제거하고
    /// 기존 오브젝트를 이동할 때 원래 위치의 마커를 타일맵에서 제거하는 데 사용하는 메소드
    /// </summary>
    private void RemoveBuildingMarkers(Vector3Int startCell, Vector2Int tileSize, float customOffset = 0f, bool isInterior = false)
    {
        Tilemap targetTilemap = isInterior ? ExistingInteriorTilemap : ExistingTilemap;
        
        if (targetTilemap == null)
            return;

        // 실제 해당 스크립트에서 사용할 오프셋, customOffset가 0이 아니면 해당 customOffset을 사용하지만, 0이면 markerOffset 사용함
        float offsetToUse = customOffset != 0f ? customOffset : markerOffset;

        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                Vector3Int tilePos = new Vector3Int(startCell.x + x, startCell.y + y, startCell.z);
                Vector3 worldPos = grid.CellToWorld(tilePos);
                
                // 오프셋 적용
                worldPos.y += offsetToUse;
                //Debug.Log("Removing marker at world position: " + worldPos + " 이때 offsetToUse : " + offsetToUse);
                
                Vector3Int placeCell = grid.WorldToCell(worldPos);
                placeCell.z = startCell.z;
                
                ExistingTilemap.SetTile(placeCell, null);
            }
        }
    }

    /// <summary>
    /// 마커 타일맵 렌더러 활성화 (드래그 중일때 호출)
    /// </summary>
    private void ShowMarkerRenderer()
    {
        if (previewTilemapRenderer != null)
        {
            previewTilemapRenderer.enabled = true;
        }
        if (ExistingTilemapRenderer != null)
        {
            ExistingTilemapRenderer.enabled = true;
        }
        if (ExistingInteriorTilemapRenderer != null)
        {
            ExistingInteriorTilemapRenderer.enabled = true;
        }
    }

    /// <summary>
    /// 마커 타일맵 렌더러 비활성화 (배치 완료 후 호출)
    /// </summary>
    private void HideMarkerRenderer()
    {
        if (previewTilemapRenderer != null)
        {
            previewTilemapRenderer.enabled = false;
        }
        if (ExistingTilemapRenderer != null)
        {
            ExistingTilemapRenderer.enabled = false;
        }
        if (ExistingInteriorTilemapRenderer != null)
        {
            ExistingInteriorTilemapRenderer.enabled = false;
        }
    }

    #endregion

    #region New Building Placement
    /// <summary>
    /// 새로 구매한 건물의 배치 모드를 시작함
    /// BuildBuildingButtonUI에서 호출
    /// 배치 확정 시 BuildingFactory를 통해 실제 건물 GameObject가 생성됨
    /// </summary>
    public void StartNewBuildingPlacement(BuildingData buildingData)
    {
        if (buildingData == null || buildingData.building_sprite == null)
        {
            return;
        }

        // 프리뷰용 임시 스프라이트 오브젝트 생성
        GameObject previewObj = new GameObject($"Preview_{buildingData.Building_Name}");
        
        SpriteRenderer spriteRenderer = previewObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = buildingData.building_sprite;
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 10; // 다른 오브젝트보다 위에 표시
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        
        BoxCollider2D collider = previewObj.AddComponent<BoxCollider2D>();
        if (buildingData.tileSize.x > 0 && buildingData.tileSize.y > 0)
        {
            collider.size = new Vector2(buildingData.tileSize.x, buildingData.tileSize.y);
        }

        // 드래그 상태 설정
        draggedSpriteObject = previewObj;
        draggedSpriteRenderer = spriteRenderer;
        originalSpriteColor = spriteRenderer.color;
        isDraggingSprite = true;
        onEdit = true;
        
        // EditScroll UI 열기 (IsEditModeUI만 !!)
        if (editScrollUI != null)
        {
            editScrollUI.ToggleOnlyEditMode();
        }

        // 화면 중앙에 초기 배치
        Vector3 centerWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10f));
        centerWorldPos.z = 0;
        Vector3Int centerCell = grid.WorldToCell(centerWorldPos);
        Vector3 snapPos = grid.CellToWorld(centerCell);
        snapPos.z = 0;
        
        previewObj.transform.position = snapPos;
        originalSpritePosition = snapPos;
        originalSpriteCell = centerCell;
        
        // 타일 크기 설정
        editBuildingTileSize = buildingData.tileSize;
        
        // BuildingData에서 MarkerPositionOffset 가져오기 (새 건물 프리뷰용)
        markerOffset = buildingData.MarkerPositionOffset;
        
        // BuildingData를 임시 저장할 컴포넌트 추가
        var tempData = previewObj.AddComponent<TempBuildingData>();
        tempData.buildingData = buildingData;

        // 렌더러 활성화 및 초기 마커 프리뷰 삭제
        ClearMarkers();
        ShowMarkerRenderer();
    }
    
    /// <summary>
    /// 새로 구매한 인테리어의 배치 모드를 시작함
    /// BuildInteriorButtonUI 등에서 호출
    /// 배치 확정 시 InteriorFactory를 통해 실제 인테리어 GameObject가 생성됨
    /// </summary>
    public void StartNewInteriorPlacement(InteriorData interiorData)
    {
        if (interiorData == null || interiorData.interior_sprite == null)
        {
            return;
        }

        // 프리뷰용 임시 스프라이트 오브젝트 생성
        GameObject previewObj = new GameObject($"Preview_{interiorData.Interior_Name}");

        SpriteRenderer spriteRenderer = previewObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = interiorData.interior_sprite;
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 10; // 다른 오브젝트보다 위에 표시
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);

        BoxCollider2D collider = previewObj.AddComponent<BoxCollider2D>();
        if (interiorData.tileSize.x > 0 && interiorData.tileSize.y > 0)
        {
            collider.size = new Vector2(interiorData.tileSize.x, interiorData.tileSize.y);
        }

        // 드래그 상태 설정
        draggedSpriteObject = previewObj;
        draggedSpriteRenderer = spriteRenderer;
        originalSpriteColor = spriteRenderer.color;
        isDraggingSprite = true;
        onEdit = true;

        // 화면 중앙에 초기 배치
        Vector3 centerWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10f));
        centerWorldPos.z = 0;
        Vector3Int centerCell = grid.WorldToCell(centerWorldPos);
        Vector3 snapPos = grid.CellToWorld(centerCell);
        snapPos.z = 0;

        previewObj.transform.position = snapPos;
        originalSpritePosition = snapPos;
        originalSpriteCell = centerCell;

        // 타일 크기 설정
        editBuildingTileSize = interiorData.tileSize;

        // InteriorData에서 MarkerPositionOffset 가져오기 (새 인테리어 프리뷰용)
        markerOffset = interiorData.MarkerPositionOffset;

        // InteriorData를 임시 저장할 컴포넌트 추가
        var tempData = previewObj.AddComponent<TempInteriorData>();
        tempData.interiorData = interiorData;

        // 렌더러 활성화 및 초기 마커 프리뷰 삭제
        ClearMarkers();
        ShowMarkerRenderer();
    }

    /// <summary>
    /// 인벤토리에서 꺼낸 건물의 배치 모드를 시작함
    /// EditBuildingButtonUI에서 호출
    /// 배치 확정 시 기존 ConstructedBuilding 정보로 실제 건물 생성
    /// </summary>
    public void StartInventoryBuildingPlacement(BuildingData buildingData, int constructedBuildingId)
    {
        if (buildingData == null || buildingData.building_sprite == null)
        {
            return;
        }

        // 프리뷰용 임시 스프라이트 오브젝트 생성
        GameObject previewObj = new GameObject($"Preview_Inventory_{buildingData.Building_Name}");
        
        SpriteRenderer spriteRenderer = previewObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = buildingData.building_sprite;
        spriteRenderer.sortingLayerName = "Default";
        spriteRenderer.sortingOrder = 10;
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.7f);
        
        BoxCollider2D collider = previewObj.AddComponent<BoxCollider2D>();
        if (buildingData.tileSize.x > 0 && buildingData.tileSize.y > 0)
        {
            collider.size = new Vector2(buildingData.tileSize.x, buildingData.tileSize.y);
        }

        // 드래그 상태 설정
        draggedSpriteObject = previewObj;
        draggedSpriteRenderer = spriteRenderer;
        originalSpriteColor = spriteRenderer.color;
        isDraggingSprite = true;
        onEdit = true;
        
        // EditScroll UI 열기 (ScrollUI만 !!)
        if (editScrollUI != null)
        {
            editScrollUI.ToggleOnlyScrollUI();
        }

        // 화면 중앙에 초기 배치
        Vector3 centerWorldPos = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 10f));
        centerWorldPos.z = 0;
        Vector3Int centerCell = grid.WorldToCell(centerWorldPos);
        Vector3 snapPos = grid.CellToWorld(centerCell);
        snapPos.z = 0;
        
        previewObj.transform.position = snapPos;
        originalSpritePosition = snapPos;
        originalSpriteCell = centerCell;
        
        // 타일 크기 설정
        editBuildingTileSize = buildingData.tileSize;
        
        // BuildingData에서 MarkerPositionOffset 가져오기
        markerOffset = buildingData.MarkerPositionOffset;
        
        // TempBuildingData에 인벤토리 건물 정보 저장
        var tempData = previewObj.AddComponent<TempBuildingData>();
        tempData.buildingData = buildingData;
        tempData.isFromInventory = true;
        tempData.constructedBuildingId = constructedBuildingId;
        
        // 렌더러 활성화 및 초기 마커 프리뷰 삭제
        ClearMarkers();
        ShowMarkerRenderer();
        
        Debug.Log($"인벤토리 건물 '{buildingData.Building_Name}' (ID: {constructedBuildingId}) 배치 시작");
    }

    /// <summary>
    /// 건물 배치 후 일정 시간 대기 후 완료 UI를 표시하는 코루틴
    /// UI를 누르면 TempBuilding을 삭제하고 실제 건물을 생성
    /// </summary>
    private IEnumerator ShowBuildingCompleteUI(GameObject tempBuilding, BuildingData buildingData, Vector3 position, Vector3Int cellPosition)
    {
        // TempBuildingData 확인
        TempBuildingData tempData = tempBuilding != null ? tempBuilding.GetComponent<TempBuildingData>() : null;
        bool isFromInventory = tempData != null && tempData.isFromInventory;
        
        // 새 건물인 경우에만 재화 소비 및 건설 시간 대기
        if (!isFromInventory)
        {
            ResourceData moneyData = ResourceRepository.Instance.GetResourceByName("Money");
            ResourceData woodData = ResourceRepository.Instance.GetResourceByName("Wood");
            moneyData.current_amount -= buildingData.construction_cost_gold; // 재화 소비
            woodData.current_amount -= buildingData.construction_cost_wood; // 재화 소비

            buildingConstructionDelay = buildingData.construction_time_minutes * 60f; // 분 단위라, 60초 곱함
            // 지정된 시간만큼 대기
            yield return new WaitForSeconds(buildingConstructionDelay);
        }        
        // 인벤토리 건물인 경우 UI 누를 필요 없이 즉시 건설
        if (isFromInventory)
        {
            CompleteBuildingConstruction(tempBuilding, buildingData, position, cellPosition, null);
            yield break;
        }
        
        // 완료 UI 생성 (건물 건설만)
        if (newBuildingCompleteUI != null && tempBuilding != null)
        {
            GameObject uiInstance = Instantiate(newBuildingCompleteUI);
            
            // Prefab 내부의 Canvas 찾기
            Canvas canvas = uiInstance.GetComponentInChildren<Canvas>();
            RectTransform uiElement = null;
            
            if (canvas != null)
            {
                if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    // 월드 좌표를 스크린 좌표로 변환
                    Vector3 screenPos = mainCamera.WorldToScreenPoint(tempBuilding.transform.position + Vector3.up * 2f);
                    
                    RectTransform[] rectTransforms = uiInstance.GetComponentsInChildren<RectTransform>();
                    foreach (RectTransform rt in rectTransforms)
                    {
                        if (rt.gameObject != canvas.gameObject)
                        {
                            uiElement = rt;
                            uiElement.position = screenPos;
                            // UI 크기를 newBuildingCompleteSize로 고정
                            uiElement.sizeDelta = newBuildingCompleteSize;
                            break;
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("Prefab에서 Canvas를 찾을 수 없습니다.");
            }
            
            // 버튼 클릭 리스너 추가
            UnityEngine.UI.Button completeButton = uiInstance.GetComponentInChildren<UnityEngine.UI.Button>();
            if (completeButton != null)
            {
                completeButton.onClick.AddListener(() => 
                {
                    CompleteBuildingConstruction(tempBuilding, buildingData, position, cellPosition, uiInstance);
                });
            }
            
            // if)Screen Space Canvas -> UI가 TempBuilding을 따라다니도록 코루틴 시작, Update 함수 대신 씀
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay && uiElement != null)
            {
                // UI 위치 업데이트 코루틴 시작
                StartCoroutine(UpdateUIPositionOverlay(tempBuilding, uiElement, uiInstance));
            }
        }
    }

    /// <summary>
    /// Screen Space Overlay UI를 TempBuilding 위치에 맞춰 업데이트
    /// 만약 줌 인 시에 UI 크기 커지고 반대로 줌 아웃 시 UI 크기가 작아져, UI 크기가 일정하게 유지되어 보이게 함
    /// </summary>
    private IEnumerator UpdateUIPositionOverlay(GameObject tempBuilding, RectTransform uiElement, GameObject uiInstance)
    {
        // UI 크기 고정 = newBuildingCompleteSize로
        if (uiElement != null)
        {
            uiElement.sizeDelta = newBuildingCompleteSize;
        }
        
        // 초기 설정 저장
        float initialOrthoSize = mainCamera != null ? mainCamera.orthographicSize : 5f;
        Vector3 initialCameraPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
        Vector3 initialBuildingPos = tempBuilding != null ? tempBuilding.transform.position : Vector3.zero;
        
        while (tempBuilding != null && uiInstance != null && uiElement != null)
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("Main Camera를 찾을 수 없습니다."); // 없으면 곤란함 !!!
                    yield break;
                }
            }

            // TempBuilding의 월드 좌표 (오프셋 포함)
            Vector3 worldPos = tempBuilding.transform.position + Vector3.up * 2f;
            
            // 현재 카메라의 OrthographicSize로 스크린 좌표 계산
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            
            // 줌 비율 계산 = 현재 줌 / 초기 줌
            float zoomRatio = (CameraManager.instance.MaxZoomIn / 10) / mainCamera.orthographicSize;
            
            // UI 크기를 줌 비율에 맞춰 조정, 줌 인 => UI 커짐, 줌 아웃 => UI 작아짐
            Vector2 scaledSize = newBuildingCompleteSize * zoomRatio;
            
            // UI 위치 업데이트 및 가시성 처리
            if (screenPos.z > 0)
            {
                uiElement.position = screenPos;
                uiElement.sizeDelta = scaledSize;
                
                if (!uiElement.gameObject.activeSelf)
                    uiElement.gameObject.SetActive(true);
            }
            else
            {
                if (uiElement.gameObject.activeSelf)
                    uiElement.gameObject.SetActive(false);
            }
            
            yield return null;
        }
        
        if (uiInstance != null)
        {
            Destroy(uiInstance);
        }
    }
    /// <summary>
    /// TempBuilding을 삭제하고 실제 건물을 생성하는 메서드
    /// 완료 UI의 버튼 클릭 시 호출됨
    /// </summary>
    private void CompleteBuildingConstruction(GameObject tempBuilding, BuildingData buildingData, Vector3 position, Vector3Int cellPosition, GameObject uiInstance)
    {
        // TempBuildingData 확인
        TempBuildingData tempData = tempBuilding != null ? tempBuilding.GetComponent<TempBuildingData>() : null;
        bool isFromInventory = tempData != null && tempData.isFromInventory;
        int constructedBuildingId = tempData != null ? tempData.constructedBuildingId : -1;
        
        if (tempBuilding != null)
        {
            // TempBuilding 삭제
            Destroy(tempBuilding);
        }
        
        // 완료 UI 삭제
        if (uiInstance != null)
        {
            Destroy(uiInstance);
        }
        
        // BuildingFactory를 통해 실제 건물 생성
        GameObject realBuilding = BuildingFactory.CreateBuilding(buildingData, position);
        
        if (realBuilding != null)
        {
            // 인벤토리에서 꺼낸 건물인지 여부에 따라 처리
            if (isFromInventory && constructedBuildingId >= 0)
            {
                // 인벤토리에서 꺼낸 건물인 경우 -> IsEditInventory를 false로 변경
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.UpdateBuildingInventoryStatus(constructedBuildingId, false);
                    
                    // DataManager의 인벤토리 리스트 갱신 -> EditBuildingButtonUI에서 바로 확인 가능하도록
                    DataManager.Instance.RefreshEditModeInventory();
                    
                    // EditScrollUI 갱신 -> EditBuildingButtonUI에서 바로 확인 가능하도록
                    if (editScrollUI != null)
                    {
                        editScrollUI.RefreshInventoryUI();
                    }
                }
            }
            else
            {
                // 새로 구매한 건물인 경우 -> ConstructedBuilding 데이터 생성 및 저장
                if(DataManager.Instance.GetConstructedBuildingById(buildingData.building_id) == null)
                {
                    BuildingRepository.Instance.AddConstructedBuilding(buildingData.building_id, cellPosition);
                }
            }
        }
    }

    #endregion

    #region Building Inventory Management
    /// <summary>
    /// Ctrl + 우클릭으로 건물을 인벤토리로 이동
    /// </summary>
    private void TryMoveToInventory()
    {
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
        if (hit.collider != null)
        {
            BuildingBase buildingBase = hit.collider.GetComponent<BuildingBase>();
            TempBuildingData tempData = hit.collider.GetComponent<TempBuildingData>();
            
            // 건물만 인벤토리로 이동 가능
            if (buildingBase != null && tempData == null)
            {
                // DataManager를 통해 IsEditInventory 상태 업데이트
                if (DataManager.Instance != null)
                {
                    DataManager.Instance.UpdateBuildingInventoryStatus(buildingBase.ConstructedBuildingId, true);
                    
                    // DataManager의 인벤토리 리스트 갱신
                    DataManager.Instance.RefreshEditModeInventory();
                    
                    // EditScrollUI 갱신 -> EditBuildingButtonUI에서 바로 확인 가능하도록
                    if (editScrollUI != null)
                    {
                        editScrollUI.RefreshInventoryUI();
                    }
                    
                    // 건물 오브젝트 삭제 전에 해당 건물의 MarkerPositionOffset 가져오기
                    Vector3Int buildingCell = grid.WorldToCell(hit.collider.transform.position);
                    float buildingMarkerOffset = markerOffset; // 기본값
                    
                    if (DataManager.Instance != null && BuildingRepository.Instance != null)
                    {
                        ConstructedBuilding constructedBuilding = DataManager.Instance.GetConstructedBuildingById(buildingBase.ConstructedBuildingId);
                        if (constructedBuilding != null)
                        {
                            BuildingData buildingData = BuildingRepository.Instance.GetAllBuildingData()
                                .Find(data => data.building_id == constructedBuilding.Id);
                            if (buildingData != null)
                            {
                                buildingMarkerOffset = buildingData.MarkerPositionOffset;
                            }
                        }
                    }
                    
                    RemoveBuildingMarkers(buildingCell, buildingBase.TileSize, buildingMarkerOffset);
                    Destroy(hit.collider.gameObject);
                }
            }
        }
    }
    #endregion
}




