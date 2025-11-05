using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DragDropController : MonoBehaviour
{
    [Header("타일맵 설정")]
    [SerializeField] private Grid grid;
    [SerializeField] private Tilemap groundTilemap; // 그라운드 타일맵
    [SerializeField] private Tilemap buildingTilemap; // 건물 타일맵

    [Header("드래그 설정")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Tilemap previewTilemap; // 미리보기용 타일맵

    private bool isDragging = false;
    private Vector3Int originalCell;
    private TileBase draggedTile;
    private Vector3 offset;
    private Vector3Int lastPreviewCell = Vector3Int.zero;

    // 다중 선택/그룹 이동 관련 상태
    private HashSet<Vector3Int> selectedCells = new HashSet<Vector3Int>();
    private Dictionary<Vector3Int, TileBase> selectedTiles = new Dictionary<Vector3Int, TileBase>();
    private bool isGroupDragging = false;
    private Vector3Int anchorOriginalCell; // 기준 셀 (첫 선택 또는 마우스 시작 시점)
    private List<Vector3Int> lastPreviewCells = new List<Vector3Int>();
    [Header("선택 동작 설정")]
    [SerializeField] private bool clearSelectionAfterPlace = true; // 그룹 배치 후 자동 선택 해제
    // 선택 하이라이트 이전 원본 색상 복원용
    private Dictionary<Vector3Int, Color> originalTileColors = new Dictionary<Vector3Int, Color>();

    void Start()
    {
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

        if (buildingTilemap == null)
        {
            Tilemap[] tilemaps = FindObjectsOfType<Tilemap>();
            foreach (Tilemap tilemap in tilemaps)
            {
                if (tilemap.name.ToLower().Contains("building"))
                {
                    buildingTilemap = tilemap;
                    Debug.Log($"Building 타일맵 찾음: {tilemap.name}");
                    break;
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

        // 모든 타일맵의 타일 개수 확인 (모든 Z 레벨에서)
        if (buildingTilemap != null)
        {
            int tileCount = 0;
            BoundsInt bounds = buildingTilemap.cellBounds;
            Debug.Log($"Building 타일맵 bounds: {bounds}");

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    for (int z = bounds.zMin; z < bounds.zMax; z++)
                    {
                        Vector3Int pos = new Vector3Int(x, y, z);
                        if (buildingTilemap.HasTile(pos))
                        {
                            tileCount++;
                            Debug.Log($"Building 타일 위치: {pos}");
                        }
                    }
                }
            }
            Debug.Log($"Building 타일맵에 총 {tileCount}개의 타일이 있습니다.");
        }

        // 찾은 오브젝트들 확인
        Debug.Log($"Grid: {(grid != null ? "찾음" : "없음")}");
        Debug.Log($"Ground Tilemap: {(groundTilemap != null ? "찾음" : "없음")}");
        Debug.Log($"Building Tilemap: {(buildingTilemap != null ? "찾음" : "없음")}");
        Debug.Log($"Preview Tilemap: {(previewTilemap != null ? "찾음" : "없음")}");
    }

    void Update()
    {
        HandleMouseInput();
        if (isGroupDragging)
        {
            UpdateGroupPreview();
        }
        else
        {
            UpdatePreview();
        }
    }

    private void HandleMouseInput()
    {
        // Alt+우클릭: 선택 토글
        if (Input.GetMouseButtonDown(1) && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
        {
            ToggleSelectionAtMouse();
            return;
        }

        // 우클릭으로 드래그 시작 (선택이 있으면 그룹 드래그, 없으면 단일 드래그)
        if (Input.GetMouseButtonDown(1))
        {
            if (selectedCells.Count > 0)
            {
                StartGroupDrag();
            }
            else
            {
                StartDrag();
            }
        }

        // 드래그 중일 때 마우스 따라가기
        if (isDragging && !isGroupDragging)
        {
            UpdateDragPosition();
        }

        // 마우스 좌클릭 해제로 드래그 종료
        if (Input.GetMouseButtonUp(1))
        {
            if (isGroupDragging)
            {
                EndGroupDrag();
            }
            else if (isDragging)
            {
                EndDrag();
            }
        }

        // 우클릭으로 드래그 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isGroupDragging)
                CancelGroupDrag();
            else if (isDragging)
                CancelDrag();
        }

        // Alt+C: 전체 선택 해제
        if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.C))
        {
            ClearSelectionState();
        }
    }

    private void StartDrag()
    {
        Vector3Int cellForPlacement = GetMouseCell(); // 현재 설정된 배치 Z(예: 2)

        Debug.Log($"마우스 클릭 위치(배치 Z): {cellForPlacement}");
        Debug.Log($"Building Tilemap이 null인가? {buildingTilemap == null}");

        // 클릭 지점(x,y)에서 모든 Z 레벨을 위에서 아래로 탐색하여 실제 존재하는 건물을 찾음
        if (buildingTilemap != null && TryFindBuildingAtXY(cellForPlacement.x, cellForPlacement.y, out Vector3Int foundCell, out TileBase foundTile))
        {
            draggedTile = foundTile;
            originalCell = foundCell;

            buildingTilemap.SetTile(foundCell, null); // 들고 있는 상태
            isDragging = true;
            Debug.Log($"건물을 잡았습니다! 원위치: {foundCell}");
        }
        else
        {
            Debug.Log("건물을 잡을 수 없습니다. 건물 타일을 정확히 클릭했는지, 또는 올바른 타일맵에 그려졌는지 확인하세요.");
        }
    }

    private void UpdateDragPosition()
    {
        // 이 함수는 더 이상 사용하지 않음 (UpdatePreview에서 처리)
    }

    private void UpdatePreview()
    {
        if (!isDragging || draggedTile == null || previewTilemap == null)
            return;

        Vector3Int currentCell = GetMouseCell();

        // 이전 미리보기 지우기
        if (lastPreviewCell != Vector3Int.zero)
        {
            previewTilemap.SetTile(lastPreviewCell, null);
            // 색상 원복 안전 처리
            previewTilemap.SetTileFlags(lastPreviewCell, TileFlags.None);
            previewTilemap.SetColor(lastPreviewCell, Color.white);
        }

        // 현재 위치에 미리보기 표시
        previewTilemap.SetTile(currentCell, draggedTile);
        // per-cell color 적용 위해 TileFlags 해제 필요
        previewTilemap.SetTileFlags(currentCell, TileFlags.None);

        // 설치 가능 여부에 따라 색상 변경
        bool canPlace = CanPlaceAt(currentCell);
        Color previewColor = canPlace ?
            new Color(1f, 1f, 1f, 0.6f) :  // 흰색 반투명 (설치 가능)
            new Color(1f, 0.3f, 0.3f, 0.6f); // 빨간색 반투명 (설치 불가)

        previewTilemap.SetColor(currentCell, previewColor);

        Debug.Log($"프리뷰 표시 셀: {currentCell}, 설치가능: {canPlace}");

        lastPreviewCell = currentCell;
    }

    private void EndDrag()
    {
        Vector3Int dropCell = GetMouseCell();

        // 미리보기 제거
        ClearPreview();

        // 그라운드에 타일이 있고, 건물 위치가 비어있는지 확인
        if (CanPlaceAt(dropCell))
        {
            // 건물 설치
            buildingTilemap.SetTile(dropCell, draggedTile);
            Debug.Log($"건물을 ({dropCell.x}, {dropCell.y})에 설치했습니다!");
        }
        else
        {
            // 설치할 수 없으면 원래 위치로 되돌리기
            buildingTilemap.SetTile(originalCell, draggedTile);
            Debug.Log("설치할 수 없는 위치입니다. 원래 위치로 되돌렸습니다.");
        }

        // 드래그 상태 초기화
        isDragging = false;
        draggedTile = null;
    }

    private void CancelDrag()
    {
        // 미리보기 제거
        ClearPreview();

        // 원래 위치로 되돌리기
        if (draggedTile != null)
        {
            buildingTilemap.SetTile(originalCell, draggedTile);
        }

        // 드래그 상태 초기화
        isDragging = false;
        draggedTile = null;

        Debug.Log("드래그를 취소했습니다.");
    }

    private void ClearPreview()
    {
        if (previewTilemap != null && lastPreviewCell != Vector3Int.zero)
        {
            previewTilemap.SetTile(lastPreviewCell, null);
            previewTilemap.SetTileFlags(lastPreviewCell, TileFlags.None);
            previewTilemap.SetColor(lastPreviewCell, Color.white);
            lastPreviewCell = Vector3Int.zero;
        }
    }

    // ====== 다중 선택/그룹 이동 ======
    private void ToggleSelectionAtMouse()
    {
        Vector3Int cell = GetMouseCell();
        if (buildingTilemap != null && TryFindBuildingAtXY(cell.x, cell.y, out Vector3Int foundCell, out TileBase foundTile))
        {
            if (selectedCells.Contains(foundCell))
            {
                // 해제
                DeselectCell(foundCell);
            }
            else
            {
                // 선택
                SelectCell(foundCell);
            }
        }
        else
        {
            // 빈 공간 Alt+우클릭 시 전체 해제
            if (selectedCells.Count > 0)
                ClearSelectionState();
        }
    }

    private void SelectCell(Vector3Int cell)
    {
        selectedCells.Add(cell);
        if (!selectedTiles.ContainsKey(cell))
        {
            TileBase t = buildingTilemap.GetTile(cell);
            if (t != null)
            {
                selectedTiles[cell] = t;
            }
        }
        // 하이라이트 (기존 색 저장 후 적용)
        buildingTilemap.SetTileFlags(cell, TileFlags.None);
        if (!originalTileColors.ContainsKey(cell))
        {
            originalTileColors[cell] = buildingTilemap.GetColor(cell);
        }
        buildingTilemap.SetColor(cell, new Color(0.3f, 0.9f, 1f, 1f));
    }

    private void DeselectCell(Vector3Int cell)
    {
        // 색상 복구 (원본 저장값이 있으면 그걸로, 없으면 흰색)
        buildingTilemap.SetTileFlags(cell, TileFlags.None);
        if (originalTileColors.TryGetValue(cell, out var orig))
            buildingTilemap.SetColor(cell, orig);
        else
            buildingTilemap.SetColor(cell, Color.white);
        selectedCells.Remove(cell);
        selectedTiles.Remove(cell);
        originalTileColors.Remove(cell);
    }

    private void ClearAllSelectionHighlights()
    {
        foreach (var cell in selectedCells)
        {
            buildingTilemap.SetTileFlags(cell, TileFlags.None);
            if (originalTileColors.TryGetValue(cell, out var orig))
                buildingTilemap.SetColor(cell, orig);
            else
                buildingTilemap.SetColor(cell, Color.white);
        }
    }

    private void ClearSelectionState()
    {
        ClearAllSelectionHighlights();
        selectedCells.Clear();
        selectedTiles.Clear();
        originalTileColors.Clear();
    }

    private void StartGroupDrag()
    {
        if (selectedCells.Count == 0 || buildingTilemap == null)
            return;

        // 기준 셀 결정: 마우스 아래 셀 우선
        Vector3Int mouseCell = GetMouseCell();
        if (selectedCells.Contains(mouseCell))
            anchorOriginalCell = mouseCell;
        else
            foreach (var c in selectedCells) { anchorOriginalCell = c; break; }

        // 하이라이트 제거 후 타일 들어올리기
        ClearAllSelectionHighlights();

        // 선택된 모든 타일 캐싱 (누락된 경우 보강) 및 제거
        List<Vector3Int> toRemove = new List<Vector3Int>();
        foreach (var c in selectedCells)
        {
            TileBase t = buildingTilemap.GetTile(c);
            if (t != null)
            {
                selectedTiles[c] = t;
                toRemove.Add(c);
            }
        }
        foreach (var c in toRemove)
        {
            buildingTilemap.SetTile(c, null);
        }

        isGroupDragging = true;
        // 프리뷰 초기화
        ClearGroupPreview();
    }

    private void UpdateGroupPreview()
    {
        if (!isGroupDragging || previewTilemap == null)
            return;

        // 기존 프리뷰 제거
        ClearGroupPreview();

        Vector3Int currentCell = GetMouseCell();
        Vector3Int delta = new Vector3Int(currentCell.x - anchorOriginalCell.x, currentCell.y - anchorOriginalCell.y, 0);

        foreach (var kv in selectedTiles)
        {
            Vector3Int origin = kv.Key;
            TileBase tile = kv.Value;
            Vector3Int target = new Vector3Int(origin.x + delta.x, origin.y + delta.y, 2);

            bool canPlace = CanPlaceAt(target);
            previewTilemap.SetTile(target, tile);
            previewTilemap.SetTileFlags(target, TileFlags.None);
            previewTilemap.SetColor(target, canPlace ? new Color(1f, 1f, 1f, 0.6f) : new Color(1f, 0.3f, 0.3f, 0.6f));
            lastPreviewCells.Add(target);
        }
    }

    private void EndGroupDrag()
    {
        if (!isGroupDragging)
            return;

        Vector3Int dropCell = GetMouseCell();
        Vector3Int delta = new Vector3Int(dropCell.x - anchorOriginalCell.x, dropCell.y - anchorOriginalCell.y, 0);

        // 설치 가능성 체크
        List<(Vector3Int origin, Vector3Int target, TileBase tile)> placements = new List<(Vector3Int, Vector3Int, TileBase)>();
        foreach (var kv in selectedTiles)
        {
            Vector3Int origin = kv.Key;
            TileBase tile = kv.Value;
            Vector3Int target = new Vector3Int(origin.x + delta.x, origin.y + delta.y, 2);
            placements.Add((origin, target, tile));
        }

        bool allPlaceable = true;
        foreach (var p in placements)
        {
            if (!CanPlaceAt(p.target))
            {
                allPlaceable = false;
                break;
            }
        }

        // 프리뷰 제거
        ClearGroupPreview();

        if (allPlaceable)
        {
            // 신규 위치에 설치
            foreach (var p in placements)
            {
                buildingTilemap.SetTile(p.target, p.tile);
            }

            // 옵션: 배치 후 자동 선택 해제
            if (clearSelectionAfterPlace)
            {
                ClearSelectionState();
            }
            else
            {
                // 선택을 새 위치로 유지
                HashSet<Vector3Int> newSelected = new HashSet<Vector3Int>();
                Dictionary<Vector3Int, TileBase> newSelectedTiles = new Dictionary<Vector3Int, TileBase>();
                foreach (var p in placements)
                {
                    newSelected.Add(p.target);
                    newSelectedTiles[p.target] = p.tile;
                }
                selectedCells = newSelected;
                selectedTiles = newSelectedTiles;
                foreach (var c in selectedCells)
                {
                    buildingTilemap.SetTileFlags(c, TileFlags.None);
                    buildingTilemap.SetColor(c, new Color(0.3f, 0.9f, 1f, 1f));
                }
            }
        }
        else
        {
            // 원위치로 복원
            foreach (var kv in selectedTiles)
            {
                buildingTilemap.SetTile(kv.Key, kv.Value);
            }
            // 하이라이트 복구
            foreach (var c in selectedCells)
            {
                buildingTilemap.SetTileFlags(c, TileFlags.None);
                buildingTilemap.SetColor(c, new Color(0.3f, 0.9f, 1f, 1f));
            }
            Debug.Log("설치할 수 없는 위치가 있어 원위치로 되돌렸습니다.");
        }

        isGroupDragging = false;
    }

    private void CancelGroupDrag()
    {
        if (!isGroupDragging)
            return;

        // 프리뷰 제거
        ClearGroupPreview();

        // 원래 위치로 되돌리기
        foreach (var kv in selectedTiles)
        {
            buildingTilemap.SetTile(kv.Key, kv.Value);
        }
        // 하이라이트 복구
        foreach (var c in selectedCells)
        {
            buildingTilemap.SetTileFlags(c, TileFlags.None);
            buildingTilemap.SetColor(c, new Color(0.3f, 0.9f, 1f, 1f));
        }

        isGroupDragging = false;
        Debug.Log("그룹 드래그를 취소했습니다.");
    }

    private void ClearGroupPreview()
    {
        if (previewTilemap == null)
            return;
        foreach (var c in lastPreviewCells)
        {
            previewTilemap.SetTile(c, null);
            previewTilemap.SetTileFlags(c, TileFlags.None);
            previewTilemap.SetColor(c, Color.white);
        }
        lastPreviewCells.Clear();
    }

    // 클릭된 x,y에서 모든 Z를 탐색하여 실제로 존재하는 건물 타일을 찾는다 (가장 높은 Z 우선)
    private bool TryFindBuildingAtXY(int x, int y, out Vector3Int foundCell, out TileBase foundTile)
    {
        foundCell = new Vector3Int(x, y, 0);
        foundTile = null;
        if (buildingTilemap == null)
            return false;

        BoundsInt bounds = buildingTilemap.cellBounds;
        for (int z = bounds.zMax - 1; z >= bounds.zMin; z--)
        {
            Vector3Int pos = new Vector3Int(x, y, z);
            if (buildingTilemap.HasTile(pos))
            {
                foundCell = pos;
                foundTile = buildingTilemap.GetTile(pos);
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

        // 건물은 Z=2에서 확인
        Vector3Int buildingCell = new Vector3Int(cell.x, cell.y, 2);
        bool emptyBuilding = buildingTilemap != null && !buildingTilemap.HasTile(buildingCell);

        return hasGround && emptyBuilding;
    }
}