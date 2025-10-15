using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 건물 타일의 드래그 앤 드롭 기능을 관리하는 컨트롤러
/// 사용자가 건물을 클릭하여 드래그하고, 유효한 위치에 드롭할 수 있도록 함
/// </summary>
public class DragDropController : MonoBehaviour
{
    [Header("타일맵 설정")]
    [SerializeField] private Grid grid; // 그리드 시스템 (타일 좌표계 관리)
    [SerializeField] private Tilemap groundTilemap; // 그라운드 타일맵 (건물을 설치할 수 있는 바닥)
    [SerializeField] private Tilemap buildingTilemap; // 건물 타일맵 (실제 건물들이 배치되는 레이어)
    
    [Header("드래그 설정")]
    [SerializeField] private Camera mainCamera; // 마우스 좌표를 월드 좌표로 변환하기 위한 카메라
    
    // 드래그 상태 관리 변수들
    private bool isDragging = false; // 현재 드래그 중인지 여부
    private Vector3Int originalCell; // 드래그 시작 전 건물이 있던 원래 위치
    private TileBase draggedTile; // 현재 드래그 중인 타일 데이터
    private Vector3 offset; // 마우스와 타일 중심점 간의 오프셋 (현재 미사용)
    
    /// <summary>
    /// 초기화 메서드 - 필요한 컴포넌트들을 자동으로 찾아서 설정
    /// Inspector에서 수동으로 할당하지 않은 경우 자동으로 검색하여 할당
    /// </summary>
    void Start()
    {
        // 메인 카메라가 할당되지 않은 경우 자동으로 찾기
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        // Grid 컴포넌트 자동 검색 (타일 좌표계 관리용)
        if (grid == null)
        {
            grid = FindObjectOfType<Grid>();
            if (grid == null)
            {
                Debug.LogError("씬에서 Grid를 찾을 수 없습니다!");
            }
        }
        
        // Ground 타일맵 자동 검색 (건물을 설치할 수 있는 바닥 레이어)
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
        
        // Building 타일맵 자동 검색 (건물들이 배치되는 레이어)
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
        
        // 디버그용: Building 타일맵에 있는 모든 타일의 위치와 개수 확인
        if (buildingTilemap != null)
        {
            int tileCount = 0;
            BoundsInt bounds = buildingTilemap.cellBounds;
            Debug.Log($"Building 타일맵 bounds: {bounds}");
            
            // 모든 Z 레벨에서 타일 검색
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
        
        // 초기화 결과 확인 및 로그 출력
        Debug.Log($"Grid: {(grid != null ? "찾음" : "없음")}");
        Debug.Log($"Ground Tilemap: {(groundTilemap != null ? "찾음" : "없음")}");
        Debug.Log($"Building Tilemap: {(buildingTilemap != null ? "찾음" : "없음")}");
    }
    
    /// <summary>
    /// 매 프레임마다 호출되는 업데이트 메서드
    /// 마우스 입력 처리를 담당
    /// </summary>
    void Update()
    {
        HandleMouseInput();
    }
    
    /// <summary>
    /// 마우스 입력을 처리하는 메서드
    /// 좌클릭: 드래그 시작/종료, 우클릭: 드래그 취소
    /// </summary>
    private void HandleMouseInput()
    {
        // 마우스 좌클릭으로 드래그 시작 (건물 선택)
        if (Input.GetMouseButtonDown(0))
        {
            StartDrag();
        }
        
        // 드래그 중일 때 마우스 위치에 따라 건물 위치 업데이트
        if (isDragging)
        {
            UpdateDragPosition();
        }
        
        // 마우스 좌클릭 해제로 드래그 종료 (건물 설치)
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
        
        // 우클릭으로 드래그 취소 (원래 위치로 되돌리기)
        if (Input.GetMouseButtonDown(1) && isDragging)
        {
            CancelDrag();
        }
    }
    
    /// <summary>
    /// 드래그를 시작하는 메서드
    /// 마우스 클릭 위치에 건물이 있는지 확인하고, 있으면 드래그 상태로 전환
    /// </summary>
    private void StartDrag()
    {
        // 마우스 위치를 그리드 좌표로 변환
        Vector3Int cell = GetMouseCell();
        
        // 디버그 로그 출력
        Debug.Log($"마우스 클릭 위치: {cell}");
        Debug.Log($"Building Tilemap이 null인가? {buildingTilemap == null}");
        
        if (buildingTilemap != null)
        {
            Debug.Log($"해당 위치에 타일이 있는가? {buildingTilemap.HasTile(cell)}");
        }
        
        // 건물 타일맵에서 클릭한 위치에 타일이 있는지 확인
        if (buildingTilemap != null && buildingTilemap.HasTile(cell))
        {
            // 드래그할 타일 데이터 저장
            draggedTile = buildingTilemap.GetTile(cell);
            originalCell = cell; // 원래 위치 저장 (취소 시 되돌리기용)
            
            // 원래 위치에서 타일 제거 (건물을 "들고 있는" 상태로 만듦)
            buildingTilemap.SetTile(cell, null);
            
            // 드래그 상태로 전환
            isDragging = true;
            
            Debug.Log("건물을 잡았습니다!");
        }
        else
        {
            Debug.Log("건물을 잡을 수 없습니다. 건물 타일을 정확히 클릭해주세요.");
        }
    }
    
    /// <summary>
    /// 드래그 중일 때 건물 위치를 업데이트하는 메서드
    /// 현재는 마우스 위치만 추적하고, 실제 시각적 피드백은 미구현
    /// </summary>
    private void UpdateDragPosition()
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // 2D 게임이므로 z 좌표는 0으로 고정
        
        // 현재는 마우스 위치만 추적하고 있음
        // 향후 개선: 건물 오브젝트를 마우스 위치에 따라 이동시키는 시각적 피드백 추가 가능
        // 예: 투명한 건물 프리팹을 마우스 위치에 배치하여 설치 예정 위치 표시
    }
    
    /// <summary>
    /// 드래그를 종료하고 건물을 설치하는 메서드
    /// 설치 가능한 위치인지 확인 후 건물을 배치하거나 원래 위치로 되돌림
    /// </summary>
    private void EndDrag()
    {
        // 마우스 위치를 그리드 좌표로 변환
        Vector3Int dropCell = GetMouseCell();
        
        // 설치 가능한 위치인지 확인 (그라운드가 있고, 건물 위치가 비어있는지)
        if (CanPlaceAt(dropCell))
        {
            // 건물을 새로운 위치에 설치
            buildingTilemap.SetTile(dropCell, draggedTile);
            Debug.Log($"건물을 ({dropCell.x}, {dropCell.y})에 설치했습니다!");
        }
        else
        {
            // 설치할 수 없는 위치면 원래 위치로 되돌리기
            buildingTilemap.SetTile(originalCell, draggedTile);
            Debug.Log("설치할 수 없는 위치입니다. 원래 위치로 되돌렸습니다.");
        }
        
        // 드래그 상태 초기화
        isDragging = false;
        draggedTile = null;
    }
    
    /// <summary>
    /// 드래그를 취소하고 건물을 원래 위치로 되돌리는 메서드
    /// 우클릭으로 호출되며, 건물을 원래 위치에 복원
    /// </summary>
    private void CancelDrag()
    {
        // 건물을 원래 위치로 되돌리기
        if (draggedTile != null)
        {
            buildingTilemap.SetTile(originalCell, draggedTile);
        }
        
        // 드래그 상태 초기화
        isDragging = false;
        draggedTile = null;
        
        Debug.Log("드래그를 취소했습니다.");
    }
    
    /// <summary>
    /// 마우스 위치를 그리드 좌표로 변환하는 유틸리티 메서드
    /// 마우스 스크린 좌표를 월드 좌표로 변환한 후 그리드 셀 좌표로 변환
    /// </summary>
    /// <returns>변환된 그리드 셀 좌표 (Z=2로 설정됨)</returns>
    private Vector3Int GetMouseCell()
    {
        // 마우스 위치를 월드 좌표로 변환
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0; // 2D 게임이므로 z 좌표는 0으로 고정
        
        // 월드 좌표를 그리드 셀 좌표로 변환
        Vector3Int cell = grid.WorldToCell(mouseWorldPos);
        
        // Z Position을 2로 설정 (Tile Palette의 Z Position과 동일하게 맞춤)
        // 이는 건물 레이어가 Z=2에 위치하기 때문
        cell.z = 2;
        
        return cell;
    }
    
    /// <summary>
    /// 지정된 위치에 건물을 설치할 수 있는지 확인하는 메서드
    /// 설치 조건: 그라운드가 있어야 하고, 건물 위치가 비어있어야 함
    /// </summary>
    /// <param name="cell">확인할 그리드 셀 좌표</param>
    /// <returns>설치 가능하면 true, 불가능하면 false</returns>
    private bool CanPlaceAt(Vector3Int cell)
    {
        // 그라운드 레이어(Z=0)에 바닥 타일이 있는지 확인
        Vector3Int groundCell = new Vector3Int(cell.x, cell.y, 0);
        bool hasGround = groundTilemap != null && groundTilemap.HasTile(groundCell);
        
        // 건물 레이어(Z=2)에 다른 건물이 없는지 확인 (비어있는지)
        Vector3Int buildingCell = new Vector3Int(cell.x, cell.y, 2);
        bool emptyBuilding = buildingTilemap != null && !buildingTilemap.HasTile(buildingCell);
        
        // 두 조건을 모두 만족해야 설치 가능
        return hasGround && emptyBuilding;
    }
}