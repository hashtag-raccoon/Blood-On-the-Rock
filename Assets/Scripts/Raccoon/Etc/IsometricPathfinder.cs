using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class IsometricPathfinder : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap; // 타일맵 오브젝트(인스펙터에서 받아야함)
    [SerializeField] private TileBase walkableTile; // 이동 가능한 타일 (null이면 모든 타일이 이동 가능), 타일베이스를 상속받은 모든 타일 가능

    private Dictionary<Vector3Int, Node> nodes = new Dictionary<Vector3Int, Node>(); // 탐색된 노드를 저장할 딕셔너리

    private class Node // A* 알고리즘에서 사용할 노드 클래스
    {
        public Vector3Int position;
        public Node parent;
        public float gCost; // 시작점으로부터의 거리
        public float hCost; // 목표점까지의 추정 거리
        public float fCost => gCost + hCost;

        public Node(Vector3Int pos)
        {
            position = pos;
        }
    }

    // A* 알고리즘을 사용한 경로 찾기
    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target) // 시작 노드부터 목표 노드까지의 경로 반환
    {
        
        // 타일이 유효한지 확인
        if (!IsWalkable(start))
        {
            Debug.LogWarning("시작점이 이동 불가능한 타일입니다. 현재 시작 위치는 [" + start + "] 입니다");
            return null;
        }
        if (!IsWalkable(target))
        {
            Debug.LogWarning("목표점이 이동 불가능한 타일입니다. 현재 목표 위치는 [" + target +"] 입니다");
            return null;
        }

        nodes.Clear(); // 이전 탐색 결과 초기화
        List<Node> openSet = new List<Node>(); // 탐색할 노드 리스트
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>(); // 탐색 완료된 노드 집합

        Node startNode = new Node(start); // 시작 노드 초기화
        startNode.gCost = 0;// 시작 노드의 gCost는 0
        startNode.hCost = GetDistance(start, target); // 시작 노드의 hCost 계산
        openSet.Add(startNode); // 시작 노드를 오픈 셋에 추가
        nodes[start] = startNode; // 딕셔너리에 시작 노드 추가

        while (openSet.Count > 0) // 오픈 셋에 노드가 남아있는 동안 반복
        {
            // fCost가 가장 낮은 노드 찾기
            Node currentNode = openSet[0]; // 임시로 첫 번째 노드로 설정
            for (int i = 1; i < openSet.Count; i++) // 나머지 노드들과 비교
            {
                if (openSet[i].fCost < currentNode.fCost || // fCost가 더 낮거나
                    (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)) // fCost가 같으면 hCost가 더 낮은 노드 선택
                {
                    currentNode = openSet[i]; // 현재 노드 갱신
                }
            }

            openSet.Remove(currentNode); // 현재 노드를 오픈 셋에서 제거
            closedSet.Add(currentNode.position); // 현재 노드를 클로즈드 셋에 추가

            // 목표 도달
            if (currentNode.position == target) // 목표 노드에 도달했으면
            {
                return RetracePath(startNode, currentNode); // 경로 역추적하여 반환
            }

            // 이웃 노드 탐색
            foreach (Vector3Int neighbor in GetNeighbors(currentNode.position)) // 4방향 이웃 노드 가져오기
            {
                if (!IsWalkable(neighbor) || closedSet.Contains(neighbor)) // 이동 불가능하거나 이미 탐색된 노드면 무시
                    continue; // 다음 이웃 노드로

                float newGCost = currentNode.gCost + GetDistance(currentNode.position, neighbor); // 현재 노드에서 이웃 노드까지의 거리 계산

                if (!nodes.ContainsKey(neighbor)) // 이웃 노드가 딕셔너리에 없으면 새로 생성
                {
                    nodes[neighbor] = new Node(neighbor); // 새 노드 생성 및 딕셔너리에 추가
                }

                Node neighborNode = nodes[neighbor]; // 이웃 노드 가져오기

                if (newGCost < neighborNode.gCost || !openSet.Contains(neighborNode)) // 더 짧은 경로를 찾았거나 오픈 셋에 없으면
                {
                    neighborNode.gCost = newGCost; // gCost 갱신
                    neighborNode.hCost = GetDistance(neighbor, target); // hCost 갱신
                    neighborNode.parent = currentNode; // 부모 노드 갱신

                    if (!openSet.Contains(neighborNode)) // 오픈 셋에 없으면 추가
                    {
                        openSet.Add(neighborNode); // 오픈 셋에 추가
                    }
                }
            }
        }

        // 경로를 찾지 못함
        return null;
    }

    // Isometric 타일의 4방향 이웃 가져오기
    private List<Vector3Int> GetNeighbors(Vector3Int pos) // 4방향 이웃 노드 반환
    {
        List<Vector3Int> neighbors = new List<Vector3Int> // 4방향 이웃 노드 리스트
        {
            pos + new Vector3Int(1, 0, 0),   // 우측
            pos + new Vector3Int(-1, 0, 0),  // 좌측
            pos + new Vector3Int(0, 1, 0),   // 위쪽
            pos + new Vector3Int(0, -1, 0)   // 아래쪽
        };

        return neighbors; // 이웃 노드 리스트 반환
    }

    /*
    // Isometric 타일의 8방향 이웃 가져오기 (대각선 포함)
    private List<Vector3Int> GetNeighbors8(Vector3Int pos)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>
        {
            pos + new Vector3Int(1, 0, 0),   // 우측
            pos + new Vector3Int(-1, 0, 0),  // 좌측
            pos + new Vector3Int(0, 1, 0),   // 위쪽
            pos + new Vector3Int(0, -1, 0),  // 아래쪽
            pos + new Vector3Int(1, 1, 0),   // 우상단
            pos + new Vector3Int(1, -1, 0),  // 우하단
            pos + new Vector3Int(-1, 1, 0),  // 좌상단
            pos + new Vector3Int(-1, -1, 0)  // 좌하단
        };

        return neighbors;
    }
    */

    // 두 타일 간의 거리 계산 (맨해튼 거리)
    private float GetDistance(Vector3Int a, Vector3Int b) // 두 타일 간의 거리 계산
    {
        int dx = Mathf.Abs(a.x - b.x); // x 좌표 차이
        int dy = Mathf.Abs(a.y - b.y); // y 좌표 차이

        // 대각선 이동을 허용하는 경우 체비셰프 거리 사용
        // return Mathf.Max(dx, dy);

        // 4방향만 허용하는 경우 맨해튼 거리 사용
        return dx + dy; // 맨해튼 거리 반환
    }

    // 타일이 이동 가능한지 확인
    private bool IsWalkable(Vector3Int pos) // 타일이 이동 가능한지 확인
    {
        TileBase tile = tilemap.GetTile(pos); // 해당 위치의 타일 가져오기

        // null이 아니고 walkableTile과 같으면 이동 가능
        // walkableTile이 설정되지 않았다면 null이 아닌 모든 타일이 이동 가능
        if (walkableTile != null) // 특정 타일만 이동 가능하도록 설정된 경우
        {
            return tile == walkableTile; // 해당 타일이 이동 가능한 타일인지 확인
        }

        return tile != null; // null이 아닌 모든 타일이 이동 가능
    }

    // 경로 역추적
    private List<Vector3Int> RetracePath(Node startNode, Node endNode) // 경로 역추적하여 리스트로 반환
    {
        List<Vector3Int> path = new List<Vector3Int>(); // 경로 리스트
        Node currentNode = endNode; // 현재 노드를 목표 노드로 설정

        while (currentNode != startNode) // 시작 노드에 도달할 때까지 반복
        {
            path.Add(currentNode.position); // 현재 노드 위치를 경로에 추가
            currentNode = currentNode.parent; // 부모 노드로 이동
        }

        path.Reverse(); // 경로를 시작점에서 목표점 순서로 뒤집기
        return path; // 경로 반환
    }

    // 월드 좌표를 타일맵 셀 좌표로 변환
    public Vector3Int WorldToCell(Vector3 worldPosition) // 월드 좌표를 셀 좌표로 변환
    {
        return tilemap.WorldToCell(worldPosition); // 타일맵의 월드ToCell 메서드 사용
    }

    // 타일맵 셀 좌표를 월드 좌표로 변환
    public Vector3 CellToWorld(Vector3Int cellPosition) // 셀 좌표를 월드 좌표로 변환
    {
        return tilemap.CellToWorld(cellPosition); // 타일맵의 CellToWorld 메서드 사용
    }

    // 경로 시각화 (디버그용)
    public void DrawPath(List<Vector3Int> path) // 경로를 디버그 라인으로 시각화
    {
        if (path == null || path.Count == 0) return;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 start = tilemap.GetCellCenterWorld(path[i]);
            Vector3 end = tilemap.GetCellCenterWorld(path[i + 1]);
            Debug.DrawLine(start, end, Color.green, 2f); // 2초 동안 초록색 라인으로 표시
        }
    }
}
