using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 바 씬에서 알바생들이 대기하는 위치와 방향을 관리하는 클래스
/// </summary>
public class ArbeitPoint : MonoBehaviour
{
    [Header("대기 위치 설정")]
    [SerializeField] private Transform waitingPosition;

    [Header("대기 방향 설정")]
    [Tooltip("알바생들이 바라볼 방향 (Default: 아래쪽)")]
    [SerializeField] private Vector3 facingDirection = Vector3.down;

    [Header("줄서기 설정/알바생들이 줄 설때의 간격")]
    [SerializeField] private float spacing = 1.5f;

    [Tooltip("아이소메트릭 뷰 기준 줄서기 방향")]
    [SerializeField] private IsometricLineDirection lineDirection = IsometricLineDirection.Up;

    [Header("디버그 옵션")]
    [SerializeField] private bool showDebugGizmos = true;

    [Tooltip("현재 대기 중인 알바생 목록 (디버그용)")]
    [SerializeField] private List<GameObject> waitingArbeiters = new List<GameObject>();

    public enum IsometricLineDirection
    {
        Up = 0,      // 아이소메트릭 위 방향
        Down = 1,    // 아이소메트릭 아래 방향
        Left = 2,    // 아이소메트릭 왼쪽 방향
        Right = 3    // 아이소메트릭 오른쪽 방향
    }

    private void Awake()
    {
        if (waitingPosition == null)
        {
            waitingPosition = transform;
        }
    }

    /// <summary>
    /// 알바생을 대기열에 추가
    /// </summary>
    public int AddToWaitingLine(GameObject arbeiter)
    {
        if (arbeiter == null)
        {
            Debug.LogWarning("[ArbeitPoint] 추가하려는 알바생이 null입니다.");
            return -1;
        }

        if (!waitingArbeiters.Contains(arbeiter))
        {
            waitingArbeiters.Add(arbeiter);
            return waitingArbeiters.Count - 1;
        }

        return waitingArbeiters.IndexOf(arbeiter);
    }

    /// <summary>
    /// 알바생을 대기열에서 제거
    /// </summary>
    public void RemoveFromWaitingLine(GameObject arbeiter)
    {
        if (arbeiter == null) return;

        if (waitingArbeiters.Contains(arbeiter))
        {
            waitingArbeiters.Remove(arbeiter);
            UpdateAllWaitingPositions();
        }
    }

    /// <summary>
    /// 모든 대기 중인 알바생의 위치 업데이트
    /// </summary>
    private void UpdateAllWaitingPositions()
    {
        for (int i = 0; i < waitingArbeiters.Count; i++)
        {
            if (waitingArbeiters[i] != null)
            {
                ArbeitController controller = waitingArbeiters[i].GetComponent<ArbeitController>();
                if (controller != null)
                {
                    controller.UpdateWaitingPosition(i);
                }
            }
        }
    }

    /// <summary>
    /// 특정 위치의 대기 좌표 계산 (아이소메트릭 좌표계)
    /// </summary>
    public Vector3 CalculateWaitingPosition(int position)
    {
        if (waitingPosition == null)
        {
            Debug.LogError("[ArbeitPoint] waitingPosition이 null입니다.");
            return Vector3.zero;
        }

        Vector3 basePosition = waitingPosition.position;
        Vector3 offset = Vector3.zero;

        // 아이소메트릭 뷰에서의 4방향으로 오프셋을 계산함
        switch (lineDirection)
        {
            case IsometricLineDirection.Up:
                offset = new Vector3(-position * spacing * 0.5f, position * spacing * 0.5f, 0);
                break;

            case IsometricLineDirection.Down:
                offset = new Vector3(position * spacing * 0.5f, -position * spacing * 0.5f, 0);
                break;

            case IsometricLineDirection.Left:
                offset = new Vector3(-position * spacing * 0.5f, -position * spacing * 0.5f, 0);
                break;

            case IsometricLineDirection.Right:
                offset = new Vector3(position * spacing * 0.5f, position * spacing * 0.5f, 0);
                break;
        }

        return basePosition + offset;
    }

    /// <summary>
    /// 알바생이 바라볼 방향 반환
    /// </summary>
    public Vector3 GetFacingDirection()
    {
        return facingDirection.normalized;
    }

    /// <summary>
    /// 대기 중인 알바생 수 반환
    /// </summary>
    public int GetWaitingCount()
    {
        return waitingArbeiters.Count;
    }

    /// <summary>
    /// 특정 알바생의 대기 위치 인덱스 반환
    /// </summary>
    public int GetWaitingIndex(GameObject arbeiter)
    {
        return waitingArbeiters.IndexOf(arbeiter);
    }

    /// <summary>
    /// 대기열 비우는 메소드 
    /// </summary>
    public void ClearWaitingLine()
    {
        waitingArbeiters.Clear();
    }

    #region 디버깅용 기즈모
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || waitingPosition == null) return;

        // 기준 위치 표시
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(waitingPosition.position, 0.3f);

        // 방향 표시
        Gizmos.color = Color.yellow;
        Vector3 directionEnd = waitingPosition.position + facingDirection.normalized * 0.5f;
        Gizmos.DrawLine(waitingPosition.position, directionEnd);
        DrawArrow(waitingPosition.position, directionEnd, Color.yellow);

        // 대기 위치들 표시 (최대 5개)
        Gizmos.color = Color.green;
        for (int i = 0; i < 5; i++)
        {
            Vector3 pos = CalculateWaitingPosition(i);
            Gizmos.DrawWireSphere(pos, 0.2f);

            // 인덱스 표시
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 0.3f, i.ToString());
#endif
        }
    }

    private void DrawArrow(Vector3 start, Vector3 end, Color color)
    {
        Gizmos.color = color;
        Vector3 direction = (end - start).normalized;
        Vector3 right = Vector3.Cross(Vector3.forward, direction) * 0.2f;

        Gizmos.DrawLine(end, end - direction * 0.3f + right);
        Gizmos.DrawLine(end, end - direction * 0.3f - right);
    }
    #endregion
}
