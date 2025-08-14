using UnityEngine;
using UnityEngine.UI;

public class CanvasSwapper : MonoBehaviour
{
    public CanvasScaler canvasScaler
    {
        get
        {
            if (m_CanvasScaler == null && !TryGetComponent(out m_CanvasScaler))
                Debug.LogWarning("No Canvas Scalar Found.");
            return m_CanvasScaler;
        }
    }
    CanvasScaler m_CanvasScaler;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (Application.isMobilePlatform && Application.platform == RuntimePlatform.WebGLPlayer)
        {
            SwapToMobile();
        }
        else
        {
            SwapToDesktop();
        }
    }

    [ContextMenu("Swap To Mobile")]
    public void SwapToMobile()
    {
        canvasScaler.referenceResolution = new Vector2(1080, 1920);
        canvasScaler.matchWidthOrHeight = 1f;
    }

    [ContextMenu("Swap To Desktop")]
    public void SwapToDesktop()
    {
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0f;
    }
}
