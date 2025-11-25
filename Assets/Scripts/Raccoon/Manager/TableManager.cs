using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public enum waitingDirectionSelection
{
    Up,
    Down,
    Left,
    Right
}


public class TableManager : MonoBehaviour
{
    [Header("탁자 오브젝트들")]
    public List<GameObject> tables = new List<GameObject>();

    [Header("손님 프리팹")]
    [SerializeField] private GameObject CustomerPrefab;
    [Header("스폰할 손님 수")]
    [SerializeField] private int CustomerCount;
    [Header("스폰할 손님의 위치")]
    [SerializeField] private Transform CustomerTransform;
    [Header("대기할 손님의 위치")]
    public Transform CustomerWaitingTransform;

    [Range(1, 5)]
    public int waitingInterval = 2;

    [Header("대기열 방향 설정")]
    public waitingDirectionSelection waitingDirectionSelect = waitingDirectionSelection.Right;

    private Vector3Int waitingDirection;
    
    [Tooltip("대기열을 여러 줄로 만들지 여부")]
    public bool useMultipleLines = false;

    [Tooltip("한 줄당 최대 인원 (여러 줄 사용시)")]
    public int maxGuestsPerLine = 5;

    [Tooltip("대기 위치를 Scene 뷰에서 미리보기")]
    public bool showWaitingPositions = true;

    [Header("손님 이동 경로")]
    [SerializeField] private IsometricPathfinder CustomerPath;

    [HideInInspector]
    public Dictionary<GameObject, (int count, List<GameObject> customers)> reservedTables = new Dictionary<GameObject, (int, List<GameObject>)>();

    [HideInInspector]
    public List<GameObject> availableTables = new List<GameObject>();

    [HideInInspector]
    public Dictionary<GameObject, List<GameObject>> tableReservations = new Dictionary<GameObject, List<GameObject>>();

    [HideInInspector]
    public List<GameObject> waitingLine = new List<GameObject>();

    [HideInInspector]
    public GameObject[] tablesInCustomer;

    [Header("현재 대기 손님 수")]
    public int waitingCustomerCount = 0;
    [Header("최대 대기 손님 수")]
    public int maxWaitingCustomers = 5;

    [Header("손님 스폰 시간")]
    [SerializeField] private float spawnInterval = 10f;

    private float nextSpawnTime = 0f;
    private int PartySize; // 2인 파티용 변수

    [HideInInspector]
    public List<GameObject> waitingForPartner = new List<GameObject>();

    void Awake()
    {
        // 대기열 방향 벡터 설정
        switch (waitingDirectionSelect)
        {
            case waitingDirectionSelection.Up:
                waitingDirection = new Vector3Int(0, 1, 0);
                break;
            case waitingDirectionSelection.Down:
                waitingDirection = new Vector3Int(0, -1, 0);
                break;
            case waitingDirectionSelection.Left:
                waitingDirection = new Vector3Int(-1, 0, 0);
                break;
            case waitingDirectionSelection.Right:
                waitingDirection = new Vector3Int(1, 0, 0);
                break;
        }

        // 테이블 매니저 초기화
        availableTables.Clear();
        foreach (GameObject table in tables)
        {
            availableTables.Add(table);
            table.GetComponent<TableClass>().tableManager = this;
            if (table.GetComponent<TableClass>().pathfinder == null)
            {
                table.GetComponent<TableClass>().pathfinder = CustomerPath;
            }
        }
    }

    void Update()
    {
        // 테이블 상태 업데이트
        UpdateTableLists();

        List<GameObject> customerTables = new List<GameObject>();
        for (int i = 0; i < tables.Count; i++)
        {
            TableClass tableComp = tables[i].GetComponent<TableClass>();
            if (tableComp.isCustomerSeated)
            {
                if (!customerTables.Contains(tables[i]))
                {
                    customerTables.Add(tables[i]);
                }
            }
        }

        tablesInCustomer = customerTables.ToArray();

        /// <summary>
        /// 손님 스폰 로직
        /// </summary>
        if (CustomerCount > 0 && waitingCustomerCount < maxWaitingCustomers)
        {
            if (Time.time >= nextSpawnTime)
            {
                nextSpawnTime = Time.time + spawnInterval;
                int spawnCount = Mathf.Min(1, maxWaitingCustomers - waitingCustomerCount);

                for (int i = 0; i < spawnCount; i++)
                {
                    PartySize = Random.Range(1, 3); // 1인 또는 2인 파티 랜덤 결정
                    if(PartySize == 2 && CustomerCount < 2)
                    {
                        PartySize = 1; // 남은 손님 수가 1명일 때는 1인 파티로 조정
                    }

                    switch(PartySize)
                    {
                        case 1:
                            GameObject customer = Instantiate(CustomerPrefab, CustomerTransform.transform.position, Quaternion.identity);
                            GuestController guestController = customer.GetComponent<GuestController>();
                            guestController.tableManager = this;
                            guestController.pathfinder = CustomerPath;

                            guestController.desiredPartySize = 1;
                            CustomerCount -= 1;
                            break;
                        case 2:
                            for(int j = 0; j < 2; j++)
                            {
                                GameObject customer_party = Instantiate(CustomerPrefab, CustomerTransform.transform.position, Quaternion.identity);
                                GuestController guestController_party = customer_party.GetComponent<GuestController>();
                                guestController_party.tableManager = this;
                                guestController_party.pathfinder = CustomerPath;

                                guestController_party.desiredPartySize = 2;
                            }
                            CustomerCount -= 2;
                            break;
                    }
                }
            }
        }

        CleanupWaitingLine();
    }
    #region 테이블 상태 업데이트
    /// <summary>
    /// 테이블 상태 업데이트
    /// </summary>
    void UpdateTableLists()
    {
        reservedTables.Clear();
        availableTables.Clear();
        foreach (GameObject table in tables)
        {
            TableClass tableComp = table.GetComponent<TableClass>();

            if (tableComp.isCustomerSeated)
            {
                int seatedCount = tableComp.Seated_Customer.Count;
                reservedTables[table] = (seatedCount, new List<GameObject>(tableComp.Seated_Customer));
            }
            else
            {
                availableTables.Add(table);
            }
        }
    }
    #endregion
    #region 테이블(부분점유/빈) 찾기
    /// <summary>
    /// 부분 점유 테이블 가져오기 (그룹 크기 고려)
    /// </summary>
    public GameObject GetPartiallyOccupiedTable(int desiredSize = 0)
    {
        foreach (var kvp in reservedTables)
        {
            GameObject table = kvp.Key;
            int seatedCount = kvp.Value.count;
            TableClass tableComp = table.GetComponent<TableClass>();

            // 원하는 그룹 크기와 테이블 크기가 맞는지 확인
            if (desiredSize > 0 && tableComp.MAX_Capacity != desiredSize)
            {
                continue; // 크기가 맞지 않으면 스킵
            }

            int reservedCount = tableReservations.ContainsKey(table) ? tableReservations[table].Count : 0;
            int totalCount = seatedCount + reservedCount;

            if (seatedCount >= 1 && totalCount < tableComp.MAX_Capacity)
            {
                return table;
            }
        }
        return null;
    }

    /// <summary>
    /// 빈 테이블 가져오기 (그룹 크기 고려)
    /// </summary>
    public GameObject GetAvailableTable(int desiredSize = 0)
    {
        foreach (GameObject table in availableTables)
        {
            TableClass tableComp = table.GetComponent<TableClass>();

            // 원하는 그룹 크기와 테이블 크기가 맞는지 확인
            if (desiredSize > 0 && tableComp.MAX_Capacity != desiredSize)
            {
                continue; // 크기가 맞지 않으면 스킵
            }

            int reservedCount = tableReservations.ContainsKey(table) ? tableReservations[table].Count : 0;

            if (reservedCount < tableComp.MAX_Capacity)
            {
                return table;
            }
        }
        return null;
    }
    #endregion

    #region 테이블 예약 관리
    public void ReserveTable(GameObject table, GameObject guest) // 손님이 테이블 예약
    {
        if (table == null || guest == null)
        {
            return;
        }

        TableClass tableComp = table.GetComponent<TableClass>();

        if (tableComp == null)
        {
            return;
        }

        int seatedCount = tableComp.Seated_Customer.Count; // 현재 앉아있는 손님 수
        int reservedCount = tableReservations.ContainsKey(table) ? tableReservations[table].Count : 0; // 현재 예약된 손님 수
        int totalCount = seatedCount + reservedCount; // 총 인원 수

        if (totalCount >= tableComp.MAX_Capacity) // 테이블이 꽉 찼으면 예약 불가
        {
            return;
        }

        if (!tableReservations.ContainsKey(table)) // 테이블이 아직 예약 목록에 없으면 추가
        {
            tableReservations[table] = new List<GameObject>(); // 새 리스트 생성
        }

        if (!tableReservations[table].Contains(guest)) // 손님이 아직 예약하지 않았다면 추가
        {
            tableReservations[table].Add(guest); // 예약 추가
        }
    }

    public void CancelReservation(GameObject table, GameObject guest) // 손님이 테이블 예약 취소
    {
        if (table == null || guest == null)
        {
            return;
        }

        if (tableReservations.ContainsKey(table)) // 테이블이 예약 목록에 있으면
        {
            tableReservations[table].Remove(guest); // 예약 취소

            if (tableReservations[table].Count == 0) // 예약된 손님이 없으면 테이블 제거
            {
                tableReservations.Remove(table); // 테이블 제거
            }
        }
    }
    #endregion

    #region 대기열 관리
    public int AddToWaitingLine(GameObject guest) // 손님을 대기열에 추가하고 위치 반환
    {
        if (!waitingLine.Contains(guest)) // 손님이 아직 대기열에 없으면 추가
        {
            waitingLine.Add(guest); // 대기열에 추가
            waitingCustomerCount++; // 대기 손님 수 증가
            int position = waitingLine.Count - 1; // 대기열 - 1 를 반환시키게 함
            return position;
        }
        return waitingLine.IndexOf(guest); // 손님의 현재 대기 위치 반환
    }

    public void RemoveFromWaitingLine(GameObject guest) // 손님을 대기열에서 제거
    {
        int removedIndex = waitingLine.IndexOf(guest);
        if (removedIndex != -1)
        {
            waitingLine.RemoveAt(removedIndex);
            waitingCustomerCount--;
            UpdateWaitingLinePositions(removedIndex); // 이후 손님들의 위치 업데이트
        }
    }

    private void UpdateWaitingLinePositions(int startIndex) // 손님들의 대기열 위치 업데이트
    {
        for (int i = startIndex; i < waitingLine.Count; i++) // 이후 손님들 위치 갱신
        {
            if (waitingLine[i] != null)
            {
                GuestController guestController = waitingLine[i].GetComponent<GuestController>();
                if (guestController != null)
                {
                    guestController.UpdateWaitingPosition(i); // 손님의 대기 위치 갱신
                }
            }
        }
    }

    public Vector3 CalculateIsometricWaitingPosition(int position) // 대기열에서의 Isometric Cell 좌표계로 위치 계산
    {
        if (CustomerPath == null || CustomerWaitingTransform == null)
        {
            return Vector3.zero;
        }

        Vector3Int baseGridPos = CustomerPath.WorldToCell(CustomerWaitingTransform.position);
        Vector3Int targetGridPos;

        if (useMultipleLines && maxGuestsPerLine > 0) // 여러 줄 사용 시
        {
            int lineIndex = position / maxGuestsPerLine; // 몇 번째 줄인지
            int posInLine = position % maxGuestsPerLine; // 줄 내에서의 위치
            Vector3Int perpendicularDir = GetPerpendicularDirection(waitingDirection); // 대기열에 수직인 방향
            Vector3Int lineOffset = perpendicularDir * lineIndex; // 줄 오프셋
            Vector3Int positionOffset = waitingDirection * posInLine * waitingInterval; // 줄 내 위치 오프셋
            targetGridPos = baseGridPos + lineOffset + positionOffset; // 최종 타겟 그리드 위치
        }
        else
        {
            Vector3Int gridOffset = waitingDirection * position * waitingInterval; // 대기열 방향으로의 오프셋
            targetGridPos = baseGridPos + gridOffset; // 최종 타겟 그리드 위치
        }

        return CustomerPath.CellToWorld(targetGridPos); // 그리드 좌표를 월드 좌표로 변환하여 반환
    }
    #endregion 

    #region 정리
    // 대기열 정리 (null 참조 제거)
    public void CleanupWaitingLine()
    {
        bool needsCleanup = false;
        for (int i = 0; i < waitingLine.Count; i++)
        {
            if (waitingLine[i] == null)
            {
                needsCleanup = true;
                break;
            }
        }

        if (needsCleanup)
        {
            for (int i = waitingLine.Count - 1; i >= 0; i--)
            {
                if (waitingLine[i] == null)
                {
                    waitingLine.RemoveAt(i);
                    waitingCustomerCount--;
                }
            }

            UpdateWaitingLinePositions(0);
        }
    }
    #endregion

    #region 유틸리티용(체킹용) 메소드
   // 대기열 방향에 수직인 방향 벡터 가져오기 ( 손님 대기 시 사용 )
    private Vector3Int GetPerpendicularDirection(Vector3Int direction)
    {
        if (direction == Vector3Int.up || direction == Vector3Int.down)
        {
            return Vector3Int.right;
        }
        else if (direction == Vector3Int.left || direction == Vector3Int.right)
        {
            return Vector3Int.up;
        }
        else
        {
            return Vector3Int.right;
        }
    }

    // 대기열의 첫 번째 손님 가져오기
    public GameObject GetFirstWaitingGuest()
    {
        if (waitingLine.Count > 0 && waitingLine[0] != null)
        {
            return waitingLine[0];
        }
        return null;
    }

    // 특정 손님이 어디에 대기하고 있는지 가져오기
    public int GetWaitingPosition(GameObject guest)
    {
        return waitingLine.IndexOf(guest);
    }

    // 대기열이 가득 찼는지 확인하는 메소드
    public bool IsWaitingLineFull()
    {
        return waitingLine.Count >= maxWaitingCustomers;
    }
    #endregion

    #region 2인 파티용 메소드
    /// <summary>
    ///  2인 파티를 위한 파트너 찾기
    /// </summary>
    /// <returns>파트너로 삼음</returns>
    /// <param name="guest">파트너를 찾을 손님 게임 오브젝트</param>
    public GameObject FindPartnerForTwoPersonParty(GameObject guest)
    {
        // 대기 중인 손님들 중에서 파트너 찾기
        for (int i = 0; i < waitingForPartner.Count; i++)
        {
            // 자기 자신이 아니고, 2인 파티를 원하는 손님이며, 아직 파트너가 없는 경우
            if (waitingForPartner[i] != null && waitingForPartner[i] != guest)
            {
                GuestController partnerController = waitingForPartner[i].GetComponent<GuestController>();
                if (partnerController != null && partnerController.desiredPartySize == 2 && partnerController.groupPartner == null)
                {
                    // 대기 중 인원 중 한명을 파트너로 삼음
                    return waitingForPartner[i];
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 대기 중인 손님을 2인 파티 대기 리스트에 추가
    /// </summary>
    /// <param name="guest">추가할 손님 게임 오브젝트</param>
    public void AddToPartnerWaitingList(GameObject guest)
    {
        if (!waitingForPartner.Contains(guest))
        {
            waitingForPartner.Add(guest);
        }
    }

    /// <summary>
    /// 대기 중인 손님을 2인 파티 대기 리스트에서 제거
    /// </summary>
    /// <param name="guest">제거할 손님 게임 오브젝트</param>
    public void RemoveFromPartnerWaitingList(GameObject guest)
    {
        waitingForPartner.Remove(guest);
    }

    /// <summary>
    /// 2인 파티를 위한 테이블 예약 시도
    /// </summary>
    /// <param name="guest1">첫 번째 손님 게임 오브젝트</param>
    /// <param name="guest2">두 번째 손님 게임 오브젝트</param>
    /// <returns>예약 성공 여부</returns>
    public bool TryReserveTableForGroup(GameObject guest1, GameObject guest2)
    {
        GameObject table = GetAvailableTable(2);
        if (table == null)
        {
            return false;
        }

        TableClass tableComp = table.GetComponent<TableClass>();
        if (tableComp.MAX_Capacity < 2)
        {
            return false;
        }

        Transform seat1 = tableComp.GetAvailableSeatForGuest(guest1);
        if (seat1 == null)
        {
            return false;
        }

        Transform seat2 = tableComp.GetAvailableSeatForGuest(guest2);
        if (seat2 == null)
        {
            tableComp.ReleaseSeat(guest1);
            return false;
        }

        ReserveTable(table, guest1);
        ReserveTable(table, guest2);

        GuestController controller1 = guest1.GetComponent<GuestController>();
        GuestController controller2 = guest2.GetComponent<GuestController>();

        if (controller1 != null)
        {
            controller1.AssignTableAndSeat(table, seat1);
        }

        if (controller2 != null)
        {
            controller2.AssignTableAndSeat(table, seat2);
        }

        return true;
    }
    #endregion

    #region 디버깅 및 기즈모 출력
    [ContextMenu("대기열 상태 출력")]
    public void PrintWaitingLineStatus()
    {
        Debug.Log($"현재 아이소메트릭 대기열 상황: {waitingLine.Count}명 대기 중");

        for (int i = 0; i < waitingLine.Count; i++)
        {
            if (waitingLine[i] != null)
            {
                Vector3 pos = CalculateIsometricWaitingPosition(i);
                GuestController guest = waitingLine[i].GetComponent<GuestController>();
                int partySize = guest != null ? guest.desiredPartySize : 0;
                Debug.Log($"  {i}번째: {waitingLine[i].name} - 위치: {pos}, 원하는 크기: {partySize}인");
            }
        }
    }

    // 기즈모 - 경로 미리보기
    void OnDrawGizmosSelected()
    {
        if (!showWaitingPositions || CustomerWaitingTransform == null || CustomerPath == null)
        {
            return;
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(CustomerWaitingTransform.position, 0.5f);

        Gizmos.color = Color.yellow;
        for (int i = 0; i < maxWaitingCustomers; i++)
        {
            Vector3 waitingPos = CalculateIsometricWaitingPosition(i);
            Gizmos.DrawWireCube(waitingPos, Vector3.one * 0.8f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(waitingPos + Vector3.up * 0.5f, i.ToString());
#endif
        }

        if (CustomerWaitingTransform != null)
        {
            Gizmos.color = Color.green;
            Vector3 arrowStart = CustomerWaitingTransform.position;
            Vector3 arrowEnd = CalculateIsometricWaitingPosition(1);
            Vector3 direction = (arrowEnd - arrowStart).normalized;

            Gizmos.DrawLine(arrowStart, arrowStart + direction * 2f);
            Gizmos.DrawWireSphere(arrowStart + direction * 2f, 0.2f);
        }
    }
    #endregion
}