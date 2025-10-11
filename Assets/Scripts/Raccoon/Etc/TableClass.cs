using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TableClass : MonoBehaviour
{
    public TableManager tableManager;
    public bool isCustomerSeated = false;
    public List<GameObject> Seated_Customer = new List<GameObject>();
    public int MAX_Capacity = 2;

    [Header("좌석 설정")]
    public GameObject seatPrefab; // 좌석 프리팹
    public List<Transform> seats = new List<Transform>(); // 생성된 좌석 위치들
    public float seatDistance = 1f; // 테이블에서 좌석까지의 거리

    [Header("아이소메트릭 Pathfinder")]
    public IsometricPathfinder pathfinder; // 아이소메트릭 타일맵 참조

    // 각 좌석에 배정된 손님 추적
    private Dictionary<Transform, GameObject> seatAssignments = new Dictionary<Transform, GameObject>();

    private void Awake()
    {
        tableManager.tables.Add(this.gameObject);
        GenerateSeats(); // 좌석 생성
    }

    // 아이소메트릭 타일 기반 좌석 생성
    private void GenerateSeats()
    {
        if (MAX_Capacity <= 0) return;

        // 기존 좌석 제거
        foreach (Transform seat in seats)
        {
            if (seat != null)
                Destroy(seat.gameObject);
        }
        seats.Clear();
        seatAssignments.Clear();

        // pathfinder가 없으면 기본 좌표계 사용
        if (pathfinder == null)
        {
            Debug.LogWarning("IsometricPathfinder가 할당되지 않았습니다. 기본 좌표계를 사용합니다.");
            GenerateSeatsBasic();
            return;
        }

        // 테이블의 그리드 위치
        Vector3Int tableGridPos = pathfinder.WorldToCell(transform.position);

        if (MAX_Capacity == 1)
        {
            // 1인 테이블: 아래쪽 (0, -1, 0)
            Vector3Int seatGridPos = tableGridPos + new Vector3Int(0, -1, 0);
            CreateSeatAtGrid(seatGridPos);
        }
        else if (MAX_Capacity == 2)
        {
            // 2인 테이블: 좌우 양쪽 (ㅇ ㅁ ㅇ)
            Vector3Int leftGridPos = tableGridPos + new Vector3Int(-1, 0, 0);
            Vector3Int rightGridPos = tableGridPos + new Vector3Int(1, 0, 0);
            CreateSeatAtGrid(leftGridPos);
            CreateSeatAtGrid(rightGridPos);
        }
        else if (MAX_Capacity == 3)
        {
            // 3인 테이블: 좌, 우, 아래
            Vector3Int leftGridPos = tableGridPos + new Vector3Int(-1, 0, 0);
            Vector3Int rightGridPos = tableGridPos + new Vector3Int(1, 0, 0);
            Vector3Int bottomGridPos = tableGridPos + new Vector3Int(0, -1, 0);
            CreateSeatAtGrid(leftGridPos);
            CreateSeatAtGrid(rightGridPos);
            CreateSeatAtGrid(bottomGridPos);
        }
        else if (MAX_Capacity >= 4)
        {
            // 4인 이상 테이블: 사방
            Vector3Int leftGridPos = tableGridPos + new Vector3Int(-1, 0, 0);
            Vector3Int rightGridPos = tableGridPos + new Vector3Int(1, 0, 0);
            Vector3Int topGridPos = tableGridPos + new Vector3Int(0, 1, 0);
            Vector3Int bottomGridPos = tableGridPos + new Vector3Int(0, -1, 0);
            CreateSeatAtGrid(leftGridPos);
            CreateSeatAtGrid(rightGridPos);
            CreateSeatAtGrid(topGridPos);
            CreateSeatAtGrid(bottomGridPos);
        }
    }

    // 그리드 좌표에 좌석 생성 (아이소메트릭)
    private void CreateSeatAtGrid(Vector3Int gridPos)
    {
        // 그리드 좌표를 월드 좌표로 변환
        Vector3 worldPos = pathfinder.CellToWorld(gridPos);

        GameObject seatObj;

        if (seatPrefab != null)
        {
            seatObj = Instantiate(seatPrefab, worldPos, Quaternion.identity, transform);
        }
        else
        {
            seatObj = new GameObject($"Seat_{seats.Count}");
            seatObj.transform.position = worldPos;
            seatObj.transform.parent = transform;
        }

        seats.Add(seatObj.transform);
        seatAssignments[seatObj.transform] = null; // 초기에는 빈 좌석
    }

    // 기본 좌표계로 좌석 생성 (pathfinder 없을 때)
    private void GenerateSeatsBasic()
    {
        Vector3 tablePos = transform.position;

        if (MAX_Capacity == 1)
        {
            Vector3 seatPos = tablePos + new Vector3(0, -seatDistance, 0);
            CreateSeat(seatPos);
        }
        else if (MAX_Capacity == 2)
        {
            Vector3 leftSeat = tablePos + new Vector3(-seatDistance, 0, 0);
            Vector3 rightSeat = tablePos + new Vector3(seatDistance, 0, 0);
            CreateSeat(leftSeat);
            CreateSeat(rightSeat);
        }
        else if (MAX_Capacity == 3)
        {
            Vector3 leftSeat = tablePos + new Vector3(-seatDistance, 0, 0);
            Vector3 rightSeat = tablePos + new Vector3(seatDistance, 0, 0);
            Vector3 bottomSeat = tablePos + new Vector3(0, -seatDistance, 0);
            CreateSeat(leftSeat);
            CreateSeat(rightSeat);
            CreateSeat(bottomSeat);
        }
        else if (MAX_Capacity >= 4)
        {
            Vector3 leftSeat = tablePos + new Vector3(-seatDistance, 0, 0);
            Vector3 rightSeat = tablePos + new Vector3(seatDistance, 0, 0);
            Vector3 topSeat = tablePos + new Vector3(0, seatDistance, 0);
            Vector3 bottomSeat = tablePos + new Vector3(0, -seatDistance, 0);
            CreateSeat(leftSeat);
            CreateSeat(rightSeat);
            CreateSeat(topSeat);
            CreateSeat(bottomSeat);
        }
    }

    // 좌석 생성 헬퍼 메서드
    private void CreateSeat(Vector3 position)
    {
        GameObject seatObj;

        if (seatPrefab != null)
        {
            seatObj = Instantiate(seatPrefab, position, Quaternion.identity, transform);
        }
        else
        {
            seatObj = new GameObject($"Seat_{seats.Count}");
            seatObj.transform.position = position;
            seatObj.transform.parent = transform;
        }

        seats.Add(seatObj.transform);
        seatAssignments[seatObj.transform] = null; // 초기에는 빈 좌석
    }

    // 특정 손님에게 빈 좌석 배정 (겹치지 않게)
    public Transform GetAvailableSeatForGuest(GameObject guest)
    {
        // 이미 배정된 좌석이 있는지 확인
        foreach (var kvp in seatAssignments)
        {
            if (kvp.Value == guest)
            {
                return kvp.Key; // 이미 배정된 좌석 반환
            }
        }

        // 빈 좌석 찾기
        foreach (var kvp in seatAssignments)
        {
            if (kvp.Value == null) // 빈 좌석
            {
                seatAssignments[kvp.Key] = guest; // 손님 배정
                return kvp.Key; // 좌석 반환
            }
        }

        return null; // 빈 좌석 없음
    }

    // 손님이 좌석을 떠날 때 호출
    public void ReleaseSeat(GameObject guest)
    {
        foreach (var kvp in seatAssignments)
        {
            if (kvp.Value == guest)
            {
                seatAssignments[kvp.Key] = null; // 좌석 비우기
                break;
            }
        }
    }

    // 좌석이 특정 손님에게 배정되었는지 확인
    public bool IsSeatAssignedToGuest(Transform seat, GameObject guest)
    {
        if (seatAssignments.ContainsKey(seat))
        {
            return seatAssignments[seat] == guest;
        }
        return false;
    }

    // Scene 뷰에서 좌석 위치 시각화
    private void OnDrawGizmosSelected()
    {
        if (seats.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (Transform seat in seats)
        {
            if (seat != null)
            {
                Gizmos.DrawWireSphere(seat.position, 0.3f);
                Gizmos.DrawLine(transform.position, seat.position);

                // 배정 상태 표시
                if (seatAssignments.ContainsKey(seat) && seatAssignments[seat] != null)
                {
                    Gizmos.color = Color.red; // 배정된 좌석
                    Gizmos.DrawSphere(seat.position, 0.2f);
                    Gizmos.color = Color.cyan;
                }
            }
        }
    }
}