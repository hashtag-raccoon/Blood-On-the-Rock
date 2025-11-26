using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

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

    [Header("아이소메트릭 대기열 설정")]
    [Tooltip("대기열 방향: (0,-1,0)=아래쪽, (0,1,0)=위쪽, (-1,0,0)=왼쪽, (1,0,0)=오른쪽")]
    public Vector3Int waitingDirection = new Vector3Int(1, 0, 0);

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

    void Awake()
    {
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

        // 손님 스폰 로직
        if (CustomerCount > 0 && waitingCustomerCount < maxWaitingCustomers)
        {
            if (Time.time >= nextSpawnTime)
            {
                nextSpawnTime = Time.time + spawnInterval;
                int spawnCount = Mathf.Min(1, maxWaitingCustomers - waitingCustomerCount);

                for (int i = 0; i < spawnCount; i++)
                {
                    GameObject customer = Instantiate(CustomerPrefab, CustomerTransform.transform.position, Quaternion.identity);
                    GuestController guestController = customer.GetComponent<GuestController>();
                    guestController.tableManager = this;
                    guestController.pathfinder = CustomerPath;

                    // desiredPartySize는 프리팹에서 설정됨 (1인 또는 2인)

                    CustomerCount--;
                }
            }
        }

        CleanupWaitingLine();
    }

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

    // 부분 점유 테이블 가져오기 (그룹 크기 고려)
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

    // 빈 테이블 가져오기 (그룹 크기 고려)
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

    public void ReserveTable(GameObject table, GameObject guest)
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

        int seatedCount = tableComp.Seated_Customer.Count;
        int reservedCount = tableReservations.ContainsKey(table) ? tableReservations[table].Count : 0;
        int totalCount = seatedCount + reservedCount;

        if (totalCount >= tableComp.MAX_Capacity)
        {
            return;
        }

        if (!tableReservations.ContainsKey(table))
        {
            tableReservations[table] = new List<GameObject>();
        }

        if (!tableReservations[table].Contains(guest))
        {
            tableReservations[table].Add(guest);
        }
    }

    public void CancelReservation(GameObject table, GameObject guest)
    {
        if (table == null || guest == null)
        {
            return;
        }

        if (tableReservations.ContainsKey(table))
        {
            tableReservations[table].Remove(guest);

            if (tableReservations[table].Count == 0)
            {
                tableReservations.Remove(table);
            }
        }
    }

    public int AddToWaitingLine(GameObject guest)
    {
        if (!waitingLine.Contains(guest))
        {
            waitingLine.Add(guest);
            waitingCustomerCount++;
            int position = waitingLine.Count - 1;
            return position;
        }
        return waitingLine.IndexOf(guest);
    }

    public void RemoveFromWaitingLine(GameObject guest)
    {
        int removedIndex = waitingLine.IndexOf(guest);
        if (removedIndex != -1)
        {
            waitingLine.RemoveAt(removedIndex);
            waitingCustomerCount--;
            UpdateWaitingLinePositions(removedIndex);
        }
    }

    private void UpdateWaitingLinePositions(int startIndex)
    {
        for (int i = startIndex; i < waitingLine.Count; i++)
        {
            if (waitingLine[i] != null)
            {
                GuestController guestController = waitingLine[i].GetComponent<GuestController>();
                if (guestController != null)
                {
                    guestController.UpdateWaitingPosition(i);
                }
            }
        }
    }

    public Vector3 CalculateIsometricWaitingPosition(int position)
    {
        if (CustomerPath == null || CustomerWaitingTransform == null)
        {
            return Vector3.zero;
        }

        Vector3Int baseGridPos = CustomerPath.WorldToCell(CustomerWaitingTransform.position);
        Vector3Int targetGridPos;

        if (useMultipleLines && maxGuestsPerLine > 0)
        {
            int lineIndex = position / maxGuestsPerLine;
            int posInLine = position % maxGuestsPerLine;
            Vector3Int perpendicularDir = GetPerpendicularDirection(waitingDirection);
            Vector3Int lineOffset = perpendicularDir * lineIndex;
            Vector3Int positionOffset = waitingDirection * posInLine * waitingInterval;
            targetGridPos = baseGridPos + lineOffset + positionOffset;
        }
        else
        {
            Vector3Int gridOffset = waitingDirection * position * waitingInterval;
            targetGridPos = baseGridPos + gridOffset;
        }

        return CustomerPath.CellToWorld(targetGridPos);
    }

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

    public GameObject GetFirstWaitingGuest()
    {
        if (waitingLine.Count > 0 && waitingLine[0] != null)
        {
            return waitingLine[0];
        }
        return null;
    }

    [ContextMenu("아이소메트릭 대기열 상태 출력")]
    public void PrintWaitingLineStatus()
    {
        Debug.Log($"🎮 현재 아이소메트릭 대기열 상황: {waitingLine.Count}명 대기 중");

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

    public int GetWaitingPosition(GameObject guest)
    {
        return waitingLine.IndexOf(guest);
    }

    public bool IsWaitingLineFull()
    {
        return waitingLine.Count >= maxWaitingCustomers;
    }

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
}