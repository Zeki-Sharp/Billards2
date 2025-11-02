using UnityEngine;
using MoreMountains.Feedbacks;

namespace DeepSpaceLabs.SAM
{

/// <summary>
/// 形状特效控制器 - 动态生成形状 Mesh + MMF 动画
/// 
/// 【核心职责】：
/// - 根据传入的顶点动态生成 Mesh（三角形、圆形等）
/// - 调用 MMF Player 播放视觉动画（颜色淡出、销毁等）
/// 
/// 【设计理由】：
/// - 脚本只负责几何生成（~15行核心代码）
/// - 视觉效果交给 MMF Player（无需改代码，Inspector 配置）
/// - 支持扩展多种形状
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ShapeEffectController : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private MMF_Player feedbackPlayer;
    
    [Header("调试")]
    [SerializeField] private bool showDebugLog = false;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        
        if (feedbackPlayer == null)
        {
            feedbackPlayer = GetComponent<MMF_Player>();
        }
    }
    
    /// <summary>
    /// 设置三角形形状并播放动画
    /// </summary>
    /// <param name="p1">顶点1</param>
    /// <param name="p2">顶点2</param>
    /// <param name="p3">顶点3</param>
    public void SetTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // 生成三角形 Mesh
        Mesh mesh = new Mesh();
        mesh.name = "DynamicTriangle";
        
        // 设置顶点（世界坐标转为本地坐标）
        Vector3 center = (p1 + p2 + p3) / 3f;
        transform.position = center;
        
        Vector3[] vertices = new Vector3[]
        {
            transform.InverseTransformPoint(p1),
            transform.InverseTransformPoint(p2),
            transform.InverseTransformPoint(p3)
        };
        
        // 设置三角形索引
        int[] triangles = new int[] { 0, 1, 2 };
        
        // 设置UV（用于材质映射）
        Vector2[] uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0.5f, 1)
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
        
        if (showDebugLog)
        {
            Debug.Log($"[ShapeEffectController] 生成三角形 Mesh，中心: {center}, 顶点: [{p1}, {p2}, {p3}]");
        }
        
        // 播放 MMF 动画
        PlayEffect();
    }
    
    /// <summary>
    /// 设置圆形形状并播放动画
    /// </summary>
    /// <param name="center">圆心</param>
    /// <param name="radius">半径</param>
    /// <param name="segments">圆形分段数（默认32）</param>
    public void SetCircle(Vector3 center, float radius, int segments = 32)
    {
        // 生成圆形 Mesh
        Mesh mesh = new Mesh();
        mesh.name = "DynamicCircle";
        
        transform.position = center;
        
        // 生成圆形顶点
        Vector3[] vertices = new Vector3[segments + 1];
        vertices[0] = Vector3.zero; // 中心点
        
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );
        }
        
        // 生成三角形索引
        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % segments + 1;
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        meshFilter.mesh = mesh;
        
        if (showDebugLog)
        {
            Debug.Log($"[ShapeEffectController] 生成圆形 Mesh，中心: {center}, 半径: {radius}");
        }
        
        // 播放 MMF 动画
        PlayEffect();
    }
    
    /// <summary>
    /// 播放特效动画（调用 MMF Player）
    /// </summary>
    private void PlayEffect()
    {
        if (feedbackPlayer != null)
        {
            feedbackPlayer.PlayFeedbacks();
        }
        else
        {
            Debug.LogWarning("[ShapeEffectController] MMF_Player 未配置，无法播放动画");
            // 如果没有 MMF，延迟销毁
            Destroy(gameObject, 0.5f);
        }
    }
}

} // namespace DeepSpaceLabs.SAM

