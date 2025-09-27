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
    
    [Header("组件引用")]
    [SerializeField] private AimLineMaterialController materialController;
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugInfo = false;
    
    // 渲染对象管理
    private LineRenderer mainAimLine;
    private List<LineRenderer> segmentLines = new List<LineRenderer>();
    private GameObject lineContainer;
    
    // 常量定义
    private const float MIN_ANGLE_THRESHOLD = 0.01f;
    private const float BACKOFF_MULTIPLIER = 0.5f;
    
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
    }
    
    /// <summary>
    /// 渲染简单瞄准线（无反射）
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="endPos">结束位置</param>
    public void RenderSimpleAimLine(Vector3 startPos, Vector3 endPos)
    {
        // 清除分段线段
        ClearSegmentLines();
        
        // 创建或更新主瞄准线
        if (mainAimLine == null)
        {
            mainAimLine = CreateMainAimLine();
        }
        
        // 设置线条位置
        mainAimLine.positionCount = 2;
        mainAimLine.SetPosition(0, startPos);
        mainAimLine.SetPosition(1, endPos);
        
        // 应用材质效果
        if (materialController != null)
        {
            float segmentLength = Vector3.Distance(startPos, endPos);
            materialController.UpdateSegmentMaterial(mainAimLine, segmentLength, true);
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"AimLineRenderer: 渲染简单瞄准线 - 起点: {startPos}, 终点: {endPos}");
        }
    }
    
    /// <summary>
    /// 渲染分段反射瞄准线
    /// </summary>
    /// <param name="pathPoints">路径点列表</param>
    public void RenderSegmentedAimLine(List<Vector3> pathPoints)
    {
        if (pathPoints == null || pathPoints.Count <= 1)
        {
            ClearAllLines();
            return;
        }
        
        // 隐藏主瞄准线
        if (mainAimLine != null)
        {
            mainAimLine.positionCount = 0;
        }
        
        // 清除旧的分段线段
        ClearSegmentLines();
        
        // 获取线条宽度
        float lineWidth = defaultLineWidth;
        if (mainAimLine != null)
        {
            lineWidth = mainAimLine.startWidth;
        }
        
        // 根据路径点数创建分段
        int segmentCount = pathPoints.Count - 1;
        
        for (int i = 0; i < segmentCount; i++)
        {
            // 创建分段线段
            LineRenderer segmentLine = CreateSegmentLine(i);
            segmentLines.Add(segmentLine);
            
            // 计算起点和终点
            Vector3 startPoint = pathPoints[i];
            Vector3 endPoint = pathPoints[i + 1];
            
            // 计算回退（避免线段重叠）
            if (i > 0)
            {
                Vector3 prevPoint = pathPoints[i - 1];
                float backoff = CalculateBackoff(prevPoint, pathPoints[i], endPoint, lineWidth);
                Vector3 direction = (pathPoints[i] - prevPoint).normalized;
                startPoint = pathPoints[i] - direction * backoff;
            }
            
            if (i < segmentCount - 1)
            {
                Vector3 nextPoint = pathPoints[i + 2];
                float backoff = CalculateBackoff(pathPoints[i], pathPoints[i + 1], nextPoint, lineWidth);
                Vector3 direction = (pathPoints[i + 1] - pathPoints[i]).normalized;
                endPoint = pathPoints[i + 1] - direction * backoff;
            }
            
            // 设置分段线段的起点和终点
            segmentLine.SetPosition(0, startPoint);
            segmentLine.SetPosition(1, endPoint);
            
            // 应用材质效果
            if (materialController != null)
            {
                float segmentLength = Vector3.Distance(startPoint, endPoint);
                bool isLastSegment = (i == segmentCount - 1);
                materialController.UpdateSegmentMaterial(segmentLine, segmentLength, isLastSegment);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"AimLineRenderer: 渲染分段瞄准线 - 段数: {segmentCount}, 路径点: {pathPoints.Count}");
        }
    }
    
    /// <summary>
    /// 清除所有瞄准线
    /// </summary>
    public void ClearAllLines()
    {
        // 清除主瞄准线
        if (mainAimLine != null)
        {
            mainAimLine.positionCount = 0;
        }
        
        // 清除分段线段
        ClearSegmentLines();
    }
    
    /// <summary>
    /// 创建主瞄准线
    /// </summary>
    LineRenderer CreateMainAimLine()
    {
        GameObject lineObj = new GameObject("MainAimLine");
        lineObj.transform.SetParent(lineContainer.transform);
        
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        
        // 设置基础属性
        SetupLineRenderer(lineRenderer);
        
        return lineRenderer;
    }
    
    /// <summary>
    /// 创建分段线段
    /// </summary>
    LineRenderer CreateSegmentLine(int segmentIndex)
    {
        GameObject segmentObj = new GameObject($"AimLineSegment_{segmentIndex}");
        segmentObj.transform.SetParent(lineContainer.transform);
        
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
        lineRenderer.alignment = LineAlignment.TransformZ;
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
    /// 计算端点回退距离
    /// </summary>
    float CalculateBackoff(Vector3 point1, Vector3 point2, Vector3 point3, float lineWidth)
    {
        // 计算两条线段的方向向量
        Vector3 dir1 = (point2 - point1).normalized;
        Vector3 dir2 = (point3 - point2).normalized;
        
        // 计算夹角（弧度）
        float angle = Vector3.Angle(dir1, dir2) * Mathf.Deg2Rad;
        
        // 避免除零错误
        if (angle < MIN_ANGLE_THRESHOLD)
        {
            return 0f;
        }
        
        // 使用公式：backoff = 0.5 * width / tan(angle/2)
        float backoff = BACKOFF_MULTIPLIER * lineWidth / Mathf.Tan(angle * BACKOFF_MULTIPLIER);
        
        return backoff;
    }
    
    /// <summary>
    /// 设置材质控制器引用
    /// </summary>
    public void SetMaterialController(AimLineMaterialController controller)
    {
        materialController = controller;
    }
    
    /// <summary>
    /// 获取渲染统计信息
    /// </summary>
    public string GetRenderStats()
    {
        return $"渲染器状态:\n" +
               $"- 主瞄准线: {(mainAimLine != null ? "已创建" : "未创建")}\n" +
               $"- 分段线段数: {segmentLines.Count}\n" +
               $"- 材质控制器: {(materialController != null ? "已连接" : "未连接")}\n" +
               $"- 线条宽度: {defaultLineWidth}\n" +
               $"- 排序顺序: {defaultSortingOrder}";
    }
}
