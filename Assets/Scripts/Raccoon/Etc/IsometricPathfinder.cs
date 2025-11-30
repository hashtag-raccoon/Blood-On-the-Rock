using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

// 이거 문자가 깨져있어서 우선 Gemini 이용해서 주석이랑 서머리 작성함,,,!!
// 추후 다시 작성할예정,,

/// <summary>
/// 유니티 타일맵 시스템에서 A* 알고리즘을 사용하여 길찾기를 수행하는 클래스입니다.
/// 아이소메트릭(Isometric) 및 일반 그리드 맵에서 모두 사용할 수 있습니다.
/// </summary>
public class IsometricPathfinder : MonoBehaviour
{
    #region 변수 및 설정 (Variables & Settings)

    [Header("Map Settings")]
    /// <summary>
    /// 여러 타일맵을 관리하는 배열입니다. 특정 타일맵에서 길찾기를 수행할 때 사용합니다.
    /// </summary>
    [SerializeField] private Tilemap[] tilemaps;

    /// <summary>
    /// 이동 가능한 타일의 종류를 지정합니다. 
    /// <para>null일 경우 타일이 존재하는 모든 곳을 이동 가능으로 간주하며, 특정 타일(예: 땅)을 지정하면 그 타일 위로만 이동합니다.</para>
    /// </summary>
    [SerializeField] private TileBase[] walkableTile;
    

    /// <summary>
    /// 현재 길찾기 연산 중에 생성된 노드들을 관리하는 딕셔너리입니다.
    /// <para>Key: 타일 좌표(Vector3Int), Value: 해당 위치의 노드 객체</para>
    /// </summary>
    private Dictionary<Vector3Int, Node> nodes = new Dictionary<Vector3Int, Node>();

    #endregion

    #region 내부 클래스 (Inner Classes)

    /// <summary>
    /// A* 알고리즘 연산을 위해 각 타일 위치의 정보를 담는 노드 클래스입니다.
    /// </summary>
    private class Node
    {
        /// <summary>
        /// 그리드 상의 좌표입니다.
        /// </summary>
        public Vector3Int position;

        /// <summary>
        /// 길찾기 경로상에서 이 노드의 바로 이전 노드(부모)입니다. 경로 역추적에 사용됩니다.
        /// </summary>
        public Node parent;

        /// <summary>
        /// 시작 지점부터 현재 노드까지 이동하는 데 드는 비용(Cost)입니다.
        /// </summary>
        public float gCost;

        /// <summary>
        /// 현재 노드에서 목표 지점까지의 예상 비용(Heuristic)입니다.
        /// </summary>
        public float hCost;

        /// <summary>
        /// G 비용과 H 비용의 합으로, 경로 탐색의 우선순위를 결정하는 최종 점수입니다.
        /// </summary>
        public float fCost => gCost + hCost;

        public Node(Vector3Int pos)
        {
            position = pos;
        }
    }

    #endregion

    #region 핵심 길찾기 로직 (Core Pathfinding Logic)

    // FindPath 메서드 오버로드해서 구현을 했음
    // 3번째 매개변수로 특정 타일맵을 넣어주면 해당 타일맵에서 길찾기를 수행함
    // 만약 시작점과 끝점만 넣어주면 기본 타일맵에서 길찾기를 수행함

    /// <summary>
    /// A* 알고리즘을 사용하여 시작 위치에서 목표 위치까지의 최단 경로를 계산합니다.
    /// </summary>
    /// <param name="start">시작 타일 좌표 (Grid 좌표)</param>
    /// <param name="target">목표 타일 좌표 (Grid 좌표)</param>
    /// <returns>경로를 구성하는 타일 좌표들의 리스트 (경로가 없으면 null 반환)</returns>
    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target)
    {
        return FindPath(start, target, 0, 0);
    }

    /// <summary>
    /// A* 알고리즘을 사용하여 특정 타일맵에서 시작 위치에서 목표 위치까지의 최단 경로를 계산합니다.
    /// </summary>
    /// <param name="start">시작 타일 좌표 (Grid 좌표)</param>
    /// <param name="target">목표 타일 좌표 (Grid 좌표)</param>
    /// <param name="Tilemap_index">길찾기를 수행할 타일맵 인덱스</param>
    /// <param name="targetTilemap_index">walkableTile 배열의 인덱스</param>
    /// <returns>경로를 구성하는 타일 좌표들의 리스트 (경로가 없으면 null 반환)</returns>
    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target, int Tilemap_index, int targetTilemap_index)
    {
        Tilemap targetTilemap = tilemaps[Tilemap_index];
        if (targetTilemap == null)
        {
            Debug.LogError("[Pathfinder] 타일맵이 설정되지 않았습니다.");
            return null;
        }

        // 1. 유효성 검사: 시작점이나 목표점이 이동 불가능한 곳인지 확인
        if (!IsWalkable(start, targetTilemap, targetTilemap_index))
        {
            Debug.LogWarning($"[Pathfinder] 시작 위치가 이동 불가능한 타일입니다: {start}");
            return null;
        }
        if (!IsWalkable(target, targetTilemap, targetTilemap_index))
        {
            //Debug.LogWarning($"[Pathfinder] 목표 위치가 이동 불가능한 타일입니다: {target}");
            return null;
        }

        // 2. 초기화
        nodes.Clear();                                  // 이전 탐색 데이터 초기화
        List<Node> openSet = new List<Node>();          // 탐색할 노드 목록 (Open List)
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>(); // 이미 탐색을 마친 노드 목록 (Closed List)

        // 시작 노드 설정
        Node startNode = new Node(start);
        startNode.gCost = 0;
        startNode.hCost = GetDistance(start, target);
        
        openSet.Add(startNode);
        nodes[start] = startNode;

        // 3. 탐색 루프 (Open Set이 빌 때까지 반복)
        while (openSet.Count > 0)
        {
            // 3-1. Open Set에서 F Cost가 가장 낮은(가장 유망한) 노드 선택
            Node currentNode = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                // F Cost가 작거나, F Cost가 같다면 H Cost(목표까지 남은 거리)가 작은 것을 선택
                if (openSet[i].fCost < currentNode.fCost ||
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);       // 처리 중인 노드는 Open Set에서 제거
            closedSet.Add(currentNode.position); // Closed Set에 추가 (재방문 방지)

            // 3-2. 목표 도달 확인
            if (currentNode.position == target)
            {
                return RetracePath(startNode, currentNode); // 경로 생성 후 반환
            }

            // 3-3. 주변 이웃 노드 탐색
            foreach (Vector3Int neighborPos in GetNeighbors(currentNode.position))
            {
                // 이동 불가능하거나 이미 닫힌 목록에 있다면 스킵
                if (!IsWalkable(neighborPos, targetTilemap, targetTilemap_index) || closedSet.Contains(neighborPos))
                    continue;

                // G Cost 계산: 현재까지의 거리 + 이웃까지의 거리 (여기선 인접하므로 거리는 보통 1)
                float newGCost = currentNode.gCost + GetDistance(currentNode.position, neighborPos);

                // 노드 캐싱 확인 및 생성
                if (!nodes.ContainsKey(neighborPos))
                {
                    nodes[neighborPos] = new Node(neighborPos);
                }
                Node neighborNode = nodes[neighborPos];

                // 더 짧은 경로를 발견했거나, 아직 Open Set에 없는 경우 업데이트
                if (newGCost < neighborNode.gCost || !openSet.Contains(neighborNode))
                {
                    neighborNode.gCost = newGCost;
                    neighborNode.hCost = GetDistance(neighborPos, target); // 휴리스틱 재계산
                    neighborNode.parent = currentNode; // 부모 노드 설정 (경로 추적용)

                    if (!openSet.Contains(neighborNode))
                    {
                        openSet.Add(neighborNode);
                    }
                }
            }
        }

        // 루프를 빠져나왔다면 경로를 찾지 못한 것임
        return null;
    }

    /// <summary>
    /// 목표 노드에서 시작 노드까지 부모를 따라 역추적하여 최종 경로 리스트를 생성합니다.
    /// </summary>
    /// <param name="startNode">경로의 시작 노드</param>
    /// <param name="endNode">경로의 끝 노드</param>
    /// <returns>시작점부터 끝점까지 순서대로 정렬된 좌표 리스트</returns>
    private List<Vector3Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Node currentNode = endNode;

        // 목표 지점부터 시작 지점까지 거슬러 올라감
        while (currentNode != startNode)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }

        path.Reverse(); // 역순으로 담겼으므로 뒤집어서 [시작 -> 끝] 순서로 변경
        return path;
    }

    #endregion

    #region 헬퍼 메서드 (Helper Methods: Distance, Neighbor, Walkable)

    /// <summary>
    /// 주어진 위치 기준 상하좌우 4방향의 이웃 좌표를 반환합니다.
    /// <para>아이소메트릭 뷰에서도 그리드 논리 좌표는 직교 좌표계와 동일하게 작동합니다.</para>
    /// </summary>
    /// <param name="pos">중심 좌표</param>
    /// <returns>이웃 좌표 리스트</returns>
    private List<Vector3Int> GetNeighbors(Vector3Int pos)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>
        {
            pos + new Vector3Int(1, 0, 0),   // 우
            pos + new Vector3Int(-1, 0, 0),  // 좌
            pos + new Vector3Int(0, 1, 0),   // 상
            pos + new Vector3Int(0, -1, 0)   // 하
        };

        return neighbors;
    }

    /// <summary>
    /// 두 타일 사이의 거리를 계산합니다 (Heuristic).
    /// <para>4방향 이동을 기준으로 하므로 맨해튼 거리(Manhattan Distance) 공식을 사용합니다.</para>
    /// </summary>
    /// <param name="a">좌표 A</param>
    /// <param name="b">좌표 B</param>
    /// <returns>예상 거리 비용</returns>
    private float GetDistance(Vector3Int a, Vector3Int b)
    {
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);

        // 4방향 이동 시에는 대각선 이동이 불가능하므로 가로+세로 거리를 합산함 (맨해튼 거리)
        return dx + dy;
        
        // 만약 8방향(대각선) 이동을 허용한다면 아래  Chebyshev 거리 등을 고려해야 함
        // return Mathf.Max(dx, dy);
    }

    /// <summary>
    /// 해당 좌표의 타일이 이동 가능한지 확인합니다.
    /// </summary>
    /// <param name="pos">확인할 좌표</param>
    /// <returns>이동 가능 여부</returns>
    private bool IsWalkable(Vector3Int pos)
    {
        return IsWalkable(pos, tilemaps != null && tilemaps.Length > 0 ? tilemaps[0] : null, 0);
    }

    /// <summary>
    /// 특정 타일맵에서 해당 좌표의 타일이 이동 가능한지 확인합니다.
    /// </summary>
    /// <param name="pos">확인할 좌표</param>
    /// <param name="targetTilemap">검사할 타일맵</param>
    /// <param name="targetTilemap_index">walkableTile 배열의 인덱스</param>
    /// <returns>이동 가능 여부</returns>
    private bool IsWalkable(Vector3Int pos, Tilemap targetTilemap, int targetTilemap_index)
    {
        if (targetTilemap == null)
        {
            return false;
        }

        TileBase tile = targetTilemap.GetTile(pos);

        // walkableTile 배열이 설정되어 있고, 인덱스가 유효하다면 해당 타일과 일치해야만 이동 가능
        if (walkableTile != null && targetTilemap_index >= 0 && targetTilemap_index < walkableTile.Length && walkableTile[targetTilemap_index] != null)
        {
            return tile == walkableTile[targetTilemap_index];
        }

        // walkableTile이 설정되지 않았다면, 타일이 존재하기만 하면 이동 가능 (빈 공간은 이동 불가)
        return tile != null;
    }

    #endregion

    #region 유틸리티 & 디버그 (Utilities & Debug)

    /// <summary>
    /// 타일맵 배열에서 인덱스로 타일맵을 가져옵니다.
    /// </summary>
    /// <param name="index">타일맵 배열의 인덱스</param>
    /// <returns>해당 인덱스의 타일맵 (없으면 null)</returns>
    public Tilemap GetTilemapByIndex(int index)
    {
        if (tilemaps == null || index < 0 || index >= tilemaps.Length)
        {
            Debug.LogWarning($"[Pathfinder] 유효하지 않은 타일맵 인덱스: {index}");
            return null;
        }
        return tilemaps[index];
    }

    /// <summary>
    /// 월드 좌표(Vector3)를 그리드 셀 좌표(Vector3Int)로 변환합니다.
    /// </summary>
    /// <param name="worldPosition">월드 상의 위치</param>
    /// <returns>해당 위치의 그리드 좌표</returns>
    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return tilemaps != null && tilemaps.Length > 0 ? tilemaps[0].WorldToCell(worldPosition) : Vector3Int.zero;
    }

    /// <summary>
    /// 특정 타일맵에서 월드 좌표(Vector3)를 그리드 셀 좌표(Vector3Int)로 변환합니다.
    /// </summary>
    /// <param name="worldPosition">월드 상의 위치</param>
    /// <param name="targetTilemap">변환할 타일맵</param>
    /// <returns>해당 위치의 그리드 좌표</returns>
    public Vector3Int WorldToCell(Vector3 worldPosition, Tilemap targetTilemap)
    {
        if (targetTilemap == null)
        {
            Debug.LogWarning("[Pathfinder] 타일맵이 null입니다. 기본 타일맵을 사용합니다.");
            return tilemaps != null && tilemaps.Length > 0 ? tilemaps[0].WorldToCell(worldPosition) : Vector3Int.zero;
        }
        return targetTilemap.WorldToCell(worldPosition);
    }

    /// <summary>
    /// 그리드 셀 좌표(Vector3Int)를 월드 좌표(Vector3)로 변환합니다.
    /// </summary>
    /// <param name="cellPosition">그리드 좌표</param>
    /// <returns>해당 그리드의 월드 중심 좌표</returns>
    public Vector3 CellToWorld(Vector3Int cellPosition)
    {
        return tilemaps != null && tilemaps.Length > 0 ? tilemaps[0].CellToWorld(cellPosition) : Vector3.zero;
    }

    /// <summary>
    /// 특정 타일맵에서 그리드 셀 좌표(Vector3Int)를 월드 좌표(Vector3)로 변환합니다.
    /// </summary>
    /// <param name="cellPosition">그리드 좌표</param>
    /// <param name="targetTilemap">변환할 타일맵</param>
    /// <returns>해당 그리드의 월드 중심 좌표</returns>
    public Vector3 CellToWorld(Vector3Int cellPosition, Tilemap targetTilemap)
    {
        if (targetTilemap == null)
        {
            Debug.LogWarning("[Pathfinder] 타일맵이 null입니다. 기본 타일맵을 사용합니다.");
            return tilemaps != null && tilemaps.Length > 0 ? tilemaps[0].CellToWorld(cellPosition) : Vector3.zero;
        }
        return targetTilemap.CellToWorld(cellPosition);
    }

    /// <summary>
    /// 계산된 경로를 씬 뷰(Scene View)에 선으로 그려 시각화합니다. (디버깅용)
    /// </summary>
    /// <param name="path">그릴 경로 리스트</param>
    public void DrawPath(List<Vector3Int> path)
    {
        DrawPath(path, tilemaps != null && tilemaps.Length > 0 ? tilemaps[0] : null);
    }

    /// <summary>
    /// 특정 타일맵에서 계산된 경로를 씬 뷰(Scene View)에 선으로 그려 시각화합니다. (디버깅용)
    /// </summary>
    /// <param name="path">그릴 경로 리스트</param>
    /// <param name="targetTilemap">경로를 그릴 타일맵</param>
    public void DrawPath(List<Vector3Int> path, Tilemap targetTilemap)
    {
        if (path == null || path.Count == 0) return;
        if (targetTilemap == null)
        {
            Debug.LogWarning("[Pathfinder] 타일맵이 null입니다.");
            return;
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            // 경로의 각 지점을 월드 좌표로 변환하여 선을 그림
            Vector3 start = targetTilemap.GetCellCenterWorld(path[i]);
            Vector3 end = targetTilemap.GetCellCenterWorld(path[i + 1]);
            
            // 녹색 선을 2초간 표시
            Debug.DrawLine(start, end, Color.green, 2f);
        }
    }

    #endregion
}