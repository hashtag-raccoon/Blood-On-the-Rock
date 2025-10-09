using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class TableManager : MonoBehaviour
{
    [Header("탁자 오브젝트들")]
    public List<GameObject> tables = new List<GameObject>(); // 전체 테이블 리스트

    [Header("손님 프리랩")]
    [SerializeField] private GameObject CustomerPrefab;
    [Header("스폰할 손님 수")]
    [SerializeField] private int CustomerCount;
    [Header("스폰할 손님의 위치")]
    [SerializeField] private Transform CustomerTransform;
    [Header("대기할 손님의 위치")]
    public Transform CustomerWaitingTransform;

    [Range(1, 5)]
    public int waitingInterval = 2; // 대기 간격
    
    [Header("아이소메트릭 대기열 설정")]
    [Tooltip("대기열 방향: (0,-1,0)=아래쪽, (0,1,0)=위쪽, (-1,0,0)=왼쪽, (1,0,0)=오른쪽")]
    public Vector3Int waitingDirection = new Vector3Int(1, 0, 0); // 대기열 방향

    [Tooltip("대기열을 여러 줄로 만들지 여부")]
    public bool useMultipleLines = false;

    [Tooltip("한 줄당 최대 인원 (여러 줄 사용시)")]
    public int maxGuestsPerLine = 5; // 줄 당 인원

    [Tooltip("대기 위치를 Scene 뷰에서 미리보기")]
    public bool showWaitingPositions = true;

    [Header("손님 이동 경로")]
    [SerializeField] private IsometricPathfinder CustomerPath;

    // 예약된 테이블 딕셔너리: 테이블( 키 ) -> ( 앉은 손님 수, 손님 오브젝트들 )
    [HideInInspector]
    public Dictionary<GameObject, (int count, List<GameObject> customers)> reservedTables = new Dictionary<GameObject, (int, List<GameObject>)>();

    // 예약되지 않은 테이블 리스트 ( 빈 테이블 )
    [HideInInspector]
    public List<GameObject> availableTables = new List<GameObject>();

    // 테이블 예약 리스트 ( 테이블 -> 예약한 손님 리스트 )
    [HideInInspector]
    public Dictionary<GameObject, List<GameObject>> tableReservations = new Dictionary<GameObject, List<GameObject>>();

    // 대기열 관리를 위한 변수
    [HideInInspector]
    public List<GameObject> waitingLine = new List<GameObject>();

    [HideInInspector]
    public GameObject[] tablesInCustomer; // 테이블에 착석 중인 손님

    [Header("현재 대기 손님 수")]
    public int waitingCustomerCount = 0;
    [Header("최대 대기 손님 수")]
    public int maxWaitingCustomers = 5;

    [Header("손님 스폰 시간")]
    [SerializeField] private float spawnInterval = 10f;

    private float nextSpawnTime = 0f;

    void Start()
    {
        // 초기화: 모든 테이블을 예약되지 않은 테이블로 설정
        availableTables.Clear();
        foreach (GameObject table in tables)
        {
            availableTables.Add(table); // 처음에는 모든 테이블이 예약되지 않은 상태
        }
    }

    void Update()
    {
        UpdateTableLists(); // 테이블 상태 업데이트

        // 손님이 앉아있는 탁자만 중복 없이 리스트에 저장
        List<GameObject> customerTables = new List<GameObject>();
        for (int i = 0; i < tables.Count; i++)
        {
            Table tableComp = tables[i].GetComponent<Table>();
            if (tableComp.isCustomerSeated) // 손님이 앉아있는 테이블이면
            {
                if (!customerTables.Contains(tables[i])) // 중복 체크
                {
                    customerTables.Add(tables[i]); // 리스트에 추가
                }
            }
        }

        tablesInCustomer = customerTables.ToArray(); // 배열로 변환

        // 손님 스폰 로직, 대기열이 가득 차지 않았고 남은 손님이 있을 때
        if (CustomerCount > 0 && waitingCustomerCount < maxWaitingCustomers)
        {
            if (Time.time >= nextSpawnTime)
            {
                // spawnInterval 초마다 손님 스폰
                nextSpawnTime = Time.time + spawnInterval;
                int spawnCount = Mathf.Min(1, maxWaitingCustomers - waitingCustomerCount); // 한 번에 스폰할 손님 수 (최대 1명)
                // 실제 스폰할 손님 수는 남은 손님 수와 대기열 여유 공간에 따라 결정
                for (int i = 0; i < spawnCount; i++)
                {
                    GameObject Customers = Instantiate(CustomerPrefab, CustomerTransform.transform.position, Quaternion.identity);
                    Customers.GetComponent<GuestController>().tableManager = this;
                    Customers.GetComponent<GuestController>().pathfinder = CustomerPath;
                    CustomerCount--;
                }
            }
        }

        // 대기열 정리 (null 객체 제거)
        CleanupWaitingLine();
    }

    // 테이블 상태 업데이트
    void UpdateTableLists()
    {
        reservedTables.Clear(); // 예약된 테이블 초기화
        availableTables.Clear(); // 예약되지 않은 테이블 초기화
        foreach (GameObject table in tables)
        {
            Table tableComp = table.GetComponent<Table>(); // Table 컴포넌트 가져오기

            if (tableComp.isCustomerSeated) // 앉은 손님이 있는 테이블
            {
                // 예약된 테이블에 추가
                int seatedCount = tableComp.Seated_Customer.Count;
                // 딕셔너리에 테이블( 키 )과 ( 앉은 손님 수, 손님 오브젝트 리스트 ) 값 추가
                reservedTables[table] = (seatedCount, new List<GameObject>(tableComp.Seated_Customer));
            }
            else
            {
                // 예약되지 않은 테이블에 추가
                availableTables.Add(table);
            }
        }
    }

    // 1번 조건: 예약된 테이블 중 앉은 손님 수가 1명 이상이고 자리가 남은 테이블
    public GameObject GetPartiallyOccupiedTable()
    {
        foreach (var kvp in reservedTables) // 예약된 테이블( 딕셔너리 ) 수만큼 반복
        {
            GameObject table = kvp.Key; // 딕셔너리의 키( 테이블 오브젝트 )
            int seatedCount = kvp.Value.count; // 딕셔너리의 값( 앉은 손님 수 )
            Table tableComp = table.GetComponent<Table>();

            // 예약된 손님 수 = tableReservations 딕셔너리에서 확인, 없으면 0 있으면 그 수
            int reservedCount = tableReservations.ContainsKey(table) ? tableReservations[table].Count : 0;
            // 현재 좌석 수 = 실제 앉은 손님 수 + 예약된 손님 수
            int totalCount = seatedCount + reservedCount;

            // 1명 이상 앉아있고, 총 인원(앉은+예약)이 테이블의 MAX_Capacity(최대 수용량) 미만
            if (seatedCount >= 1 && totalCount < tableComp.MAX_Capacity)
            {
                return table; // 조건에 맞는 테이블 반환
            }
        }
        return null; // 조건에 맞는 테이블이 없으면 null 반환
    }

    // 2번 조건: 예약되지 않은 테이블
    public GameObject GetAvailableTable()
    {
        // 예약되지 않은 테이블(빈 테이블) 리스트 수 만큼 반복
        foreach (GameObject table in availableTables)
        {
            Table tableComp = table.GetComponent<Table>();

            // 예약된 손님 수 확인
            // 예약된 수 = tableReservations 딕셔너리에서 확인, 없으면 0 있으면 그 수
            int reservedCount = tableReservations.ContainsKey(table) ? tableReservations[table].Count : 0;

            // 예약 수가 테이블의 MAX_Capacity 미만인 테이블만 반환
            if (reservedCount < tableComp.MAX_Capacity)
            {
                return table; // 조건에 맞는 테이블 반환
            }
        }
        return null; // 조건에 맞는 테이블이 없으면 null 반환
    }

    // 테이블 예약
    public void ReserveTable(GameObject table, GameObject guest)
    {
        if (table == null || guest == null) // 널 체크
        {
            return;
        }

        Table tableComp = table.GetComponent<Table>();

        if (tableComp == null)
        {
            return;
        }

        // 테이블에 현재 앉은 손님 수
        int seatedCount = tableComp.Seated_Customer.Count;

        // 예약된 손님 수 = tableReservations 딕셔너리에서 확인, 없으면 0 있으면 그 수
        int reservedCount = tableReservations.ContainsKey(table) ? tableReservations[table].Count : 0;

        // 총 인원 수 = 앉은 수 + 예약된 수
        int totalCount = seatedCount + reservedCount;

        // MAX_Capacity 초과 방지
        if (totalCount >= tableComp.MAX_Capacity) // 총 인원 수 >= 최대 수용량
        {
            Debug.LogWarning($"테이블 예약 실패: 최대 수용 인원 초과. 테이블: {table.name}, 앉은: {seatedCount}명, 예약: {reservedCount}명, 최대: {tableComp.MAX_Capacity}명");
            return;
        }

        if (!tableReservations.ContainsKey(table)) // 테이블이 딕셔너리에 없으면 딕셔너리에 새로 추가
        {
            tableReservations[table] = new List<GameObject>(); // 빈 리스트로 초기화
        }

        if (!tableReservations[table].Contains(guest)) // 이미 예약된 손님이 아니면 예약 추가
        {
            tableReservations[table].Add(guest); // 예약된 테이블에 손님 추가
            Debug.Log($"테이블 예약 완료. 테이블: {table.name}, 착석: {seatedCount}명, 예약: {tableReservations[table].Count}명, 최대: {tableComp.MAX_Capacity}명");
        }
    }

    // 테이블 예약 취소
    public void CancelReservation(GameObject table, GameObject guest) // 예약 취소 메서드
    {
        if (table == null || guest == null)
        {
            return;
        }

        if (tableReservations.ContainsKey(table)) // 테이블이 딕셔너리에 있으면
        {
            tableReservations[table].Remove(guest); // 해당 손님 예약 취소
            Debug.Log($"테이블 예약 취소. 테이블: {table.name}, 착석 예약: {tableReservations[table].Count}");

            if (tableReservations[table].Count == 0) // 예약된 손님이 없으면
            {
                tableReservations.Remove(table); // 딕셔너리에서 테이블 제거
            }
        }
    }

    // [아이소메트릭 대기열 관리 메서드들]
    // 대기열에 손님을 추가하는 메서드
    // <param name="guest">추가할 손님 GameObject</param>
    // <returns>대기열에서의 위치 (0부터 시작)</returns>
    public int AddToWaitingLine(GameObject guest) // 손님을 대기열에 추가
    {
        if (!waitingLine.Contains(guest)) // 이미 대기열에 없으면 추가
        {
            waitingLine.Add(guest); // 대기열에 손님 추가
            waitingCustomerCount++; // 대기 손님 수 증가
            int position = waitingLine.Count - 1; // 대기열에서의 위치 (0부터 시작)
            Debug.Log($"🚶 손님이 대기열에 추가됨. 위치: {position}, 총 대기 인원: {waitingCustomerCount}");
            return position; // 대기열에서의 위치 반환
        }
        return waitingLine.IndexOf(guest); // 이미 대기열에 있으면 현재 위치 반환
    }

    // 대기열에서 손님을 제거하는 메서드
    // <param name="guest">제거할 손님 GameObject</param>
    public void RemoveFromWaitingLine(GameObject guest) // 손님을 대기열에서 제거
    {
        int removedIndex = waitingLine.IndexOf(guest); // 제거할 손님의 인덱스 찾기
        if (removedIndex != -1) // 손님이 대기열에 있으면
        {
            waitingLine.RemoveAt(removedIndex); // 대기열에서 손님 제거
            waitingCustomerCount--; // 대기 손님 수 감소
            Debug.Log($"✅ 손님이 아이소메트릭 대기열에서 제거됨. 제거된 위치: {removedIndex}, 남은 대기 인원: {waitingCustomerCount}");

            // 뒤에 있던 손님들을 한 칸씩 앞으로 이동
            UpdateWaitingLinePositions(removedIndex);
        }
    }

    // 특정 인덱스 이후의 모든 손님들의 대기 위치를 업데이트
    // <param name="startIndex">업데이트를 시작할 인덱스</param>
    private void UpdateWaitingLinePositions(int startIndex) // 타일 대기열 위치 업데이트
    {
        for (int i = startIndex; i < waitingLine.Count; i++) // 시작 인덱스부터 끝까지 반복
        {
            if (waitingLine[i] != null) // null 체크
            {
                // 각 손님의 GuestController 스크립트에서 대기 위치 업데이트 메서드 호출
                GuestController guestController = waitingLine[i].GetComponent<GuestController>();
                if (guestController != null)
                {
                    // 각 손님의 대기 위치를 재계산하여 업데이트
                    guestController.UpdateWaitingPosition(i);
                    Debug.Log($"⬆️ 아이소메트릭 대기 위치 업데이트: {waitingLine[i].name} -> {i}번째 위치");
                }
            }
        }
    }

    // 아이소메트릭 그리드 기반 대기 위치 계산
    // <param name="position">대기열에서의 위치</param>
    public Vector3 CalculateIsometricWaitingPosition(int position) // 아이소메트릭 타일 계산
    {
        if (CustomerPath == null || CustomerWaitingTransform == null) // 필수 컴포넌트 체크
        {
            Debug.LogError("CustomerPath 또는 CustomerWaitingTransform이 없습니다!");
            return Vector3.zero; // 기본 위치 반환
        }

        // 기본 대기 위치 => 그리드 좌표
        Vector3Int baseGridPos = CustomerPath.WorldToCell(CustomerWaitingTransform.position);

        Vector3Int targetGridPos; // 최종 대기 위치 그리드 좌표

        if (useMultipleLines && maxGuestsPerLine > 0) // 여러 줄 대기열 사용 시
        {
            // 여러 줄 대기열
            int lineIndex = position / maxGuestsPerLine;     // 몇 번째 줄인지
            int posInLine = position % maxGuestsPerLine;     // 줄 안에서 몇 번째인지

            // 수직 방향으로 줄 확장
            Vector3Int perpendicularDir = GetPerpendicularDirection(waitingDirection);
            // 대기 위치 오프셋 계산
            Vector3Int lineOffset = perpendicularDir * lineIndex;
            // 방향 * n번째 줄 * 간격
            Vector3Int positionOffset = waitingDirection * posInLine * waitingInterval;
            // 최종 대기 위치 계산
            targetGridPos = baseGridPos + lineOffset + positionOffset;
        }
        else // 단일 줄 대기열
        {
            // 방향 * n번째 손님 * 간격
            Vector3Int gridOffset = waitingDirection * position * waitingInterval;
            targetGridPos = baseGridPos + gridOffset; // 최종 대기 위치 계산
        }

        // 그리드 좌표를 월드 좌표로 변환
        return CustomerPath.CellToWorld(targetGridPos);
    }

    // 주어진 방향에 수직인 방향을 반환
    // <param name="direction">기준 방향</param>
    private Vector3Int GetPerpendicularDirection(Vector3Int direction)
    {
        // 2D 아이소메트릭에서 수직 방향 계산
        if (direction == Vector3Int.up || direction == Vector3Int.down) // 위 또는 아래 방향이면
        {
            return Vector3Int.right; // 수직 방향은 오른쪽
        }
        else if (direction == Vector3Int.left || direction == Vector3Int.right) // 왼쪽 또는 오른쪽 방향이면
        {
            return Vector3Int.up; // 수직 방향은 위쪽
        }
        else
        {
            return Vector3Int.right; // 기본값
        }
    }

    // 대기열의 맨 앞 손님을 가져오는 메서드
    // <returns>맨 앞 손님 GameObject, 없으면 null</returns>
    public GameObject GetFirstWaitingGuest() // 대기열의 맨 앞 손님 반환
    {
        if (waitingLine.Count > 0 && waitingLine[0] != null) // 대기열에 손님이 있으면
        {
            return waitingLine[0]; // 맨 앞 손님 반환
        }
        return null; // 대기열이 비었거나 null이면 null 반환
    }

    // 대기열 정보를 출력하는 디버그 메서드
    [ContextMenu("아이소메트릭 대기열 상태 출력")]
    public void PrintWaitingLineStatus()
    {
        Debug.Log($"🎮 현재 아이소메트릭 대기열 상황: {waitingLine.Count}명 대기 중");
        Debug.Log($"📐 대기 방향: {waitingDirection}, 여러 줄: {useMultipleLines}, 줄당 최대: {maxGuestsPerLine}명");

        for (int i = 0; i < waitingLine.Count; i++)
        {
            if (waitingLine[i] != null)
            {
                Vector3 pos = CalculateIsometricWaitingPosition(i);
                Debug.Log($"  {i}번째: {waitingLine[i].name} - 위치: {pos}");
            }
            else
            {
                Debug.Log($"  {i}번째: null (정리 필요)");
            }
        }
    }

    // 대기열을 정리하는 메서드 (null 객체 제거)
    public void CleanupWaitingLine() // 대기열 정리 메서드
    {
        // null 객체가 있는지 확인
        bool needsCleanup = false;
        for (int i = 0; i < waitingLine.Count; i++) // 대기열 전체 검사
        {
            if (waitingLine[i] == null) // null 객체 발견 시
            {
                needsCleanup = true; // 정리 필요 플래그 설정
                break; // 더 이상 검사할 필요 없음
            }
        }

        if (needsCleanup) // null 객체가 있으면 정리 수행
        {
            // 뒤에서부터 제거하여 인덱스 변화 방지
            for (int i = waitingLine.Count - 1; i >= 0; i--) // 뒤에서부터 검사
            {
                if (waitingLine[i] == null) // null 객체 발견 시
                {
                    waitingLine.RemoveAt(i); // 대기열에서 제거
                    waitingCustomerCount--; // 대기 손님 수 감소
                    Debug.Log($"🧹 아이소메트릭 대기열에서 null 객체 제거됨. 인덱스: {i}");
                }
            }

            // 모든 위치 재조정
            UpdateWaitingLinePositions(0);
        }
    }

    // 특정 손님의 대기열 위치를 반환
    /// <param name="guest">확인할 손님 GameObject</param>
    public int GetWaitingPosition(GameObject guest) // 특정 손님의 대기 위치 반환
    {
        return waitingLine.IndexOf(guest); // 손님의 인덱스 반환, 없으면 -1
    }

    // 대기열이 가득 찼는지 확인
    public bool IsWaitingLineFull() 
    {
        return waitingLine.Count >= maxWaitingCustomers; // 대기열 수 >= 최대 대기 손님 수 = True or False 반환
    }

    // === Scene 뷰 시각화 ===
    // Scene 뷰에서 Gizmo로 대기 위치들을 시각화
    void OnDrawGizmosSelected()
    {
        if (!showWaitingPositions || CustomerWaitingTransform == null || CustomerPath == null)
        {
            return;
        }

        // 기본 대기 위치 표시
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(CustomerWaitingTransform.position, 0.5f);

        // 예상 대기 위치들 표시
        Gizmos.color = Color.yellow;
        for (int i = 0; i < maxWaitingCustomers; i++)
        {
            Vector3 waitingPos = CalculateIsometricWaitingPosition(i);
            Gizmos.DrawWireCube(waitingPos, Vector3.one * 0.8f);

            // 번호 표시 (에디터에서만)
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(waitingPos + Vector3.up * 0.5f, i.ToString());
#endif
        }

        // 대기 방향 화살표 표시
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