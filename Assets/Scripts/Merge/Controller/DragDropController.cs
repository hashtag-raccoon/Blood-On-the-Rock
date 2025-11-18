using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Merge;

/// <summary>
/// 오브젝트를 타일맵 위에 드래그 앤 드롭하여 배치하는 기능
/// 오브젝트에 Collider2D(중요!)와 SpriteRenderer가 있어야 함
/// 타일맵에는 Grid 컴포넌트가 있어야 함, 타일맵에는 ground 라는 이름이 포함되어야 함
/// 편집모드(onEdit = True)일 때 드래그 앤 드롭기능 활성화, 만약 아닐 경우 비활성화됨
/// 편집모드가 아닐때에는 건물 위에 마우스 우클릭을 꾹 누를경우(3초 정도 && 마우스 이동이 거의 없을때) 편집모드 활성화
///                     또는 EditButton을 눌러 편집모드 활성화 가능
/// </summary>
public class DragDropController : MonoBehaviour
{
    public static DragDropController instance;

    [Header("타일맵 설정")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap groundTilemap; // 땅 타일맵, 건물 배치 시, 배치될 땅 타일
    [SerializeField] private int buildingTilemapCount = 1; // 건물 타일맵(레이어) 개수

    // 건물 타일맵 배열, 그려진 타일 없는 타일맵들
    [SerializeField] private Tilemap previewTilemap;
    [SerializeField] private Tilemap banTilemap; // 건물 배치 불가능한 타일맵
    [SerializeField] private Tilemap ExistingTilemap; // 기존 건물들의 타일맵
    [SerializeField] private TileBase markerTile; // 건물 배치 시 나오는 프리뷰 타일

    [Header("드래그 설정")]
    [SerializeField] private Camera mainCamera;

    [Header("편집 모드(스크롤 및 진행바 UI)")]
    [SerializeField] private CircularProgressBar editModeProgressBar; // 편집 모드 활성화 진행바
    [SerializeField] private EditScrollUI editScrollUI; // 편집 모드 활성화 진행바

    [Header("건물 마커 설정/마커 타일맵의 Y 오프셋")]
    [SerializeField] private float markerOffset = -3;

    private bool isDraggingSprite = false;
    private GameObject draggedSpriteObject = null;
    private Vector3Int originalSpriteCell; //오브젝트의 원래 셀 위치
    private Vector3 originalSpritePosition; // 오브젝트의 원래 월드 위치
    private SpriteRenderer draggedSpriteRenderer = null; // 드래그 중인 오브젝트의 스프라이트 렌더러
    private Color originalSpriteColor; // 원래 스프라이트 색상 (프리뷰용)

    public bool onEdit = false; // 편집 모드 활성화 여부
    public bool isUI = false;

    // 편집 모드 or 스프라이트 드래그 중일 경우 => IsEditMode true 반환
    // BuildingBase.cs 스크립트에서 편집모드일때 클릭되는걸 방지하기 위해 사용됨
    public bool IsEditMode => onEdit || isDraggingSprite;

    private GameObject editTargetObject = null; // 편집대상인 오브젝트
    private float rightClickHoldTime = 0f; // 우클릭 유지 시간
    [SerializeField] private float EditMode_Time = 3f; // 편집 모드 활성화 시간(우클릭 꾹 누르면 활성화)
    private bool isHoldingRightClick = false; // 우클릭을 누르고 있는지(꾹 누르고 있는지, 홀딩 중인지)
    private Vector2Int editBuildingTileSize = Vector2Int.one; // 편집 중인 건물의 타일 크기, 자동 변경될 예정
    private Vector3 rightClickStartPosition = Vector3.zero; // 우클릭 시작 위치
    [SerializeField] private float maxPositionDrift = 0.3f; // 편집 모드 활성화 중 허용되는 최대 마우스 이동 거리, 혹시 모를 손떨림을 대비하기 위함

    // 마커 타일 관리
    private List<Vector3Int> currentMarkerPositions = new List<Vector3Int>(); // 드래그 중 프리뷰 마커 위치
    private Vector3Int originalBuildingCell; // 드래그 시작 시 건물의 원래 셀 위치
    private Vector2Int originalBuildingTileSize; // 드래그 시작 시 건물의 원래 타일 크기
    // 건물 배치 이동 시 잠시 활성화, 반대로 배치 종료 시 비활성화
    private TilemapRenderer previewTilemapRenderer;
    private TilemapRenderer ExistingTilemapRenderer;

    #region Initialization

    private void Awake()
    {
        // 싱글턴 패턴 초기화
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
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
            // 편집 모드일 때는 편집 모드 전용 프리뷰 사용 아닐 경우 스프라이트 드래그 프리뷰를 사용

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
            // 건물 선택을 위한 우클릭으로 처리 (편집 모드 해제하지 않음)
            if (onEdit && !isDraggingSprite)
            {
                Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = 0;

                RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
                if (hit.collider != null)
                {
                    BuildingBase buildingBase = hit.collider.GetComponent<BuildingBase>();
                    if (buildingBase != null)
                    {
                        // 편집 모드에서 새로운 건물 선택 - 드래그 시작
                        editTargetObject = hit.collider.gameObject;
                        StartEditModeDrag();
                        return;
                    }
                }
                // 건물이 아닌 곳을 클릭하면 리턴 => 편집모드는 그대로 유지
                return;
            }

            isHoldingRightClick = true;
            rightClickHoldTime = 0f;

            // 우클릭 시작 위치 저장 (편집 모드 활성화 위치 고정 체크용)
            Vector3 mousePos2 = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos2.z = 0;
            rightClickStartPosition = mousePos2;

            // 편집 대상인 오브젝트 감지 (BuildingBase를 상속받은 오브젝트만 = 건물인 오브젝트만)
            RaycastHit2D hit2 = Physics2D.Raycast(mousePos2, Vector2.zero);
            if (hit2.collider != null)
            {
                BuildingBase buildingBase = hit2.collider.GetComponent<BuildingBase>();
                if (buildingBase != null)
                {
                    editTargetObject = hit2.collider.gameObject;

                    // 진행바 표시 (건물 위에서 우클릭 시작했을 때만)
                    if (editModeProgressBar != null && !onEdit)
                    {
                        editModeProgressBar.Show();
                        editModeProgressBar.SetWorldPosition(hit2.collider.transform.position, mainCamera);
                    }
                }
            }
        }

        // 우클릭 유지 시간 체크, 우클릭을 꾹 누를 시 편집 모드 활성화
        if (isHoldingRightClick && Input.GetMouseButton(1))
        {
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
            // 편집 모드가 아닐 때만 우클릭 업으로 배치 확정
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
                DeactivateEditMode();
            }

            // 진행바 숨기기
            if (editModeProgressBar != null)
            {
                editModeProgressBar.Hide();
            }
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

        // 건물을 사서, 배치를 진행하고 있던 상태 Vs 이미 있는 건물을 배치하는 상태
        // TempBuildingData가 있는 경우(건물을 사서 배치하는 상태) = 새 건물 프리뷰 오브젝트 => 삭제
        TempBuildingData tempData = draggedSpriteObject.GetComponent<TempBuildingData>();
        if (tempData != null)
        {
            // 프리뷰 오브젝트 삭제
            GameObject previewObj = draggedSpriteObject;
            draggedSpriteObject = null;
            draggedSpriteRenderer = null;
            isDraggingSprite = false;
            Destroy(previewObj);
            var refundMoneyData = DataManager.Instance.GetResourceByName("Money");
            var refundWoodData = DataManager.Instance.GetResourceByName("Wood");
            if (refundMoneyData != null)
            {
                refundMoneyData.current_amount -= tempData.buildingData.construction_cost_gold;
                refundMoneyData.current_amount -= tempData.buildingData.construction_cost_wood;
            }
        }
        else
        {
            // 기존 건물인 경우 => 배치 전 원래 위치로 되돌리기
            draggedSpriteObject.transform.position = originalSpritePosition;
            draggedSpriteRenderer.color = originalSpriteColor;

            // 프리뷰 마커 삭제
            ClearMarkers();

            // 원래 위치에 마커 복구
            PlaceTilemapMarkers(originalBuildingCell, originalBuildingTileSize);

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
        StartCoroutine(editScrollUI.OpenIsEditModeUI());
        // BuildingBase 컴포넌트가 있는지 확인
        BuildingBase buildingBase = editTargetObject.GetComponent<BuildingBase>();
        if (buildingBase != null)
        {
            editBuildingTileSize = buildingBase.TileSize;
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

        // BuildingBase 컴포넌트에서 타일 크기 및 BuildingData 가져오기
        BuildingBase buildingBase = editTargetObject.GetComponent<BuildingBase>();
        if (buildingBase != null)
        {
            editBuildingTileSize = buildingBase.TileSize;

            // BuildingData에서 MarkerPositionOffset 가져오기
            if (DataManager.Instance != null && DataManager.Instance.ConstructedBuildings != null)
            {
                ConstructedBuilding constructedBuilding = DataManager.Instance.GetConstructedBuildingById(buildingBase.ConstructedBuildingId);
                if (constructedBuilding != null && DataManager.Instance.BuildingDatas != null)
                {
                    BuildingData buildingData = DataManager.Instance.BuildingDatas.Find(data => data.building_id == constructedBuilding.Id);
                    if (buildingData != null)
                    {
                        markerOffset = buildingData.MarkerPositionOffset;
                    }
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
            originalBuildingCell = grid.WorldToCell(editTargetObject.transform.position);
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
    /// 건물의 TileSize 범위 내 모든 셀에서 Ground 타일이 있고 다른 건물이 없어야 함
    /// markerTile = 배치 가능한 위치
    /// </summary>
    private bool CanPlaceWithSize(Vector3Int startCell, Vector2Int tileSize)
    {
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

                // ExistingTilemap에서 기존 건물 마커 확인 = 다른 건물과 겹치는지 검사
                if (ExistingTilemap != null)
                {
                    var existingTile = ExistingTilemap.GetTile(offsetCell);
                    if (existingTile != null)
                    {
                        return false; // 기존 건물 마커가 있으면 배치 불가
                    }
                }
            }
        }

        return true;
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

            // TempBuildingData가 있는 경우 = 새 건물 배치
            TempBuildingData tempData = draggedSpriteObject.GetComponent<TempBuildingData>();
            if (tempData != null && tempData.buildingData != null)
            {
                BuildingData buildingData = tempData.buildingData;

                // 프리뷰 오브젝트 참조 저장 후 즉시 삭제
                GameObject previewObj = draggedSpriteObject;
                Destroy(previewObj);

                // 상태 초기화 (프리뷰 삭제 후)
                draggedSpriteObject = null;
                draggedSpriteRenderer = null;
                isDraggingSprite = false;
                onEdit = false;

                // BuildingFactory를 통해 실제 건물 생성
                GameObject realBuilding = BuildingFactory.CreateBuilding(buildingData, worldPos);

                if (realBuilding != null)
                {
                    // 타일맵에 마커 배치
                    PlaceTilemapMarkers(dropCell, editBuildingTileSize);

                    // ConstructedBuildingProduction 데이터 생성 및 저장
                    if (DataManager.Instance.GetConstructedBuildingById(buildingData.building_id) == null)
                    {
                        //SaveNewConstructedBuilding(buildingData);
                    }
                }

                return; // 배치 완료 후 바로 종료
            }
            else
            {
                // 기존 건물 이동 (편집 모드)
                draggedSpriteObject.transform.position = worldPos;

                if (draggedSpriteRenderer != null)
                    draggedSpriteRenderer.color = originalSpriteColor;

                PlaceTilemapMarkers(dropCell, editBuildingTileSize);

                // 기존 건물 배치 완료 - 드래그 상태만 초기화 (편집 모드는 유지)
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
    private void PlaceTilemapMarkers(Vector3Int startCell, Vector2Int tileSize)
    {
        // ExistingTilemap에 기존 건물 마커 배치
        if (ExistingTilemap != null && markerTile != null)
        {
            // 드래그 중 프리뷰 마커만 삭제 (배치된 다른 건물 마커는 유지)
            ClearMarkers();

            for (int x = 0; x < tileSize.x; x++)
            {
                for (int y = 0; y < tileSize.y; y++)
                {
                    // 타일 좌표 -> 월드 좌표 -> 오프셋 적용 -> 월드 좌표로 다시 변환
                    Vector3Int tilePos = new Vector3Int(startCell.x + x, startCell.y + y, startCell.z);
                    Vector3 worldPos = grid.CellToWorld(tilePos);

                    // 오프셋 적용
                    worldPos.y += markerOffset;

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
        StartCoroutine(editScrollUI.CloseIsEditModeUI());
        // 원본 스프라이트 렌더러 색상 복원
        if (draggedSpriteRenderer != null)
        {
            draggedSpriteRenderer.color = originalSpriteColor;
        }

        // 마커 정리 및 렌더러 숨김
        ClearMarkers();
        HideMarkerRenderer();

        onEdit = false;
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
    /// 특정 위치의 건물 마커를 타일 크기만큼 제거하고
    /// 기존 건물을 이동할 때 원래 위치의 마커를 ExistingTilemap에서 제거하는 데 사용하는 메소드
    /// </summary>
    private void RemoveBuildingMarkers(Vector3Int startCell, Vector2Int tileSize)
    {
        if (ExistingTilemap == null)
            return;

        for (int x = 0; x < tileSize.x; x++)
        {
            for (int y = 0; y < tileSize.y; y++)
            {
                Vector3Int tilePos = new Vector3Int(startCell.x + x, startCell.y + y, startCell.z);
                Vector3 worldPos = grid.CellToWorld(tilePos);

                // 오프셋 적용
                worldPos.y += markerOffset;

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
    }
    #endregion

    #region Public Methods - New Building Placement
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
    #endregion
}
