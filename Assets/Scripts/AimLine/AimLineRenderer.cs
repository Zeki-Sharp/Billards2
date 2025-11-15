using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瞄准线渲染器 - 专门负责瞄准线的渲染管理
/// 
/// 【核心职责】：
/// - 管理所有LineRenderer的创建、更新和销毁
/// - 处理瞄准线的分段渲染
/// - 与AimLineMaterialController协作处理材质效果
/// - 提供简洁的渲染接口给AimController
/// 
/// 【设计原则】：
/// - 单一职责：只负责渲染，不处理业务逻辑
/// - 组件协作：与AimLineMaterialController协作
/// - 性能优化：减少不必要的对象创建和销毁
/// - 易于使用：提供简洁的公共接口
/// </summary>
public class AimLineRenderer : MonoBehaviour
{
    [Header("渲染设置")]
    [SerializeField] private float defaultLineWidth = 0.1f;
    [SerializeField] private int defaultSortingOrder = 10;
    [SerializeField] private int defaultCapVertices = 8;
    
    [Tooltip("转角圆滑顶点数（0=尖角，5=适中，10=完全圆滑）")]
    [SerializeField] private int defaultCornerVertices = 5;
    
    [Header("碰撞指示器设置")]
    [SerializeField] private Sprite collisionIndicatorSprite; // 碰撞指示器图片
    [SerializeField] private float indicatorSize = 0.3f; // 指示器大小
    [SerializeField] private Color indicatorColor = Color.red; // 指示器颜色
    [SerializeField] private int indicatorSortingOrder = 11; // 指示器排序顺序（比瞄准线高）
    [SerializeField] private bool showCollisionIndicators = true; // 是否显示碰撞指示器
    
    [Header("组件引用")]
    [SerializeField] private AimLineMaterialController materialController;
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugInfo = false;
    
    [Header("投射设置")]
    [SerializeField] private bool projectIndicatorsToGround = true;
    [SerializeField] private float groundHeight = 0f;
    [SerializeField] private float indicatorHeightOffset = 0.01f;
    [SerializeField] private bool flattenIndicators = true;
    [SerializeField] private bool projectLineToGround = true;
    [SerializeField] private float lineHeightOffset = 0.01f;
    
    // 渲染对象管理
    private List<LineRenderer> segmentLines = new List<LineRenderer>();
    private List<GameObject> collisionIndicators = new List<GameObject>();
    private GameObject lineContainer;
    private GameObject indicatorContainer;
    
    void Start()
    {
        InitializeRenderer();
    }
    
    /// <summary>
    /// 初始化渲染器
    /// </summary>
    void InitializeRenderer()
    {
        // 获取材质控制器引用
        if (materialController == null)
        {
            materialController = GetComponent<AimLineMaterialController>();
            if (materialController == null)
            {
                materialController = gameObject.AddComponent<AimLineMaterialController>();
            }
        }
        
        // 创建渲染容器
        CreateLineContainer();
        CreateIndicatorContainer();
        
        if (showDebugInfo)
        {
            Debug.Log("AimLineRenderer: 初始化完成");
        }
    }
    
    /// <summary>
    /// 创建线条容器
    /// </summary>
    void CreateLineContainer()
    {
        if (lineContainer != null)
        {
            DestroyImmediate(lineContainer);
        }
        
        lineContainer = new GameObject("AimLineContainer");
        lineContainer.transform.SetParent(transform);
        lineContainer.transform.localPosition = Vector3.zero;
        lineContainer.transform.localRotation = Quaternion.identity;
    }
    
    /// <summary>
    /// 创建指示器容器
    /// </summary>
    void CreateIndicatorContainer()
    {
        if (indicatorContainer != null)
        {
            DestroyImmediate(indicatorContainer);
        }
        
        indicatorContainer = new GameObject("CollisionIndicatorContainer");
        indicatorContainer.transform.SetParent(transform);
        indicatorContainer.transform.localPosition = Vector3.zero;
        indicatorContainer.transform.localRotation = Quaternion.identity;
    }
    
    
    /// <summary>
    /// 渲染轨迹瞄准线（物理模拟系统）
    /// </summary>
    /// <param name="pathPoints">路径点列表（用于画线）</param>
    /// <param name="collisionPoints">碰撞点列表（用于画指示器）</param>
    public void RenderSegmentedAimLine(List<Vector3> pathPoints, List<Vector3> collisionPoints)
    {
        if (pathPoints == null || pathPoints.Count <= 1)
        {
            ClearAllLines();
            return;
        }
        
        // 清除旧的分段线段和指示器
        ClearSegmentLines();
        ClearCollisionIndicators();
        
        // ✅ 使用单个 LineRenderer 绘制平滑轨迹
        RenderSmoothTrajectory(pathPoints);
        
        // 渲染碰撞指示器
        if (showCollisionIndicators && collisionPoints != null && collisionPoints.Count > 0)
        {
            RenderCollisionIndicatorsFromList(collisionPoints);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"AimLineRenderer: 渲染轨迹 - 路径点: {pathPoints.Count}, 碰撞点: {(collisionPoints != null ? collisionPoints.Count : 0)}");
        }
    }
    
    /// <summary>
    /// 渲染平滑轨迹（新系统：使用单个 LineRenderer）
    /// </summary>
    void RenderSmoothTrajectory(List<Vector3> pathPoints)
    {
        if (pathPoints == null || pathPoints.Count < 2)
        {
            return;
        }
        
        // 创建单个 LineRenderer
        LineRenderer trajectoryLine = CreateSegmentLine(0);
        segmentLines.Add(trajectoryLine);
        
        // 设置所有路径点
        Vector3[] positions = pathPoints.ToArray();
        if (projectLineToGround)
        {
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i].y = groundHeight + lineHeightOffset;
            }
        }
        
        trajectoryLine.positionCount = positions.Length;
        trajectoryLine.SetPositions(positions);
        
        // 应用材质效果（末端渐隐）
        if (materialController != null)
        {
            float totalLength = 0f;
            for (int i = 1; i < pathPoints.Count; i++)
            {
                totalLength += Vector3.Distance(pathPoints[i-1], pathPoints[i]);
            }
            materialController.UpdateSegmentMaterial(trajectoryLine, totalLength, true);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"AimLineRenderer: 渲染平滑轨迹 - 点数: {pathPoints.Count}");
        }
    }
    
    /// <summary>
    /// 清除所有瞄准线
    /// </summary>
    public void ClearAllLines()
    {
        // 清除分段线段和指示器
        ClearSegmentLines();
        ClearCollisionIndicators();
    }
    
    /// <summary>
    /// 计算截断后的路径点
    /// </summary>
    /// <param name="pathPoints">完整路径点列表</param>
    /// <param name="maxDistance">最大距离</param>
    /// <returns>截断后的路径点列表</returns>
    List<Vector3> CalculateTruncatedPath(List<Vector3> pathPoints, float maxDistance)
    {
        List<Vector3> truncatedPoints = new List<Vector3>();
        
        if (pathPoints.Count <= 1)
        {
            return truncatedPoints;
        }
        
        // 添加起点
        truncatedPoints.Add(pathPoints[0]);
        
        float accumulatedDistance = 0f;
        
        // 遍历路径点，计算累积距离
        for (int i = 1; i < pathPoints.Count; i++)
        {
            float segmentDistance = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            
            // 如果加上这段距离会超过最大距离
            if (accumulatedDistance + segmentDistance > maxDistance)
            {
                // 计算在这段中的截断点
                float remainingDistance = maxDistance - accumulatedDistance;
                Vector3 direction = (pathPoints[i] - pathPoints[i - 1]).normalized;
                Vector3 truncationPoint = pathPoints[i - 1] + direction * remainingDistance;
                
                truncatedPoints.Add(truncationPoint);
                break;
            }
            
            // 添加当前点
            truncatedPoints.Add(pathPoints[i]);
            accumulatedDistance += segmentDistance;
        }
        
        return truncatedPoints;
    }
    
    /// <summary>
    /// 获取路径总长度
    /// </summary>
    /// <param name="pathPoints">路径点列表</param>
    /// <returns>总长度</returns>
    public float GetPathTotalLength(List<Vector3> pathPoints)
    {
        if (pathPoints == null || pathPoints.Count <= 1)
        {
            return 0f;
        }
        
        float totalLength = 0f;
        for (int i = 1; i < pathPoints.Count; i++)
        {
            totalLength += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
        }
        
        return totalLength;
    }
    
    /// <summary>
    /// 在指定距离处截断路径
    /// </summary>
    /// <param name="pathPoints">完整路径点列表</param>
    /// <param name="truncationDistance">截断距离</param>
    /// <returns>截断后的路径点列表</returns>
    public List<Vector3> TruncatePathAtDistance(List<Vector3> pathPoints, float truncationDistance)
    {
        return CalculateTruncatedPath(pathPoints, truncationDistance);
    }
    
    
    /// <summary>
    /// 创建分段线段
    /// </summary>
    LineRenderer CreateSegmentLine(int segmentIndex)
    {
        GameObject segmentObj = new GameObject($"AimLineSegment_{segmentIndex}");
        segmentObj.transform.SetParent(lineContainer.transform);
        segmentObj.transform.localPosition = Vector3.zero;
        segmentObj.transform.localRotation = Quaternion.identity;
        
        LineRenderer segmentLine = segmentObj.AddComponent<LineRenderer>();
        
        // 设置基础属性
        SetupLineRenderer(segmentLine);
        
        // 设置分段特定属性
        segmentLine.positionCount = 2; // 每个线段只有起点和终点
        
        return segmentLine;
    }
    
    /// <summary>
    /// 设置LineRenderer的基础属性
    /// </summary>
    void SetupLineRenderer(LineRenderer lineRenderer)
    {
        // 设置材质
        if (materialController != null && materialController.GetAimLineMaterial() != null)
        {
            lineRenderer.material = materialController.GetAimLineMaterial();
        }
        else
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        // 设置颜色
        if (materialController != null && materialController.GetAimLineMaterial() != null)
        {
            Material aimMaterial = materialController.GetAimLineMaterial();
            if (aimMaterial.HasProperty("_Tint"))
            {
                Color tintColor = aimMaterial.GetColor("_Tint");
                lineRenderer.startColor = tintColor;
                lineRenderer.endColor = tintColor;
            }
            else
            {
                lineRenderer.startColor = Color.yellow;
                lineRenderer.endColor = Color.yellow;
            }
        }
        else
        {
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = Color.yellow;
        }
        
        // 设置其他属性
        lineRenderer.startWidth = defaultLineWidth;
        lineRenderer.endWidth = defaultLineWidth;
        lineRenderer.sortingOrder = defaultSortingOrder;
        lineRenderer.useWorldSpace = true;
        lineRenderer.numCapVertices = defaultCapVertices;
        lineRenderer.numCornerVertices = defaultCornerVertices;  // ✅ 转角圆滑处理
        lineRenderer.alignment = LineAlignment.View;
    }
    
    /// <summary>
    /// 清除所有分段线段
    /// </summary>
    void ClearSegmentLines()
    {
        foreach (LineRenderer line in segmentLines)
        {
            if (line != null)
            {
                DestroyImmediate(line.gameObject);
            }
        }
        segmentLines.Clear();
    }
    
    /// <summary>
    /// 清除所有碰撞指示器
    /// </summary>
    void ClearCollisionIndicators()
    {
        foreach (GameObject indicator in collisionIndicators)
        {
            if (indicator != null)
            {
                DestroyImmediate(indicator);
            }
        }
        collisionIndicators.Clear();
    }
    
    /// <summary>
    /// 渲染碰撞指示器
    /// </summary>
    /// <param name="collisionPoints">碰撞点列表</param>
    void RenderCollisionIndicatorsFromList(List<Vector3> collisionPoints)
    {
        if (collisionPoints == null || collisionPoints.Count == 0)
        {
            return;
        }
        
        // 为每个碰撞点创建指示器
        for (int i = 0; i < collisionPoints.Count; i++)
        {
            CreateCollisionIndicator(collisionPoints[i], i);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"AimLineRenderer: 渲染碰撞指示器 - 数量: {collisionPoints.Count}");
        }
    }
    
    /// <summary>
    /// 创建单个碰撞指示器
    /// </summary>
    /// <param name="position">指示器位置</param>
    /// <param name="index">指示器索引</param>
    void CreateCollisionIndicator(Vector3 position, int index)
    {
        GameObject indicatorObj = new GameObject($"CollisionIndicator_{index}");
        indicatorObj.transform.SetParent(indicatorContainer.transform);
        indicatorObj.transform.position = ProjectIndicatorPosition(position);
        ApplyGroundFacingRotation(indicatorObj.transform);
        
        // 添加SpriteRenderer组件
        SpriteRenderer spriteRenderer = indicatorObj.AddComponent<SpriteRenderer>();
        
        // 设置图片
        if (collisionIndicatorSprite != null)
        {
            spriteRenderer.sprite = collisionIndicatorSprite;
        }
        else
        {
            // 如果没有设置图片，使用默认的白色方块
            spriteRenderer.sprite = CreateDefaultIndicatorSprite();
        }
        
        // 设置属性
        spriteRenderer.color = indicatorColor;
        spriteRenderer.sortingOrder = indicatorSortingOrder;
        spriteRenderer.transform.localScale = Vector3.one * indicatorSize;
        
        collisionIndicators.Add(indicatorObj);
        
        if (showDebugInfo)
        {
            Debug.Log($"AimLineRenderer: 创建碰撞指示器 - 位置: {indicatorObj.transform.position}, 索引: {index}");
        }
    }
    
    /// <summary>
    /// 创建默认指示器图片（当没有设置自定义图片时）
    /// </summary>
    /// <returns>默认的白色方块Sprite</returns>
    Sprite CreateDefaultIndicatorSprite()
    {
        // 创建一个简单的白色方块作为默认指示器
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        texture.SetPixels(pixels);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }
    
    Vector3 ProjectIndicatorPosition(Vector3 source)
    {
        if (!projectIndicatorsToGround)
        {
            return source;
        }
        
        return new Vector3(source.x, groundHeight + indicatorHeightOffset, source.z);
    }
    
    void ApplyGroundFacingRotation(Transform target)
    {
        if (!flattenIndicators || target == null)
        {
            return;
        }
        
        target.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
    }
    /// <summary>
    /// 设置材质控制器引用
    /// </summary>
    public void SetMaterialController(AimLineMaterialController controller)
    {
        materialController = controller;
    }
    
    /// <summary>
    /// 设置碰撞指示器图片
    /// </summary>
    /// <param name="sprite">指示器图片</param>
    public void SetCollisionIndicatorSprite(Sprite sprite)
    {
        collisionIndicatorSprite = sprite;
    }
    
    /// <summary>
    /// 设置指示器大小
    /// </summary>
    /// <param name="size">指示器大小</param>
    public void SetIndicatorSize(float size)
    {
        indicatorSize = Mathf.Max(0.1f, size);
    }
    
    /// <summary>
    /// 设置指示器颜色
    /// </summary>
    /// <param name="color">指示器颜色</param>
    public void SetIndicatorColor(Color color)
    {
        indicatorColor = color;
    }
    
    /// <summary>
    /// 设置是否显示碰撞指示器
    /// </summary>
    /// <param name="show">是否显示</param>
    public void SetShowCollisionIndicators(bool show)
    {
        showCollisionIndicators = show;
        if (!show)
        {
            ClearCollisionIndicators();
        }
    }
    
    /// <summary>
    /// 获取渲染统计信息
    /// </summary>
    public string GetRenderStats()
    {
        return $"渲染器状态:\n" +
               $"- 分段线段数: {segmentLines.Count}\n" +
               $"- 碰撞指示器数: {collisionIndicators.Count}\n" +
               $"- 材质控制器: {(materialController != null ? "已连接" : "未连接")}\n" +
               $"- 线条宽度: {defaultLineWidth}\n" +
               $"- 排序顺序: {defaultSortingOrder}\n" +
               $"- 指示器显示: {(showCollisionIndicators ? "启用" : "禁用")}\n" +
               $"- 指示器大小: {indicatorSize}\n" +
               $"- 指示器颜色: {indicatorColor}";
    }
}
