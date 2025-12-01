using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

// 머지 도중 주석이 날아가서 우선 Gemini 이용해서 주석을 달음
// 후에 내 방식으로 주석을 다시 처음부터 다는 작업을 할 예정이니 참고바람 !!


public enum seatingDirectionSelection
{
    Up,
    Down,
    Left,
    Right
}


/// <summary>
/// 테이블의 데이터와 좌석 관리 기능을 담당하는 클래스입니다.
/// 좌석 자동 생성 및 손님 배정 로직을 포함합니다.
/// </summary>
public class TableClass : MonoBehaviour
{
    #region Variables

    [Header("참조 및 상태")]
    public TableManager tableManager;
    public bool isCustomerSeated = false;
    public List<GameObject> Seated_Customer = new List<GameObject>();
    public int MAX_Capacity = 2;

    [Header("좌석 설정")]
    /// <summary>
    /// 생성할 좌석의 프리팹입니다.
    /// </summary>
    public GameObject seatPrefab;

    /// <summary>
    /// 현재 생성된 좌석들의 Transform 리스트입니다.
    /// </summary>
    public List<Transform> seats = new List<Transform>();

    /// <summary>
    /// 1인 테이블 시, 앉는 좌석의 방향을 설정할 수 있음
    /// </summary>
    public seatingDirectionSelection seatingDirection = seatingDirectionSelection.Down;

    /// <summary>
    /// Pathfinder가 없을 때 사용하는 기본 좌석 간 거리입니다.
    /// </summary>
    public float seatDistance = 1f;

    [Header("아이소메트릭 Pathfinder")]
    /// <summary>
    /// 타일맵 그리드 좌표 변환을 위한 아이소메트릭 경로 탐색기 참조입니다.
    /// </summary>
    public IsometricPathfinder pathfinder;

    /// <summary>
    /// 각 좌석(Transform)에 배정된 손님(GameObject)을 관리하는 딕셔너리입니다.
    /// </summary>
    private Dictionary<Transform, GameObject> seatAssignments = new Dictionary<Transform, GameObject>();

    #endregion

    #region Unity Methods

    private void Awake()
    {
        // 테이블 매니저 리스트에 자신을 등록
        tableManager.tables.Add(this.gameObject);

        // 초기 좌석 생성
        GenerateSeats();
    }

    /// <summary>
    /// 에디터 상에서 좌석 위치와 배정 상태를 시각화합니다.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (seats.Count == 0) return;

        Gizmos.color = Color.cyan;
        foreach (Transform seat in seats)
        {
            if (seat != null)
            {
                // 좌석 위치 표시
                Gizmos.DrawWireSphere(seat.position, 0.3f);
                Gizmos.DrawLine(transform.position, seat.position);

                // 배정된 상태 표시 (빨간색)
                if (seatAssignments.ContainsKey(seat) && seatAssignments[seat] != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(seat.position, 0.2f);
                    Gizmos.color = Color.cyan; // 색상 초기화
                }
            }
        }
    }

    #endregion

    #region Seat Generation Logic

    /// <summary>
    /// Pathfinder 유무에 따라 적절한 방식으로 좌석을 생성합니다.
    /// </summary>
    private void GenerateSeats()
    {
        if (MAX_Capacity <= 0) return;

        // 기존 좌석 제거 및 초기화
        foreach (Transform seat in seats)
        {
            if (seat != null)
                Destroy(seat.gameObject);
        }
        seats.Clear();
        seatAssignments.Clear();

        // Pathfinder가 없으면 기본 좌표 계산 방식으로 전환
        if (pathfinder == null)
        {
            Debug.LogWarning("IsometricPathfinder가 할당되지 않았습니다. 기본 좌표계를 사용합니다.");
            GenerateSeatsBasic();
            return;
        }

        // 아이소메트릭 그리드 기반 배치
        Vector3Int tableGridPos = pathfinder.WorldToCell(transform.position);

        if (MAX_Capacity == 1)
        {
            switch (seatingDirection)
            {
                case seatingDirectionSelection.Up:
                    tableGridPos += new Vector3Int(0, 1, 0);
                    break;
                case seatingDirectionSelection.Down:
                    tableGridPos += new Vector3Int(0, -1, 0);
                    break;
                case seatingDirectionSelection.Left:
                    tableGridPos += new Vector3Int(-1, 0, 0);
                    break;
                case seatingDirectionSelection.Right:
                    tableGridPos += new Vector3Int(1, 0, 0);
                    break;
            }

            // 1인 테이블: seatingDirection 에 따라 달라짐
            Vector3Int seatGridPos = tableGridPos;
            CreateSeatAtGrid(seatGridPos);
        }
        else if (MAX_Capacity == 2)
        {
            // 2인 테이블: 좌우 (서로 마주봄)
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
            // 4인 이상 테이블: 상하좌우 모두 배치
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

    /// <summary>
    /// 그리드 좌표를 기반으로 좌석을 생성합니다 (Pathfinder 사용 시).
    /// </summary>
    /// <param name="gridPos">생성할 그리드 좌표</param>
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

    /// <summary>
    /// Pathfinder 없이 기본 월드 좌표 거리 계산으로 좌석을 생성합니다.
    /// </summary>
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

    /// <summary>
    /// 월드 좌표에 좌석 오브젝트를 생성하는 내부 헬퍼 메서드입니다.
    /// </summary>
    /// <param name="position">생성할 월드 좌표</param>
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

    #endregion

    #region Guest Management Logic

    /// <summary>
    /// 특정 손님에게 배정 가능한 빈 좌석을 찾아 반환합니다.
    /// 이미 배정된 경우 해당 좌석을 반환하고, 없으면 빈 좌석을 새로 할당합니다.
    /// </summary>
    /// <param name="guest">좌석을 찾는 손님 오브젝트</param>
    /// <returns>배정된 좌석의 Transform (실패 시 null)</returns>
    public Transform GetAvailableSeatForGuest(GameObject guest)
    {
        // 1. 이미 이 손님에게 배정된 좌석이 있는지 확인
        foreach (var kvp in seatAssignments)
        {
            if (kvp.Value == guest)
            {
                return kvp.Key; // 이미 배정된 좌석 반환
            }
        }

        // 2. 빈 좌석 찾기 및 배정
        foreach (var kvp in seatAssignments)
        {
            if (kvp.Value == null) // 빈 좌석 발견
            {
                seatAssignments[kvp.Key] = guest; // 손님 배정
                return kvp.Key; // 좌석 반환
            }
        }

        return null; // 빈 좌석 없음
    }

    /// <summary>
    /// 손님이 떠날 때 좌석 배정을 해제합니다.
    /// </summary>
    /// <param name="guest">떠나는 손님 오브젝트</param>
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

    /// <summary>
    /// 특정 좌석이 해당 손님에게 배정되었는지 확인합니다.
    /// </summary>
    /// <param name="seat">확인할 좌석 Transform</param>
    /// <param name="guest">확인할 손님 GameObject</param>
    /// <returns>배정 여부</returns>
    public bool IsSeatAssignedToGuest(Transform seat, GameObject guest)
    {
        if (seatAssignments.ContainsKey(seat))
        {
            return seatAssignments[seat] == guest;
        }
        return false;
    }

    #endregion
}