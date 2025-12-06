using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

// race_id = 종족ID
// 임시로 0 = 인간, 1 = 오크, 2 = 뱀파이어

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
    // 예약 테이블

    [HideInInspector]
    public Dictionary<GameObject, (int count, List<GameObject> customers)> reservedTables = new Dictionary<GameObject, (int, List<GameObject>)>();

    // 빈 테이블
    [HideInInspector]
    public List<GameObject> availableTables = new List<GameObject>();

    // 테이블 예약 정보
    [HideInInspector]
    public Dictionary<GameObject, List<GameObject>> tableReservations = new Dictionary<GameObject, List<GameObject>>();

    // 손님 대기열
    [HideInInspector]
    public List<GameObject> waitingLine = new List<GameObject>();

    // 현재 손님이 앉아있는 테이블 배열
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

    private List<CustomerData> Customers = new List<CustomerData>();

    // TODO : 종족별 접두사 이름 리스트 (임시 하드코딩, 후에 다른 데이터 스크립트로 뺄 예정)
    private readonly List<string> humanNames = new List<string> { "교섭관", "농부", "기사단장", "계약중개인", "무역감시관", "일반" };
    private readonly List<string> oakNames = new List<string> { "전투 우두머리", "고기 사냥꾼", "혈투 전사", "부족 수호자", "전투 요리사", "일반" };
    private readonly List<string> vampireNames = new List<string> { "혈맹 장군", "순혈 집행관", "가문 감시자", "전통 심판자", "고문헌 수호자", "일반" };

    [Header("종족별 비주얼 데이터")]
    [SerializeField] private RaceVisualData humanVisualData;
    [SerializeField] private RaceVisualData oakVisualData;
    [SerializeField] private RaceVisualData vampireVisualData;

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

    #region 손님 스폰(Update에서 처리)
    void Update()
    {
        // 테이블 상태 업데이트
        UpdateTableLists();

        // 현재 손님이 앉아있는 테이블 목록 업데이트
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

        tablesInCustomer = customerTables.ToArray(); // 현재 손님이 앉아있는 테이블 배열 업데이트

        /// <summary>
        /// 손님 스폰 로직
        /// </summary>
        if (CustomerCount > 0 && waitingCustomerCount < maxWaitingCustomers) // 손님이 남아있고 대기열이 가득 차지 않았을 때
        {
            if (Time.time >= nextSpawnTime) // 스폰 시간 도달 시
            {
                nextSpawnTime = Time.time + spawnInterval;
                int spawnCount = Mathf.Min(1, maxWaitingCustomers - waitingCustomerCount); // 한 번에 스폰할 손님 수(1 or 2) 결정

                for (int i = 0; i < spawnCount; i++) // 1명씩 or 2명씩 스폰함
                {
                    PartySize = Random.Range(1, 3); // 1인 또는 2인 파티 랜덤 결정
                    if (PartySize == 2 && CustomerCount < 2)
                    {
                        PartySize = 1; // 남은 손님 수가 1명일 때는 1인 파티로 조정
                    }

                    switch (PartySize)
                    {
                        /// <summary>
                        /// 1인 파티 소환 로직
                        /// 개개인별로 손님 데이터, 초상화, 파티사이즈(1)로 전달
                        /// </summary>
                        case 1:
                            int RaceID = Random.Range(0, 3);
                            string GuestName = null;
                            string RaceName = "Unknown";
                            string prefix = null;
                            RaceVisualData visualData = null;

                            switch (RaceID)
                            {
                                case 0:
                                    RaceName = "Human";
                                    prefix = humanNames[Random.Range(0, humanNames.Count)];
                                    GuestName = prefix + " " + IntelligentNameGenerator.Generate(RaceName);
                                    visualData = humanVisualData;
                                    break;
                                case 1:
                                    RaceName = "Oak";
                                    prefix = oakNames[Random.Range(0, oakNames.Count)];
                                    GuestName = prefix + " " + IntelligentNameGenerator.Generate(RaceName);
                                    visualData = oakVisualData;
                                    break;
                                case 2:
                                    RaceName = "Vampire";
                                    prefix = vampireNames[Random.Range(0, vampireNames.Count)];
                                    GuestName = prefix + " " + IntelligentNameGenerator.Generate(RaceName);
                                    visualData = vampireVisualData;
                                    break;
                            }

                            // visualData null 체크
                            if (visualData == null)
                            {
                                Debug.LogError($"[TableManager] {RaceName}의 RaceVisualData가 Inspector에 할당되지 않았습니다! (RaceID: {RaceID})");
                                break;
                            }

                            // 종족 및 접두사에 맞는 세트(집합)를(을) 먼저 선택
                            // 선택 후 해당 세트 중에 랜덤으로 프리팹과 초상화를 가져옴
                            PrefixVisualSet visualSet = visualData?.GetRandomVisualSetByPrefix(prefix);
                            if (visualSet == null || visualSet.customerPrefab == null)
                            {
                                Debug.LogWarning($"[TableManager] '{prefix}' 접두사의 손님 비주얼 세트를 찾을 수 없습니다.");
                                Debug.LogWarning($"[TableManager] 디버그 정보:");
                                Debug.LogWarning($"  - 종족: {RaceName} (RaceID: {RaceID})");
                                Debug.LogWarning($"  - 접두사: '{prefix}'");
                                Debug.LogWarning($"  - visualData null 여부: {visualData == null}");
                                if (visualData != null)
                                {
                                    Debug.LogWarning($"  - visualData 종족 이름: {visualData.raceName}");
                                    Debug.LogWarning($"  - visualData raceId: {visualData.raceId}");
                                    Debug.LogWarning($"  - visualSet null 여부: {visualSet == null}");
                                    if (visualSet != null)
                                    {
                                        Debug.LogWarning($"  - customerPrefab null 여부: {visualSet.customerPrefab == null}");
                                    }
                                }
                                break;
                            }

                            GameObject customerPrefab = visualSet.customerPrefab;
                            Sprite portraitSprite = visualSet.portraitSprite;

                            // 프리팹 => 씬에서 쓰이는 실 객체로 인스턴스화
                            GameObject customer = Instantiate(customerPrefab, CustomerTransform.transform.position, Quaternion.identity);
                            customer.name = GuestName + Time.frameCount;

                            // GuestController 자동 할당 및 초기화
                            GuestController guestController = customer.GetComponent<GuestController>();
                            if (guestController == null)
                            {
                                guestController = customer.AddComponent<GuestController>();
                            }

                            // CustomerData 생성 및 할당
                            guestController.customerData = new CustomerData(CustomerCount, GuestName, RaceID, null, false,
                            null, 0, 0, 1, "Normal", 5, customerPrefab.name, portraitSprite);
                            guestController.desiredPartySize = 1;

                            // 할당되지 않은 필드 자동 할당
                            if (guestController.tableManager == null)
                            {
                                guestController.tableManager = this;
                            }
                            if (guestController.pathfinder == null)
                            {
                                guestController.pathfinder = CustomerPath;
                            }

                            CustomerCount -= 1;
                            break;
                        /// <summary>
                        /// 2인 파티 소환 로직
                        /// 각각의 손님에게 개개인별로 손님 데이터, 초상화, 파티사이즈(2)로 전달
                        /// 파티 내에서 파트너 설정은 GuestController에서 처리
                        /// </summary>
                        case 2:
                            int RaceID_party = Random.Range(0, 3);
                            for (int j = 0; j < 2; j++)
                            {
                                string RaceName_party = "Unknown";
                                string prefix_party = null;
                                RaceVisualData visualData_party = null;
                                GuestName = null;

                                switch (RaceID_party)
                                {
                                    case 0:
                                        RaceName_party = "Human";
                                        prefix_party = humanNames[Random.Range(0, humanNames.Count)];
                                        GuestName = prefix_party + " " + IntelligentNameGenerator.Generate(RaceName_party);
                                        visualData_party = humanVisualData;
                                        break;
                                    case 1:
                                        RaceName_party = "Oak";
                                        prefix_party = oakNames[Random.Range(0, oakNames.Count)];
                                        GuestName = prefix_party + " " + IntelligentNameGenerator.Generate(RaceName_party);
                                        visualData_party = oakVisualData;
                                        break;
                                    case 2:
                                        RaceName_party = "Vampire";
                                        prefix_party = vampireNames[Random.Range(0, vampireNames.Count)];
                                        GuestName = prefix_party + " " + IntelligentNameGenerator.Generate(RaceName_party);
                                        visualData_party = vampireVisualData;
                                        break;
                                }

                                // visualData_party null 체크
                                if (visualData_party == null)
                                {
                                    Debug.LogError($"[TableManager] {RaceName_party}의 RaceVisualData가 Inspector에 할당되지 않았습니다! (RaceID: {RaceID_party})");
                                    continue;
                                }

                                // 종족 및 접두사에 맞는 세트(집합)를(을) 먼저 선택
                                // 선택 후 해당 세트 중에 랜덤으로 프리팹과 초상화를 가져옴
                                PrefixVisualSet visualSet_party = visualData_party?.GetRandomVisualSetByPrefix(prefix_party);
                                if (visualSet_party == null || visualSet_party.customerPrefab == null)
                                {
                                    Debug.LogWarning($"[TableManager] '{prefix_party}' 접두사의 손님 비주얼 세트를 찾을 수 없습니다.");
                                    Debug.LogWarning($"[TableManager] 디버그 정보 (2인 파티):");
                                    Debug.LogWarning($"  - 종족: {RaceName_party} (RaceID: {RaceID_party})");
                                    Debug.LogWarning($"  - 접두사: '{prefix_party}'");
                                    Debug.LogWarning($"  - visualData_party null 여부: {visualData_party == null}");
                                    if (visualData_party != null)
                                    {
                                        Debug.LogWarning($"  - visualData_party 종족 이름: {visualData_party.raceName}");
                                        Debug.LogWarning($"  - visualData_party raceId: {visualData_party.raceId}");
                                        Debug.LogWarning($"  - visualSet_party null 여부: {visualSet_party == null}");
                                        if (visualSet_party != null)
                                        {
                                            Debug.LogWarning($"  - customerPrefab null 여부: {visualSet_party.customerPrefab == null}");
                                        }
                                    }
                                    continue;
                                }

                                GameObject customerPrefab_party = visualSet_party.customerPrefab;
                                Sprite portraitSprite_party = visualSet_party.portraitSprite;

                                // 프리팹 => 씬에서 쓰이는 실 객체로 인스턴스화
                                GameObject customer_party = Instantiate(customerPrefab_party, CustomerTransform.transform.position, Quaternion.identity);
                                customer_party.name = GuestName + Time.frameCount + "_" + j;

                                // GuestController 자동 할당 및 초기화
                                GuestController guestController_party = customer_party.GetComponent<GuestController>();
                                if (guestController_party == null)
                                {
                                    guestController_party = customer_party.AddComponent<GuestController>();
                                }

                                // CustomerData 생성 및 할당
                                guestController_party.customerData = new CustomerData(CustomerCount - 1 + j, GuestName, RaceID_party, null, false,
                                null, 0, 0, 1, "Normal", 5, customerPrefab_party.name, portraitSprite_party);
                                guestController_party.desiredPartySize = 2;

                                // 할당되지 않은 필드 자동 할당
                                if (guestController_party.tableManager == null)
                                {
                                    guestController_party.tableManager = this;
                                }
                                if (guestController_party.pathfinder == null)
                                {
                                    guestController_party.pathfinder = CustomerPath;
                                }
                            }
                            CustomerCount -= 2;
                            break;
                    }
                }
            }
        }
        CleanupWaitingLine();
    }
    #endregion

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
    /// <summary>
    /// 테이블 예약 관리
    /// </summary>
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