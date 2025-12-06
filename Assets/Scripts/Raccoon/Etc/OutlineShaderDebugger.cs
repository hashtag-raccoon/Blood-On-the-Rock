using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OutlineRenderTexture 디버깅용 스크립트
/// OutlineCamera GameObject에 추가하여 RenderTexture가 제대로 렌더링되는지 확인
/// - RenderTexture 정보 출력
/// - 픽셀 샘플링을 통한 검은색 여부 확인
/// - 간단한 GUI 표시
/// 등의 기능이 포함되어 있음, 사용할때만 스크립트를 껐다키고 하길 바람
/// </summary>
public class RenderTextureDebugger : MonoBehaviour
{
    [Header("디버그 설정")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private KeyCode captureKey = KeyCode.Space;

    private void Update()
    {
        if (showDebugInfo && targetCamera != null && targetCamera.targetTexture != null)
        {
            // Space 키를 누르면 RenderTexture 정보 출력
            if (Input.GetKeyDown(captureKey))
            {
                DebugRenderTexture();
            }
        }
    }

    private void OnGUI()
    {
        if (!showDebugInfo || targetCamera == null || targetCamera.targetTexture == null)
            return;

        GUILayout.BeginArea(new Rect(10, 10, 400, 300));
        GUILayout.BeginVertical("box");

        GUILayout.Label($"<b>RenderTexture Debug Info</b>", new GUIStyle(GUI.skin.label) { richText = true });
        GUILayout.Space(5);

        RenderTexture rt = targetCamera.targetTexture;
        GUILayout.Label($"Size: {rt.width} x {rt.height}");
        GUILayout.Label($"Format: {rt.format}");
        GUILayout.Label($"Depth: {rt.depth}");
        GUILayout.Label($"Camera Active: {targetCamera.gameObject.activeInHierarchy}");
        GUILayout.Label($"Camera Enabled: {targetCamera.enabled}");
        GUILayout.Label($"Culling Mask: {targetCamera.cullingMask}");
        GUILayout.Label($"Background: {targetCamera.backgroundColor}");

        GUILayout.Space(5);
        GUILayout.Label($"Press [{captureKey}] to capture debug info");

        GUILayout.EndVertical();
        GUILayout.EndArea();

        // RenderTexture 미리보기 (우측 상단에 작게 표시)
        if (rt != null)
        {
            float previewSize = 200f;
            Rect previewRect = new Rect(Screen.width - previewSize - 10, 10, previewSize, previewSize);
            GUI.DrawTexture(previewRect, rt, ScaleMode.ScaleToFit, true);
            GUI.Box(previewRect, "");
        }
    }

    private void DebugRenderTexture()
    {
        if (targetCamera == null || targetCamera.targetTexture == null)
        {
            Debug.LogWarning("[RenderTextureDebugger] Camera 또는 RenderTexture가 null입니다!");
            return;
        }

        RenderTexture rt = targetCamera.targetTexture;
        
        Debug.Log($"=== RenderTexture Debug Info ===");
        Debug.Log($"RenderTexture: {rt.name}");
        Debug.Log($"Size: {rt.width} x {rt.height}");
        Debug.Log($"Format: {rt.format}");
        Debug.Log($"Depth Buffer: {rt.depth} bit");
        Debug.Log($"Anti-aliasing: {rt.antiAliasing}x");
        Debug.Log($"Filter Mode: {rt.filterMode}");
        Debug.Log($"Wrap Mode: {rt.wrapMode}");
        
        Debug.Log($"\n=== Camera Info ===");
        Debug.Log($"Camera: {targetCamera.name}");
        Debug.Log($"Active: {targetCamera.gameObject.activeInHierarchy}");
        Debug.Log($"Enabled: {targetCamera.enabled}");
        Debug.Log($"Position: {targetCamera.transform.position}");
        Debug.Log($"Rotation: {targetCamera.transform.rotation.eulerAngles}");
        Debug.Log($"Culling Mask: {targetCamera.cullingMask} ({LayerMaskToString(targetCamera.cullingMask)})");
        Debug.Log($"Clear Flags: {targetCamera.clearFlags}");
        Debug.Log($"Background: {targetCamera.backgroundColor}");
        Debug.Log($"Orthographic Size: {targetCamera.orthographicSize}");
        Debug.Log($"Projection: {targetCamera.orthographic}");

        // 픽셀 샘플링 (중앙 픽셀 확인)
        Texture2D tex = RenderTextureToTexture2D(rt);
        if (tex != null)
        {
            Color centerPixel = tex.GetPixel(rt.width / 2, rt.height / 2);
            Debug.Log($"\n=== Pixel Sampling ===");
            Debug.Log($"Center Pixel Color: {centerPixel}");
            
            // 검은색인지 확인
            bool isBlack = centerPixel.r < 0.1f && centerPixel.g < 0.1f && centerPixel.b < 0.1f;
            Debug.Log($"Is mostly black: {isBlack}");
            
            Destroy(tex);
        }

        Debug.Log($"================================");
    }

    private Texture2D RenderTextureToTexture2D(RenderTexture rt)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        RenderTexture.active = currentRT;
        return tex;
    }

    private string LayerMaskToString(int layerMask)
    {
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    if (result.Length > 0) result += ", ";
                    result += layerName;
                }
            }
        }
        return string.IsNullOrEmpty(result) ? "None" : result;
    }
}