using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GuestController : MonoBehaviour
{
    public IsometricPathfinder pathfinder;
    [SerializeField] private float moveSpeed = 5f;

    [HideInInspector]
    public TableManager tableManager;

    [Header("손님 그룹 설정")]
    public int desiredPartySize = 1;

    [HideInInspector]
    public GameObject groupPartner;

    private Transform Target;
    private List<Vector3Int> currentPath;
    private int currentPathIndex;
    private bool isMoving = false;
    
    #region 애니메이션 제어를 위한 Public 프로퍼티 (추가됨)
    /// <summary>
    /// [수정됨] NPC의 현재 이동 상태를 반환하는 public 프로퍼티
    /// CharacterAnimationController에서 IsWalking 파라미터를 제어하기 위해 추가됨
    /// </summary>
    public bool IsMoving => isMoving;
    
    /// <summary>
    /// [수정됨] NPC의 현재 이동 속도 벡터를 반환하는 public 프로퍼티
    /// CharacterAnimationController에서 MoveY 파라미터를 제어하기 위해 추가됨
    /// 매 프레임마다 MoveAlongPath()에서 계산되어 업데이트됨
    /// </summary>
    public Vector3 CurrentVelocity { get; private set; }
    #endregion
    private GameObject assignedTable;
    private bool isWaiting = false;
    private int myWaitingPosition = -1;
    private GameObject waitingTargetObject;
    private bool isSeated = false;
    private Transform assignedSeat; 
    [SerializeField] private float UIDelay = 3.0f; // Task UI 생성 지연 시간

    void Start()
    {
        // [수정됨] CurrentVelocity 초기화
        // 게임 시작 시 속도를 0으로 설정하여 애니메이션이 올바르게 시작되도록 함
        CurrentVelocity = Vector3.zero;
        if (pathfinder == null)
        {
            Debug.LogError("Pathfinder 컴포넌트가 할당되지 않았습니다."); // 없으면 곴란함!!!
            return;
        }

        if (desiredPartySize == 2)
        {
            HandleTwoPersonParty();
        }
        else
        {
            DetermineTarget();
        }
    }

    void Update()
    {
        // [수정됨] pathfinder가 없거나 앉아있는 상태일 때 속도를 0으로 설정
        // CharacterAnimationController가 정지 상태를 올바르게 감지할 수 있도록 함
        if (pathfinder == null || isSeated)
        {
            CurrentVelocity = Vector3.zero;
            return;
        }

        if (isWaiting)
        {
            // 대기 상태일 경우 테이블 확인
            CheckForAvailableTable();
        }

        if (Target == null)
        {
            // 대상이 없으면 다시 타겟 탐색
            DetermineTarget();
        }
        // [수정됨] Target이 없을 때 속도를 0으로 설정
        // 목표가 없으면 이동하지 않으므로 애니메이션도 정지 상태로 설정
        if (Target == null)
        {
            CurrentVelocity = Vector3.zero;
            return;
        }

        // 이동 중 X(멈춰있거나 도착한 경우) => 경로 계산 시작
        if (!isMoving)
        {

            Vector3 targetPos = new Vector3(Target.position.x, Target.position.y, 0);
            Vector3 startPos = new Vector3(this.transform.position.x, this.transform.position.y, 0);
            Vector3Int targetCell = pathfinder.WorldToCell(targetPos);
            Vector3Int startCell = pathfinder.WorldToCell(startPos);

            // 목표 Cell 의 좌표(Vector3) 리스트를 받음
            currentPath = pathfinder.FindPath(startCell, targetCell);

            if (currentPath != null) // 경로가 유효하면 이동 시작
            {
                pathfinder.DrawPath(currentPath);
                currentPathIndex = 0; // 경로의 시작점으로 초기화
                isMoving = true; // 바로 이동 시작
            }
            else
            {
                // [수정됨] 경로를 찾을 수 없을 때 속도를 0으로 설정
                // 경로가 없으면 이동할 수 없으므로 애니메이션도 정지 상태로 설정
                CurrentVelocity = Vector3.zero;
            }
        }

        if (isMoving && currentPath != null) // 이동 중이면 경로를 따라 이동
        {
            MoveAlongPath(); // 경로를 따라 이동
        }
        else if (!isMoving)
        {
            // [수정됨] 이동하지 않을 때 속도를 0으로 설정
            // isMoving이 false일 때 애니메이션이 정지 상태로 전환되도록 함
            CurrentVelocity = Vector3.zero;
        }
        else if (!isMoving)
        {
            // [수정됨] 이동하지 않을 때 속도를 0으로 설정
            // isMoving이 false일 때 애니메이션이 정지 상태로 전환되도록 함
            CurrentVelocity = Vector3.zero;
        }
    }

    void OnDestroy()
    {
        if (assignedTable != null && assignedSeat != null)
        {
            TableClass tableComp = assignedTable.GetComponent<TableClass>();
            if (tableComp != null)
            {
                tableComp.ReleaseSeat(this.gameObject); // 좌석 해제
            }
        }

        if (waitingTargetObject != null) // 대기 타겟 오브젝트가 있으면
        {
            DestroyImmediate(waitingTargetObject); // 대기 타겟 오브젝트 제거
        }
    }
    
    #region 테이블 & 좌석 할당
    /// <summary>
    /// 테이블과 좌석을 할당하는 메서드
    /// </summary>
    /// <param name="table">할당할 테이블 게임 오브젝트</param>
    /// <param name="seat">할당할 좌석 트랜스폼</param>
    public void AssignTableAndSeat(GameObject table, Transform seat)
    {
        assignedTable = table;
        assignedSeat = seat;
        Target = seat;
        isWaiting = false;
        isMoving = false;

        if (myWaitingPosition != -1) // 대기열에 있으면
        {
            RemoveFromWaitingLine(); // 대기열에서 제거
        }
    }
    #endregion

    #region 테이블 체킹 및 타겟 설정
    void CheckForAvailableTable() // 예약 가능한 테이블이 생겼는지 확인
    {
        if (desiredPartySize == 2 && groupPartner == null)
        {
            GameObject partner = tableManager.FindPartnerForTwoPersonParty(this.gameObject);
            
            if (partner != null)
            {
                groupPartner = partner;
                GuestController partnerController = partner.GetComponent<GuestController>();
                if (partnerController != null)
                {
                    partnerController.groupPartner = this.gameObject;
                }

                tableManager.RemoveFromPartnerWaitingList(partner);

                bool reserved = tableManager.TryReserveTableForGroup(this.gameObject, partner); // 테이블 예약 시도

                if (reserved)
                {
                    return;
                }
            }
            
            return;
        }

        GameObject partialTable = tableManager.GetPartiallyOccupiedTable(desiredPartySize); // 2명 이상의 테이블 중 예약 가능한 테이블 탐색

        if (partialTable != null) // 부분적으로 차있는 테이블이 있으면
        {
            TableClass tableComp = partialTable.GetComponent<TableClass>();
            int seatedCount = tableComp.Seated_Customer.Count; // 해당 테이블의 현재 앉아있는 수와
            // // 예약된 수를 확인함, 만약 테이블 예약 리스트의 해당 테이블에 대한 예약이 없으면 해당 테이블의 예약 수를 0으로 간주
            int reservedCount = tableManager.tableReservations.ContainsKey(partialTable) ? 
                                tableManager.tableReservations[partialTable].Count : 0;

            if (seatedCount + reservedCount < tableComp.MAX_Capacity) // 앉아있는 수 + 예약된 수 < 테이블의 최대 수용량일 경우
            {
                Transform availableSeat = tableComp.GetAvailableSeatForGuest(this.gameObject); // 사용 가능한 좌석 탐색
                if (availableSeat != null)
                {
                    tableManager.ReserveTable(partialTable, this.gameObject); // 해당 테이블 예약

                    if (tableManager.tableReservations.ContainsKey(partialTable) &&
                        tableManager.tableReservations[partialTable].Contains(this.gameObject)) // 예약 리스트에 해당 테이블이 있다면
                    {
                        RemoveFromWaitingLine(); // 대기열에서 제거
                        assignedTable = partialTable; // 테이블 할당
                        assignedSeat = availableSeat; // 좌석 할당
                        Target = assignedSeat; // 타겟 설정
                        isMoving = false; // 이동 상태 초기화
                        return;
                    }
                }
            }
        }

        // 부분적으로 차있는 테이블이 없으면 => 바로 완전 비어있는 테이블 탐색
        GameObject availableTable = tableManager.GetAvailableTable(desiredPartySize); // 완전히 비어있는 테이블 탐색
        if (availableTable != null) // 사용 가능한 테이블이 있으면
        {
            TableClass tableComp = availableTable.GetComponent<TableClass>(); 
            // 해당 테이블의 예약된 수를 확인, 만약 테이블 예약 리스트의 해당 테이블에 대한 예약이 없으면 해당 테이블의 예약 수를 0으로 간주
            int reservedCount = tableManager.tableReservations.ContainsKey(availableTable) ?
                                tableManager.tableReservations[availableTable].Count : 0;

            // 완전 비어있는 테이블의 예약된 수 < 테이블의 최대 수용량일 경우
            if (reservedCount < tableComp.MAX_Capacity)
            {
                // 사용 가능한 좌석 탐색
                Transform availableSeat = tableComp.GetAvailableSeatForGuest(this.gameObject);
                if (availableSeat != null)
                {
                    // 해당 테이블 예약
                    tableManager.ReserveTable(availableTable, this.gameObject);

                    // 예약 리스트에 해당 테이블이 있다면
                    if (tableManager.tableReservations.ContainsKey(availableTable) &&
                        tableManager.tableReservations[availableTable].Contains(this.gameObject))
                    {
                        // 대기열에서 제거
                        RemoveFromWaitingLine();
                        assignedTable = availableTable; // 테이블 할당
                        assignedSeat = availableSeat; // 좌석 할당
                        Target = assignedSeat; // 타겟 설정
                        isMoving = false; // 이동 상태 초기화
                    }
                }
            }
        }
    }

    /// <summary>
    /// 타겟 결정 메소드
    /// </summary>
    void DetermineTarget()
    {
        // 2인 파티인데 파트너가 없으면 대기열에 추가
        if (desiredPartySize == 2 && groupPartner == null)
        {
            if (tableManager.CustomerWaitingTransform != null)
            {
                AddToWaitingLine();
                assignedTable = null;
                assignedSeat = null;
            }
            return;
        }

        GameObject partialTable = tableManager.GetPartiallyOccupiedTable(desiredPartySize);
        if (partialTable != null) // 부분적으로 차있는 테이블이 있으면
        {
            TableClass tableComp = partialTable.GetComponent<TableClass>();
            int seatedCount = tableComp.Seated_Customer.Count;
            int reservedCount = tableManager.tableReservations.ContainsKey(partialTable) ?
                                tableManager.tableReservations[partialTable].Count : 0;

            if (seatedCount + reservedCount < tableComp.MAX_Capacity)
            {
                Transform availableSeat = tableComp.GetAvailableSeatForGuest(this.gameObject);
                if (availableSeat != null)
                {
                    tableManager.ReserveTable(partialTable, this.gameObject); // 해당 테이블 예약

                    if (tableManager.tableReservations.ContainsKey(partialTable) &&
                        tableManager.tableReservations[partialTable].Contains(this.gameObject))
                    {
                        isWaiting = false;
                        assignedTable = partialTable;
                        assignedSeat = availableSeat;
                        Target = assignedSeat;
                        return;
                    }
                }
            }
        }

        GameObject availableTable = tableManager.GetAvailableTable(desiredPartySize); // 완전히 비어있는 테이블 탐색
        if (availableTable != null)
        {
            TableClass tableComp = availableTable.GetComponent<TableClass>();
            int reservedCount = tableManager.tableReservations.ContainsKey(availableTable) ?
                                tableManager.tableReservations[availableTable].Count : 0;

            if (reservedCount < tableComp.MAX_Capacity)
            {
                Transform availableSeat = tableComp.GetAvailableSeatForGuest(this.gameObject);
                if (availableSeat != null)
                {
                    tableManager.ReserveTable(availableTable, this.gameObject); // 해당 테이블 예약

                    if (tableManager.tableReservations.ContainsKey(availableTable) &&
                        tableManager.tableReservations[availableTable].Contains(this.gameObject))
                    {
                        isWaiting = false;
                        assignedTable = availableTable;
                        assignedSeat = availableSeat;
                        Target = assignedSeat;
                        return;
                    }
                }
            }
        }

        if (tableManager.CustomerWaitingTransform != null) // 대기 위치가 설정되어 있으면
        {
            AddToWaitingLine(); // 대기열에 추가
            assignedTable = null;
            assignedSeat = null;
        }
    }
    #endregion

    #region 대기열 관리
    void AddToWaitingLine() // 대기열에 추가
    {
        if (myWaitingPosition == -1) // 아직 대기열에 없으면
        {
            myWaitingPosition = tableManager.AddToWaitingLine(this.gameObject); // 대기열에 추가하고 내 위치 저장
            SetIsometricWaitingTarget(); // 대기 타겟 설정
        }
    }

    void RemoveFromWaitingLine() // 대기열에서 제거
    {
        if (myWaitingPosition != -1) // 대기열에 있으면
        {
            tableManager.RemoveFromWaitingLine(this.gameObject); // 대기열에서 제거
            myWaitingPosition = -1;
            isWaiting = false;

            if (waitingTargetObject != null) // 대기 타겟 오브젝트가 있으면
            {
                DestroyImmediate(waitingTargetObject); // 대기 타겟 오브젝트 제거
                waitingTargetObject = null; // 참조 초기화
            }
        }
    }

    void SetIsometricWaitingTarget() // 대기 타겟 설정
    {
        Vector3 waitingPos = tableManager.CalculateIsometricWaitingPosition(myWaitingPosition); // 내 대기 위치 계산

        if (waitingTargetObject == null)
        {
            waitingTargetObject = new GameObject($"WaitingTarget_{this.gameObject.name}"); // 대기할때 타겟 대상이 될 오브젝트 생성
            Target = waitingTargetObject.transform; // 대기 타겟 오브젝트 생성 및 할당
        }

        Target.position = waitingPos; // 대기 타겟 오브젝트 위치 설정
        isMoving = false; // 이동 상태 초기화
    }

    public void UpdateWaitingPosition(int newPosition) // 대기 위치 업데이트
    {
        if (isWaiting && myWaitingPosition != -1)
        {
            myWaitingPosition = newPosition; // 내 대기 위치 업데이트
            SetIsometricWaitingTarget(); // 대기 타겟 설정
        }
    }
    #endregion

    #region 이동
    void MoveAlongPath() // 이동
    {
        if (currentPathIndex >= currentPath.Count) // 길의 끝에 도달했을 경우
        {
            isMoving = false;
            // [수정됨] 목적지에 도달했을 때 속도를 0으로 설정
            // 도착 시 애니메이션이 정지 상태로 전환되도록 함
            CurrentVelocity = Vector3.zero;
            OnReachedDestination();
            return;
        }

        // [수정됨] 이동 전 위치를 저장 (속도 계산을 위해)
        // 이전 프레임의 위치와 현재 프레임의 위치 차이로 속도를 계산
        Vector3 previousPosition = this.transform.position;
        Vector3 targetPos = pathfinder.CellToWorld(currentPath[currentPathIndex]);
        this.transform.position = Vector3.MoveTowards(this.transform.position, targetPos, moveSpeed * Time.deltaTime);
        
        // [수정됨] 현재 속도 계산 (애니메이션 제어용)
        // CharacterAnimationController에서 MoveY 파라미터를 제어하기 위해 필요
        // 위치 변화량을 Time.deltaTime으로 나누어 초당 이동 속도 벡터를 계산
        // 이 값의 Y 성분이 MoveY 파라미터로 사용되어 위/아래 이동 애니메이션을 제어함
        CurrentVelocity = (this.transform.position - previousPosition) / Time.deltaTime;

        if (Vector3.Distance(this.transform.position, targetPos) < 0.01f) // 다음 지점에 도달했을 경우
        {
            currentPathIndex++; // 다음 경로 지점으로 이동
        }
    }
    #endregion

    #region 도착 처리
    /// <summary>
    /// 목적지에 도착했을 때 호출되는 메서드
    /// 이동 중, 테이블/좌석에 도착했을 때 or 대기열에 도착했을때 호출됨
    /// 테이블/좌석에 도착했을때, 만약 이미 예약 or 좌석이 꽉 찬 상태면은 다른 좌석을 재탐색함
    /// </summary>
    void OnReachedDestination() // 목적지 도착 처리
    {
        // 1. 테이블과 좌석이 할당된 상태로 도착했을 경우 (식사하러 옴)
        if (assignedTable != null && assignedSeat != null)
        {
            TableClass tableComp = assignedTable.GetComponent<TableClass>();
            // 도착했더니 좌석이 이미 좌석 수용량을 넘은 상태라면? 즉 좌석이 꽉 찼다면 !
            if (tableComp.Seated_Customer.Count >= tableComp.MAX_Capacity)
            {
                tableComp.ReleaseSeat(this.gameObject); // 좌석에 이 오브젝트 앉아있다는 걸 해제

                tableManager.CancelReservation(assignedTable, this.gameObject); // 해당 좌석에 이 오브젝트의 예약 취소

                assignedTable = null;
                assignedSeat = null;
                Target = null;
                isMoving = false;
                DetermineTarget(); // 다시 타겟 재탐색
                return;
            }

            // 도착했더니 좌석이 다른 손님에게 이미 할당된 상태라면
            if (!tableComp.IsSeatAssignedToGuest(assignedSeat, this.gameObject))
            {
                tableManager.CancelReservation(assignedTable, this.gameObject); // 해당 좌석에 이 오브젝트의 예약 취소

                assignedTable = null;
                assignedSeat = null;
                Target = null;
                isMoving = false;
                DetermineTarget(); // 다시 타겟 재탐색
                return;
            }

            // 좌석에 손님이 앉기가 충분한 상태(좌석 수용량이 남아있는 상태, 예약으로 좌석이 꽉 찬 상태 X) 라면
            // 손님을 좌석에 앉힘
            if (!tableComp.Seated_Customer.Contains(this.gameObject))
            {
                tableComp.Seated_Customer.Add(this.gameObject); // 좌석 - 손님 리스트에 해당 손님 추가
                tableComp.isCustomerSeated = true; // 테이블에 손님이 앉아있음을 표시

                tableManager.CancelReservation(assignedTable, this.gameObject); // 예약 취소 (앉았으니 예약은 필요 없음)

                transform.position = assignedSeat.position; // 손님 위치를 좌석 위치로 고정

                isSeated = true; // 손님이 앉았음을 표시

                ///<summary>
                /// 손님이 좌석에 앉았을 때 추가 로직 작성 가능
                /// </summary>
                
                // 착석 후 몇 초 뒤에 Task UI 생성
                StartCoroutine(ShowTaskUIAfterDelay(UIDelay));
            } 
        }
        // 2. 대기열에 있는 상태로 도착했을 경우
        else if (myWaitingPosition != -1)
        {
            if (!isWaiting)
            {
                isWaiting = true; // 대기 상태로 설정
            }
        }
    }
    #endregion

    #region 2인 파티
    /// <summary>
    /// 2인 파티 처리를 담당하는 메서드
    /// </summary>
    void HandleTwoPersonParty()
    {
        GameObject partner = tableManager.FindPartnerForTwoPersonParty(gameObject); // 테이블 매니저를 통해 파트너 찾음

        if (partner != null) // 파트너 있으면 ?
        {
            groupPartner = partner; // 파트너 설정

            GuestController partnerController = partner.GetComponent<GuestController>();
            if (partnerController != null)
            {
                partnerController.groupPartner = gameObject; // 내 파트너 = 나, 상호 설정
            }

            tableManager.RemoveFromPartnerWaitingList(partner); // 파트너 대기열에서 제거

            bool reserved = tableManager.TryReserveTableForGroup(gameObject, partner); // 2인 파티용 테이블 예약 시도

            if (!reserved) // 예약 실패 시
            {
                AddToWaitingLine(); // 대기열에 추가
                if (partnerController != null)
                {
                    partnerController.AddToWaitingLine(); // 내 파트너도 대기열에 추가
                }
            }
        }
        else // 파트너가 없으면?
        {
            tableManager.AddToPartnerWaitingList(gameObject); // 파트너 대기열에만 추가하고 다음 2인 손님 대기
        }
    }
    #endregion

    #region 업무 UI 생성
    /// <summary>
    /// 착석 후 지정된 시간이 지나면 Task UI를 생성하는 코루틴
    /// </summary>
    /// <param name="delay">코루틴 대기 시간 (초)</param>
    private IEnumerator ShowTaskUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // 손님이 착석한 상태라면 Task UI 생성
        if (assignedTable != null && isSeated && OrderingManager.Instance != null)
        {
            OrderingManager.Instance.TaskUIInstantiate(this.gameObject);
        }
    }
    #endregion
}