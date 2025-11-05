using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

	[Header("건물(발자국) 설정")]
	[SerializeField] private Vector2Int buildingSize = new Vector2Int(1, 1); // 가로x세로 (예: 2x2)
	[SerializeField] private bool allowRotation = true; // R키 회전 허용
	[SerializeField] private Color previewValidColor = new Color(1f, 1f, 1f, 0.6f);
	[SerializeField] private Color previewInvalidColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    private bool isDragging = false;
	private Vector3Int originalOriginCell;
	private TileBase draggedTile; // 단일 타일 드래그 시 사용 또는 기본 미리보기 타일
	private Dictionary<Vector3Int, TileBase> draggedTilesByOffset; // 오프셋->타일 (멀티셀 이동 시 원본 복구/재배치)
	private List<Vector3Int> lastPreviewCells;
	private int rotation = 0; // 0,90,180,270

	// 인스턴스 레지스트리
	private struct BuildingInstance
	{
		public int id; // 유니크 ID
		public Vector3Int origin; // 원점(좌하단 기준)
		public Vector2Int size; // 가로x세로
		public int rotation; // 0/90/180/270
		public List<Vector3Int> offsets; // 원점 기준 오프셋들
	}

	private int nextInstanceId = 1;
	private readonly Dictionary<int, BuildingInstance> instanceIdToData = new Dictionary<int, BuildingInstance>();
	private readonly Dictionary<Vector3Int, int> cellToInstanceId = new Dictionary<Vector3Int, int>(); // Z=2 좌표 사용

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
		UpdatePreview();
    }

	private void HandleMouseInput()
    {
        // 마우스 좌클릭으로 드래그 시작
		if (Input.GetMouseButtonDown(1))
        {
            StartDrag();
        }

        // 드래그 중일 때 마우스 따라가기
        if (isDragging)
        {
            UpdateDragPosition();
        }

		// 마우스 좌클릭 해제로 드래그 종료
		if (Input.GetMouseButtonUp(1) && isDragging)
        {
            EndDrag();
        }

        // 우클릭으로 드래그 취소
        if (Input.GetKeyDown(KeyCode.Escape) && isDragging)
        {
            CancelDrag();
        }

		// 회전 (R)
		if (allowRotation && Input.GetKeyDown(KeyCode.R) && isDragging)
		{
			rotation = (rotation + 90) % 360;
		}
    }

	private void StartDrag()
    {
		Vector3Int cellForPlacement = GetMouseCell(); // 현재 설정된 배치 Z(예: 2)

		Debug.Log($"마우스 클릭 위치(배치 Z): {cellForPlacement}");
		Debug.Log($"Building Tilemap이 null인가? {buildingTilemap == null}");

		lastPreviewCells = lastPreviewCells ?? new List<Vector3Int>(16);
		draggedTilesByOffset = new Dictionary<Vector3Int, TileBase>();

		// 클릭 지점(x,y)에서 실제 존재하는 타일 여부 확인 (참조 타일 확보 용)
		if (buildingTilemap != null && TryFindBuildingAtXY(cellForPlacement.x, cellForPlacement.y, out Vector3Int foundCell, out TileBase foundTile))
		{
			// 1) 레지스트리에 있으면 해당 인스턴스 전체를 집어 든다
			if (TryGetInstanceAtCell(foundCell, out var instance))
			{
				originalOriginCell = instance.origin;
				rotation = instance.rotation; // 인스턴스의 회전 계승
				for (int i = 0; i < instance.offsets.Count; i++)
				{
					Vector3Int worldCell = new Vector3Int(instance.origin.x + instance.offsets[i].x, instance.origin.y + instance.offsets[i].y, 2);
					if (buildingTilemap.HasTile(worldCell))
					{
						var tile = buildingTilemap.GetTile(worldCell);
						draggedTilesByOffset[instance.offsets[i]] = tile;
						buildingTilemap.SetTile(worldCell, null);
						// 셀→인스턴스 맵에서 제거(들어 올림)
						cellToInstanceId.Remove(worldCell);
					}
				}
			}
			else
			{
				// 2) 레지스트리 미보유: 현재 buildingSize/rotation을 발자국으로 사용(폴백)
				originalOriginCell = foundCell;
				draggedTile = foundTile; // 프리뷰 기본 타일
				var offsets = GetRotatedOffsets(buildingSize, rotation);
				int collected = 0;
				for (int i = 0; i < offsets.Count; i++)
				{
					Vector3Int worldCell = new Vector3Int(originalOriginCell.x + offsets[i].x, originalOriginCell.y + offsets[i].y, 2);
					if (buildingTilemap.HasTile(worldCell))
					{
						var tile = buildingTilemap.GetTile(worldCell);
						draggedTilesByOffset[offsets[i]] = tile;
						buildingTilemap.SetTile(worldCell, null);
						collected++;
					}
				}
				if (collected == 0)
				{
					// 클릭한 셀만 단일 타일로 간주하여 이동
					draggedTilesByOffset[new Vector3Int(0, 0, 0)] = foundTile;
					buildingTilemap.SetTile(foundCell, null);
				}
			}

			isDragging = true;
			Debug.Log($"건물을 잡았습니다! 원점: {originalOriginCell}, 수거 타일 수: {draggedTilesByOffset.Count}");
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
		if (!isDragging || previewTilemap == null || draggedTilesByOffset == null)
            return;

		Vector3Int origin = GetMouseCell();

		// 이전 미리보기 지우기
		ClearPreview();

		// 대상 셀들 계산
		var offsets = draggedTilesByOffset.Keys.ToList();
		bool canPlace = CanPlaceFootprintAt(origin, offsets);
		Color previewColor = canPlace ? previewValidColor : previewInvalidColor;

		for (int i = 0; i < offsets.Count; i++)
		{
			Vector3Int cell = new Vector3Int(origin.x + offsets[i].x, origin.y + offsets[i].y, 2);
			// 프리뷰 타일: 원본 타일이 있으면 동일하게, 없으면 기본 draggedTile
			TileBase tile = draggedTilesByOffset[offsets[i]] != null ? draggedTilesByOffset[offsets[i]] : draggedTile;
			previewTilemap.SetTile(cell, tile);
			previewTilemap.SetTileFlags(cell, TileFlags.None);
			previewTilemap.SetColor(cell, previewColor);
			lastPreviewCells.Add(cell);
		}
    }

	private void EndDrag()
    {
		Vector3Int dropOrigin = GetMouseCell();

		// 미리보기 제거
		ClearPreview();

		var offsets = draggedTilesByOffset.Keys.ToList();
		bool canPlace = CanPlaceFootprintAt(dropOrigin, offsets);
		if (canPlace)
		{
			// 배치 + 레지스트리 등록
			int instanceId = RegisterInstance(dropOrigin, offsets, rotation);
			for (int i = 0; i < offsets.Count; i++)
			{
				Vector3Int cell = new Vector3Int(dropOrigin.x + offsets[i].x, dropOrigin.y + offsets[i].y, 2);
				TileBase tile = draggedTilesByOffset[offsets[i]];
				buildingTilemap.SetTile(cell, tile);
				cellToInstanceId[cell] = instanceId;
			}
			Debug.Log($"건물을 {offsets.Count}셀로 배치했습니다. 원점: {dropOrigin}");
		}
		else
		{
			// 설치 불가: 원위치 복구 (레지스트리 원복)
			int instanceId = RegisterInstance(originalOriginCell, offsets, rotation);
			for (int i = 0; i < offsets.Count; i++)
			{
				Vector3Int cell = new Vector3Int(originalOriginCell.x + offsets[i].x, originalOriginCell.y + offsets[i].y, 2);
				TileBase tile = draggedTilesByOffset[offsets[i]];
				buildingTilemap.SetTile(cell, tile);
				cellToInstanceId[cell] = instanceId;
			}
			Debug.Log("설치할 수 없는 위치입니다. 원래 위치로 되돌렸습니다.");
		}

		// 드래그 상태 초기화
		isDragging = false;
		draggedTile = null;
		draggedTilesByOffset = null;
    }

	private void CancelDrag()
    {
        // 미리보기 제거
        ClearPreview();

		// 원래 위치로 되돌리기 (멀티셀)
		if (draggedTilesByOffset != null)
		{
			var offsets = draggedTilesByOffset.Keys.ToList();
			int instanceId = RegisterInstance(originalOriginCell, offsets, rotation);
			for (int i = 0; i < offsets.Count; i++)
			{
				Vector3Int cell = new Vector3Int(originalOriginCell.x + offsets[i].x, originalOriginCell.y + offsets[i].y, 2);
				TileBase tile = draggedTilesByOffset[offsets[i]];
				buildingTilemap.SetTile(cell, tile);
				cellToInstanceId[cell] = instanceId;
			}
		}

        // 드래그 상태 초기화
        isDragging = false;
		draggedTile = null;
		draggedTilesByOffset = null;

        Debug.Log("드래그를 취소했습니다.");
    }

	private void ClearPreview()
    {
		if (previewTilemap == null || lastPreviewCells == null) return;
		for (int i = 0; i < lastPreviewCells.Count; i++)
		{
			previewTilemap.SetTile(lastPreviewCells[i], null);
			previewTilemap.SetTileFlags(lastPreviewCells[i], TileFlags.None);
			previewTilemap.SetColor(lastPreviewCells[i], Color.white);
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

	// 클릭된 셀에서 인스턴스 찾기: 레지스트리에 있으면 해당 인스턴스 반환
	private bool TryGetInstanceAtCell(Vector3Int buildingCell, out BuildingInstance instance)
	{
		instance = default;
		if (cellToInstanceId.TryGetValue(buildingCell, out int id))
		{
			if (instanceIdToData.TryGetValue(id, out instance)) return true;
		}
		return false;
	}

	// 인스턴스 등록(새 ID 발급 및 데이터 저장)
	private int RegisterInstance(Vector3Int origin, List<Vector3Int> offsets, int rotationDeg)
	{
		int id = nextInstanceId++;
		// size는 offsets의 AABB로 역산
		int minX = 0, minY = 0, maxX = 0, maxY = 0;
		for (int i = 0; i < offsets.Count; i++)
		{
			if (i == 0)
			{
				minX = maxX = offsets[i].x; minY = maxY = offsets[i].y;
			}
			else
			{
				if (offsets[i].x < minX) minX = offsets[i].x;
				if (offsets[i].x > maxX) maxX = offsets[i].x;
				if (offsets[i].y < minY) minY = offsets[i].y;
				if (offsets[i].y > maxY) maxY = offsets[i].y;
			}
		}
		Vector2Int size = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
		BuildingInstance b = new BuildingInstance
		{
			id = id,
			origin = origin,
			size = size,
			rotation = rotationDeg,
			offsets = offsets.ToList()
		};
		instanceIdToData[id] = b;
		return id;
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

	// 발자국(오프셋) 기반 설치 가능 여부
	private bool CanPlaceFootprintAt(Vector3Int origin, List<Vector3Int> offsets)
	{
		for (int i = 0; i < offsets.Count; i++)
		{
			Vector3Int cell = new Vector3Int(origin.x + offsets[i].x, origin.y + offsets[i].y, 0);
			// 그라운드 체크(Z=0)
			bool hasGround = groundTilemap != null && groundTilemap.HasTile(cell);
			if (!hasGround) return false;
			// 빌딩 체크(Z=2)
			Vector3Int buildingCell = new Vector3Int(origin.x + offsets[i].x, origin.y + offsets[i].y, 2);
			bool emptyBuilding = buildingTilemap != null && !buildingTilemap.HasTile(buildingCell);
			if (!emptyBuilding) return false;
		}
		return true;
	}

	// 기준: 좌하단(0,0)~(w-1,h-1), 회전은 (0,90,180,270)에서 원점(0,0) 기준 회전
	private List<Vector3Int> GetRotatedOffsets(Vector2Int size, int rotationDeg)
	{
		List<Vector3Int> offsets = new List<Vector3Int>(size.x * size.y);
		for (int y = 0; y < size.y; y++)
		{
			for (int x = 0; x < size.x; x++)
			{
				offsets.Add(new Vector3Int(x, y, 0));
			}
		}

		if ((rotationDeg % 360) == 0) return offsets;

		List<Vector3Int> rotated = new List<Vector3Int>(offsets.Count);
		for (int i = 0; i < offsets.Count; i++)
		{
			Vector3Int o = offsets[i];
			switch ((rotationDeg % 360 + 360) % 360)
			{
				case 90: rotated.Add(new Vector3Int(-o.y, o.x, 0)); break;
				case 180: rotated.Add(new Vector3Int(-o.x, -o.y, 0)); break;
				case 270: rotated.Add(new Vector3Int(o.y, -o.x, 0)); break;
			}
		}
		return rotated;
	}
}