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
    
    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
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
        UpdatePreview();
    }
    
    private void HandleMouseInput()
    {
        // 마우스 좌클릭으로 드래그 시작
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag();
        }
        
        // 드래그 중일 때 마우스 따라가기
        if (isDragging)
        {
            UpdateDragPosition();
        }
        
        // 마우스 좌클릭 해제로 드래그 종료
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
        
        // 우클릭으로 드래그 취소
        if (Input.GetMouseButtonDown(1) && isDragging)
        {
            CancelDrag();
        }
    }
    
    private void StartDrag()
    {
        Vector3Int cell = GetMouseCell();
        
        Debug.Log($"마우스 클릭 위치: {cell}");
        Debug.Log($"Building Tilemap이 null인가? {buildingTilemap == null}");
        
        if (buildingTilemap != null)
        {
            Debug.Log($"해당 위치에 타일이 있는가? {buildingTilemap.HasTile(cell)}");
        }
        
        // 건물 타일맵에서 클릭한 위치에 타일이 있는지 확인
        if (buildingTilemap != null && buildingTilemap.HasTile(cell))
        {
            // 드래그할 타일 저장
            draggedTile = buildingTilemap.GetTile(cell);
            originalCell = cell;
            
            // 원래 위치에서 타일 제거 (들고 있는 상태)
            buildingTilemap.SetTile(cell, null);
            
            // 드래그 시작
            isDragging = true;
            
            Debug.Log("건물을 잡았습니다!");
        }
        else
        {
            Debug.Log("건물을 잡을 수 없습니다. 건물 타일을 정확히 클릭해주세요.");
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
        }
        
        // 현재 위치에 미리보기 표시
        previewTilemap.SetTile(currentCell, draggedTile);
        
        // 설치 가능 여부에 따라 색상 변경
        bool canPlace = CanPlaceAt(currentCell);
        Color previewColor = canPlace ? 
            new Color(1f, 1f, 1f, 0.6f) :  // 흰색 반투명 (설치 가능)
            new Color(1f, 0.3f, 0.3f, 0.6f); // 빨간색 반투명 (설치 불가)
            
        previewTilemap.SetColor(currentCell, previewColor);
        
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
            previewTilemap.SetColor(lastPreviewCell, Color.white);
            lastPreviewCell = Vector3Int.zero;
        }
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