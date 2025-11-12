using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DragDropController : MonoBehaviour
{
    [Header("타일맵 설정")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap groundTilemap; // 그라운드 타일맵
    [SerializeField] private int buildingTilemapCount = 1; // 건물 타일맵 개수
    [SerializeField] private Tilemap[] buildingTilemaps; // 건물 타일맵 배열

    [Header("드래그 설정")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Tilemap previewTilemap; // 미리보기용 타일맵

    private bool isDragging = false;
    private Vector3Int originalCell; // 기준 셀 (클릭한 타일)
    private int currentBuildingTilemapIndex = -1; // 현재 드래그 중인 타일맵 인덱스
    private List<Vector3Int> buildingCells = new List<Vector3Int>(); // 연결된 모든 건물 타일 좌표
    private Dictionary<Vector3Int, TileBase> buildingTiles = new Dictionary<Vector3Int, TileBase>(); // 각 셀의 타일 정보
    private Vector3 offset;
    private Vector3Int lastPreviewCell = Vector3Int.zero;
    private List<Vector3Int> lastPreviewCells = new List<Vector3Int>(); // 프리뷰용

    // 스프라이트 오브젝트 드래그 관련
    private bool isDraggingSprite = false; // 스프라이트 오브젝트 드래그 중인지
    private GameObject draggedSpriteObject = null; // 현재 드래그 중인 스프라이트 오브젝트
    private Vector3Int originalSpriteCell; // 스프라이트 오브젝트의 원래 셀 위치
    private Vector3 originalSpritePosition; // 스프라이트 오브젝트의 원래 월드 위치
    private SpriteRenderer draggedSpriteRenderer = null; // 드래그 중인 스프라이트 렌더러
    private Color originalSpriteColor; // 원래 스프라이트 색상 (프리뷰용)


    void OnValidate()
    {
        // Inspector에서 buildingTilemapCount가 변경되면 배열 크기 조정
        if (buildingTilemapCount < 1)
            buildingTilemapCount = 1;

        if (buildingTilemaps == null || buildingTilemaps.Length != buildingTilemapCount)
        {
            // 기존 값 보존
            Tilemap[] oldTilemaps = buildingTilemaps;
            buildingTilemaps = new Tilemap[buildingTilemapCount];
            
            // 기존 값 복사
            if (oldTilemaps != null)
            {
                for (int i = 0; i < Mathf.Min(oldTilemaps.Length, buildingTilemapCount); i++)
                {
                    buildingTilemaps[i] = oldTilemaps[i];
                }
            }
        }
    }

    void Start()
    {
        // 건물 타일맵 배열 크기 초기화
        if (buildingTilemaps == null || buildingTilemaps.Length != buildingTilemapCount)
        {
            // 기존 값 보존
            Tilemap[] oldTilemaps = buildingTilemaps;
            buildingTilemaps = new Tilemap[buildingTilemapCount];
            
            // 기존 값 복사
            if (oldTilemaps != null)
            {
                for (int i = 0; i < Mathf.Min(oldTilemaps.Length, buildingTilemapCount); i++)
                {
                    buildingTilemaps[i] = oldTilemaps[i];
                }
            }
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Isometric에서 겹침 방지: 카메라 투명도 정렬축을 Y 축으로 설정
        if (mainCamera != null)
        {
            mainCamera.transparencySortMode = TransparencySortMode.CustomAxis;
            mainCamera.transparencySortAxis = Vector3.up; // (0,1,0)
        }

        // 프리뷰 타일맵이 있다면, 항상 최상단에 보이도록 정렬 순서 상향
        if (previewTilemap != null)
        {
            var pr = previewTilemap.GetComponent<TilemapRenderer>();
            if (pr != null)
            {
                // 다른 타일맵보다 충분히 높은 Order in Layer 보장
                if (pr.sortingOrder < 100)
                    pr.sortingOrder = 100;
            }
        }

        // Grid를 자동으로 찾기
        if (grid == null)
        {
            grid = FindObjectOfType<Grid>();
            if (grid == null)
            {
                Debug.LogError("씬에서 Grid를 찾을 수 없습니다!");
            }
        }

        // 타일맵들을 자동으로 찾기
        if (groundTilemap == null)
        {
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            foreach (Tilemap tilemap in tilemaps)
            {
                Debug.Log($"찾은 타일맵: {tilemap.name}");
                if (tilemap.name.ToLower().Contains("ground"))
                {
                    groundTilemap = tilemap;
                    Debug.Log($"Ground 타일맵 찾음: {tilemap.name}");
                    break;
                }
            }
        }

        // Building 타일맵 배열 자동 찾기 (비어있는 슬롯만)
        Tilemap[] allTilemaps = FindObjectsOfType<Tilemap>();
        int foundIndex = 0;
        foreach (Tilemap tilemap in allTilemaps)
        {
            if (tilemap.name.ToLower().Contains("building") && foundIndex < buildingTilemaps.Length)
            {
                if (buildingTilemaps[foundIndex] == null)
                {
                    buildingTilemaps[foundIndex] = tilemap;
                    Debug.Log($"Building 타일맵 [{foundIndex}] 찾음: {tilemap.name}");
                    foundIndex++;
                }
            }
        }

        // 프리뷰 타일맵 자동 찾기
        if (previewTilemap == null)
        {
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.name.ToLower().Contains("preview"))
                {
                    previewTilemap = tilemap;
                    Debug.Log($"Preview 타일맵 찾음: {tilemap.name}");
                    break;
                }
            }
        }

        // 찾은 오브젝트들 확인
        Debug.Log($"Grid: {(grid != null ? "찾음" : "없음")}");
        Debug.Log($"Ground Tilemap: {(groundTilemap != null ? "찾음" : "없음")}");
        for (int i = 0; i < buildingTilemaps.Length; i++)
        {
            Debug.Log($"Building Tilemap [{i}]: {(buildingTilemaps[i] != null ? buildingTilemaps[i].name : "없음")}");
        }
        Debug.Log($"Preview Tilemap: {(previewTilemap != null ? "찾음" : "없음")}");
    }

    void Update()
    {
        HandleMouseInput();
        if (isDraggingSprite)
        {
            UpdateSpritePreview();
        }
        else
        {
            UpdatePreview();
        }
    }

    private void HandleMouseInput()
    {
        // 우클릭으로 드래그 시작
        if (Input.GetMouseButtonDown(1))
        {
            // 스프라이트 오브젝트를 먼저 확인
            if (TryStartSpriteDrag())
            {
                // 스프라이트 드래그 시작됨
            }
            else
            {
                // 타일맵 드래그 시도
                StartDrag();
            }
        }

        // 드래그 중일 때 마우스 따라가기
        if (isDragging && !isDraggingSprite)
        {
            UpdateDragPosition();
        }

        // 마우스 좌클릭 해제로 드래그 종료
        if (Input.GetMouseButtonUp(1))
        {
            if (isDraggingSprite)
            {
                EndSpriteDrag();
            }
            else if (isDragging)
            {
                EndDrag();
            }
        }

        // 우클릭으로 드래그 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isDraggingSprite)
                CancelSpriteDrag();
            else if (isDragging)
                CancelDrag();
        }
    }

    private void StartDrag()
    {
        Vector3Int cellForPlacement = GetMouseCell(); // 현재 설정된 배치 Z(예: 2)

        Debug.Log($"마우스 클릭 위치(배치 Z): {cellForPlacement}");

        // 모든 타일맵을 확인하여 클릭한 위치의 건물 찾기
        for (int tilemapIndex = 0; tilemapIndex < buildingTilemaps.Length; tilemapIndex++)
        {
            Tilemap tilemap = buildingTilemaps[tilemapIndex];
            if (tilemap == null)
                continue;

            if (TryFindBuildingAtXY(cellForPlacement.x, cellForPlacement.y, tilemapIndex, out Vector3Int foundCell, out TileBase foundTile))
            {
                // 클릭한 타일에서 연결된 모든 타일 찾기 (연결된 컴포넌트)
                buildingCells.Clear();
                buildingTiles.Clear();
                
                FindConnectedTiles(foundCell, foundTile, tilemapIndex);

                if (buildingCells.Count > 0)
                {
                    // 기준 셀을 클릭한 타일로 설정
                    originalCell = foundCell;
                    currentBuildingTilemapIndex = tilemapIndex;

                    // 모든 타일 제거 (들고 있는 상태)
                    foreach (var cell in buildingCells)
                    {
                        buildingTiles[cell] = tilemap.GetTile(cell);
                        tilemap.SetTile(cell, null);
                    }

                    isDragging = true;
                    Debug.Log($"{buildingCells.Count}개의 타일로 구성된 건물을 타일맵 [{tilemapIndex}]에서 잡았습니다! 기준 위치(클릭한 타일): {originalCell}");
                    return;
                }
            }
        }

        Debug.Log("건물을 잡을 수 없습니다. 건물 타일을 정확히 클릭했는지 확인하세요.");
    }

    private void UpdateDragPosition()
    {
        // 이 함수는 더 이상 사용하지 않음 (UpdatePreview에서 처리)
    }

    private void UpdatePreview()
    {
        if (!isDragging || buildingCells.Count == 0 || previewTilemap == null)
            return;

        Vector3Int currentCell = GetMouseCell();

        // 이전 미리보기 지우기
        ClearPreview();

        // 기준점으로부터의 오프셋 계산
        Vector3Int offsetFromOriginal = new Vector3Int(
            currentCell.x - originalCell.x,
            currentCell.y - originalCell.y,
            0
        );

        // 설치 가능 여부 체크 (모든 셀)
        bool canPlace = CanPlaceBuildingAt(currentCell);

        // 모든 건물 타일에 프리뷰 표시
        foreach (var cell in buildingCells)
        {
            Vector3Int previewCell = new Vector3Int(
                cell.x + offsetFromOriginal.x,
                cell.y + offsetFromOriginal.y,
                currentCell.z
            );

            TileBase tile = buildingTiles.ContainsKey(cell) ? buildingTiles[cell] : null;
            if (tile != null)
            {
                previewTilemap.SetTile(previewCell, tile);
                previewTilemap.SetTileFlags(previewCell, TileFlags.None);

                Color previewColor = canPlace ?
                    new Color(1f, 1f, 1f, 0.6f) :  // 흰색 반투명 (설치 가능)
                    new Color(1f, 0.3f, 0.3f, 0.6f); // 빨간색 반투명 (설치 불가)

                previewTilemap.SetColor(previewCell, previewColor);
                lastPreviewCells.Add(previewCell);
            }
        }

        Debug.Log($"프리뷰 표시: {buildingCells.Count}개 타일, 기준: {currentCell}, 설치가능: {canPlace}");
    }

    private void EndDrag()
    {
        if (currentBuildingTilemapIndex < 0 || currentBuildingTilemapIndex >= buildingTilemaps.Length)
            return;

        Vector3Int dropCell = GetMouseCell();
        Tilemap currentTilemap = buildingTilemaps[currentBuildingTilemapIndex];

        // 미리보기 제거
        ClearPreview();

        // 기준점으로부터의 오프셋 계산
        Vector3Int offsetFromOriginal = new Vector3Int(
            dropCell.x - originalCell.x,
            dropCell.y - originalCell.y,
            0
        );

        // 모든 셀 설치 가능한지 확인
        if (CanPlaceBuildingAt(dropCell))
        {
            // 모든 타일에 건물 설치
            foreach (var cell in buildingCells)
            {
                Vector3Int placeCell = new Vector3Int(
                    cell.x + offsetFromOriginal.x,
                    cell.y + offsetFromOriginal.y,
                    dropCell.z
                );

                TileBase tile = buildingTiles.ContainsKey(cell) ? buildingTiles[cell] : null;
                if (tile != null)
                {
                    currentTilemap.SetTile(placeCell, tile);
                }
            }
            Debug.Log($"{buildingCells.Count}개 타일로 구성된 건물을 타일맵 [{currentBuildingTilemapIndex}]의 ({dropCell.x}, {dropCell.y})에 설치했습니다!");
        }
        else
        {
            // 설치할 수 없으면 원래 위치로 되돌리기
            RestoreOriginalBuilding();
            Debug.Log("설치할 수 없는 위치입니다. 원래 위치로 되돌렸습니다.");
        }

        // 드래그 상태 초기화
        isDragging = false;
        buildingCells.Clear();
        buildingTiles.Clear();
        currentBuildingTilemapIndex = -1;
    }

    private void CancelDrag()
    {
        // 미리보기 제거
        ClearPreview();

        // 원래 위치로 되돌리기
        if (buildingCells.Count > 0)
        {
            RestoreOriginalBuilding();
        }

        // 드래그 상태 초기화
        isDragging = false;
        buildingCells.Clear();
        buildingTiles.Clear();
        currentBuildingTilemapIndex = -1;

        Debug.Log("드래그를 취소했습니다.");
    }

    private void ClearPreview()
    {
        if (previewTilemap == null)
            return;

        // 2x2 프리뷰 셀들 지우기
        foreach (var cell in lastPreviewCells)
        {
            previewTilemap.SetTile(cell, null);
            previewTilemap.SetTileFlags(cell, TileFlags.None);
            previewTilemap.SetColor(cell, Color.white);
        }
        lastPreviewCells.Clear();

        // 단일 셀 프리뷰 지우기 (기존 방식)
        if (lastPreviewCell != Vector3Int.zero)
        {
            previewTilemap.SetTile(lastPreviewCell, null);
            previewTilemap.SetTileFlags(lastPreviewCell, TileFlags.None);
            previewTilemap.SetColor(lastPreviewCell, Color.white);
            lastPreviewCell = Vector3Int.zero;
        }
    }


    // 클릭된 x,y에서 모든 Z를 탐색하여 실제로 존재하는 건물 타일을 찾는다 (가장 높은 Z 우선)
    private bool TryFindBuildingAtXY(int x, int y, int tilemapIndex, out Vector3Int foundCell, out TileBase foundTile)
    {
        foundCell = new Vector3Int(x, y, 0);
        foundTile = null;
        
        if (tilemapIndex < 0 || tilemapIndex >= buildingTilemaps.Length)
            return false;
            
        Tilemap tilemap = buildingTilemaps[tilemapIndex];
        if (tilemap == null)
            return false;

        BoundsInt bounds = tilemap.cellBounds;
        for (int z = bounds.zMax - 1; z >= bounds.zMin; z--)
        {
            Vector3Int pos = new Vector3Int(x, y, z);
            if (tilemap.HasTile(pos))
            {
                foundCell = pos;
                foundTile = tilemap.GetTile(pos);
                return true;
            }
        }
        return false;
    }

    private Vector3Int GetMouseCell()
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // 2D이므로 z는 0으로 고정

        // 그리드 좌표로 변환
        Vector3Int cell = grid.WorldToCell(mouseWorldPos);

        // Z Position을 2로 설정 (Tile Palette의 Z Position과 동일하게)
        cell.z = 2;

        return cell;
    }

    private bool CanPlaceAt(Vector3Int cell)
    {
        // 그라운드는 Z=0에서 확인
        Vector3Int groundCell = new Vector3Int(cell.x, cell.y, 0);
        bool hasGround = groundTilemap != null && groundTilemap.HasTile(groundCell);

        // 모든 타일맵에서 건물 타일 확인
        Vector3Int buildingCell = new Vector3Int(cell.x, cell.y, 2);
        bool emptyBuilding = true;
        for (int i = 0; i < buildingTilemaps.Length; i++)
        {
            Tilemap tilemap = buildingTilemaps[i];
            if (tilemap != null && tilemap.HasTile(buildingCell))
            {
                emptyBuilding = false;
                break;
            }
        }

        return hasGround && emptyBuilding;
    }

    // 건물 설치 가능 여부 체크 (모든 연결된 셀 확인)
    private bool CanPlaceBuildingAt(Vector3Int baseCell)
    {
        if (buildingCells.Count == 0 || currentBuildingTilemapIndex < 0)
            return false;

        // 기준점으로부터의 오프셋 계산
        Vector3Int offsetFromOriginal = new Vector3Int(
            baseCell.x - originalCell.x,
            baseCell.y - originalCell.y,
            0
        );

        // 모든 셀 체크
        foreach (var cell in buildingCells)
        {
            Vector3Int checkCell = new Vector3Int(
                cell.x + offsetFromOriginal.x,
                cell.y + offsetFromOriginal.y,
                baseCell.z
            );

            // 그라운드 확인
            Vector3Int groundCell = new Vector3Int(checkCell.x, checkCell.y, 0);
            bool hasGround = groundTilemap != null && groundTilemap.HasTile(groundCell);
            if (!hasGround)
            {
                return false; // 그라운드가 없으면 설치 불가
            }

            // 모든 타일맵에서 건물 타일 확인
            Vector3Int buildingCell = new Vector3Int(checkCell.x, checkCell.y, 2);
            for (int i = 0; i < buildingTilemaps.Length; i++)
            {
                Tilemap tilemap = buildingTilemaps[i];
                if (tilemap != null && tilemap.HasTile(buildingCell))
                {
                    return false; // 이미 건물이 있으면 설치 불가
                }
            }
        }

        return true; // 모든 셀이 설치 가능
    }


    // 원래 위치에 건물 복원
    private void RestoreOriginalBuilding()
    {
        if (buildingCells.Count == 0 || currentBuildingTilemapIndex < 0 || currentBuildingTilemapIndex >= buildingTilemaps.Length)
            return;

        Tilemap currentTilemap = buildingTilemaps[currentBuildingTilemapIndex];
        if (currentTilemap == null)
            return;

        // 모든 타일을 원래 위치로 복원
        foreach (var cell in buildingCells)
        {
            TileBase tile = buildingTiles.ContainsKey(cell) ? buildingTiles[cell] : null;
            if (tile != null)
            {
                currentTilemap.SetTile(cell, tile);
            }
        }
    }

    // 연결된 타일 찾기 (연결된 컴포넌트 알고리즘)
    private void FindConnectedTiles(Vector3Int startCell, TileBase startTile, int tilemapIndex)
    {
        if (tilemapIndex < 0 || tilemapIndex >= buildingTilemaps.Length)
            return;

        Tilemap tilemap = buildingTilemaps[tilemapIndex];
        if (tilemap == null || startTile == null)
            return;

        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        
        queue.Enqueue(startCell);
        visited.Add(startCell);

        // 4방향 연결 확인 (동서남북)
        Vector3Int[] directions = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0),   // 동
            new Vector3Int(-1, 0, 0),  // 서
            new Vector3Int(0, 1, 0),   // 북
            new Vector3Int(0, -1, 0)   // 남
        };

        while (queue.Count > 0)
        {
            Vector3Int current = queue.Dequeue();
            buildingCells.Add(current);

            // 4방향으로 연결된 타일 확인
            foreach (var direction in directions)
            {
                Vector3Int neighbor = new Vector3Int(
                    current.x + direction.x,
                    current.y + direction.y,
                    current.z
                );

                // 이미 방문했거나 범위를 벗어난 경우 스킵
                if (visited.Contains(neighbor))
                    continue;

                // 같은 타일이고 같은 Z 레벨인지 확인
                TileBase neighborTile = tilemap.GetTile(neighbor);
                if (neighborTile != null && neighborTile == startTile)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
    }

    // 셀 좌표로부터 해당하는 타일맵 찾기
    private Tilemap GetTilemapForCell(Vector3Int cell)
    {
        for (int i = 0; i < buildingTilemaps.Length; i++)
        {
            Tilemap tilemap = buildingTilemaps[i];
            if (tilemap != null && tilemap.HasTile(cell))
            {
                return tilemap;
            }
        }
        return null;
    }

    // ====== 스프라이트 오브젝트 드래그 관련 ======
    
    // 마우스 클릭 위치에서 스프라이트 오브젝트 찾기 및 드래그 시작
    private bool TryStartSpriteDrag()
    {
        // 마우스 위치에서 레이캐스트로 스프라이트 오브젝트 찾기
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 2D 레이캐스트 사용
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
        
        if (hit.collider != null)
        {
            GameObject hitObject = hit.collider.gameObject;
            SpriteRenderer spriteRenderer = hitObject.GetComponent<SpriteRenderer>();
            
            // SpriteRenderer가 있고 TilemapRenderer가 없는 오브젝트만 처리
            if (spriteRenderer != null && hitObject.GetComponent<TilemapRenderer>() == null)
            {
                draggedSpriteObject = hitObject;
                draggedSpriteRenderer = spriteRenderer;
                originalSpritePosition = hitObject.transform.position;
                originalSpriteCell = grid.WorldToCell(originalSpritePosition);
                originalSpriteColor = spriteRenderer.color;
                
                isDraggingSprite = true;
                
                Debug.Log($"스프라이트 오브젝트 드래그 시작: {hitObject.name}, 원래 위치: {originalSpriteCell}");
                return true;
            }
        }
        
        return false;
    }
    
    // 스프라이트 오브젝트 프리뷰 업데이트
    private void UpdateSpritePreview()
    {
        if (!isDraggingSprite || draggedSpriteObject == null || draggedSpriteRenderer == null)
            return;
        
        Vector3Int currentCell = GetMouseCell();
        Vector3 worldPos = grid.CellToWorld(currentCell);
        worldPos.z = originalSpritePosition.z; // 원래 Z 위치 유지
        
        // 오브젝트 위치 업데이트
        draggedSpriteObject.transform.position = worldPos;
        
        // 설치 가능 여부 체크
        bool canPlace = CanPlaceAt(currentCell);
        
        // 프리뷰 색상 적용 (설치 가능하면 반투명, 불가능하면 빨간색 반투명)
        Color previewColor = canPlace ?
            new Color(1f, 1f, 1f, 0.6f) :  // 흰색 반투명 (설치 가능)
            new Color(1f, 0.3f, 0.3f, 0.6f); // 빨간색 반투명 (설치 불가)
        
        draggedSpriteRenderer.color = previewColor;
    }
    
    // 스프라이트 오브젝트 드래그 종료 및 배치
    private void EndSpriteDrag()
    {
        if (!isDraggingSprite || draggedSpriteObject == null || draggedSpriteRenderer == null)
            return;
        
        Vector3Int dropCell = GetMouseCell();
        
        // 설치 가능 여부 확인
        if (CanPlaceAt(dropCell))
        {
            // 그리드 셀 위치로 정확히 배치
            Vector3 worldPos = grid.CellToWorld(dropCell);
            worldPos.z = originalSpritePosition.z; // 원래 Z 위치 유지
            draggedSpriteObject.transform.position = worldPos;
            
            // 색상 복원
            draggedSpriteRenderer.color = originalSpriteColor;
            
            Debug.Log($"스프라이트 오브젝트를 셀 ({dropCell.x}, {dropCell.y})에 배치했습니다!");
        }
        else
        {
            // 설치할 수 없으면 원래 위치로 되돌리기
            draggedSpriteObject.transform.position = originalSpritePosition;
            draggedSpriteRenderer.color = originalSpriteColor;
            Debug.Log("설치할 수 없는 위치입니다. 원래 위치로 되돌렸습니다.");
        }
        
        // 드래그 상태 초기화
        isDraggingSprite = false;
        draggedSpriteObject = null;
        draggedSpriteRenderer = null;
    }
    
    // 스프라이트 오브젝트 드래그 취소
    private void CancelSpriteDrag()
    {
        if (!isDraggingSprite || draggedSpriteObject == null || draggedSpriteRenderer == null)
            return;
        
        // 원래 위치로 되돌리기
        draggedSpriteObject.transform.position = originalSpritePosition;
        draggedSpriteRenderer.color = originalSpriteColor;
        
        // 드래그 상태 초기화
        isDraggingSprite = false;
        draggedSpriteObject = null;
        draggedSpriteRenderer = null;
        
        Debug.Log("스프라이트 드래그를 취소했습니다.");
    }
}
