using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 토스트 메시지 UI
/// 짧은 시간 동안 화면에 메시지를 표시한 후 자동으로 사라집니다.
/// </summary>
public class ToastMessageUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Animation Settings")]
    [SerializeField] private float displayDuration = 2.0f;
    [SerializeField] private float fadeInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private Coroutine currentToastCoroutine;

    private void Awake()
    {
        if (toastPanel != null)
        {
            // CanvasGroup이 없으면 추가
            canvasGroup = toastPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = toastPanel.AddComponent<CanvasGroup>();
            }

            // 초기에는 토스트 패널 비활성화
            toastPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 토스트 메시지를 표시합니다.
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    public void ShowToast(string message)
    {
        // 이미 토스트가 표시 중이면 중단하고 새로운 토스트 표시
        if (currentToastCoroutine != null)
        {
            StopCoroutine(currentToastCoroutine);
        }

        currentToastCoroutine = StartCoroutine(ToastCoroutine(message));
    }

    private IEnumerator ToastCoroutine(string message)
    {
        // 메시지 설정
        messageText.text = message;
        toastPanel.SetActive(true);

        // 페이드 인
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeInDuration));

        // 표시 유지
        yield return new WaitForSeconds(displayDuration);

        // 페이드 아웃
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeOutDuration));

        // 패널 비활성화
        toastPanel.SetActive(false);
        currentToastCoroutine = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        cg.alpha = end;
    }
}
