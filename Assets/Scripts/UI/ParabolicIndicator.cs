using UnityEngine;

/// <summary>
/// 抛物线指示器
/// 用于远程攻击的视觉指示，在敌人和攻击范围之间绘制抛物线
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ParabolicIndicator : MonoBehaviour
{
    [Header("目标设置")]
    [Tooltip("起点Transform（通常是敌人）")]
    public Transform startPoint;
    
    [Tooltip("终点Transform（通常是AttackRange）")]
    public Transform endPoint;
    
    [Tooltip("使用固定起点位置（不跟随Transform移动）")]
    [SerializeField] private bool useFixedStartPoint = false;
    
    private Vector3 fixedStartPosition; // 固定的起点位置
    
    [Header("抛物线参数")]
    [Tooltip("抛物线高度（相对于起点和终点的中点）")]
    [SerializeField] private float arcHeight = 2f;
    
    [Tooltip("抛物线采样点数量（越多越平滑）")]
    [SerializeField] private int resolution = 30;
    
    [Header("视觉效果")]
    [Tooltip("抛物线颜色")]
    [SerializeField] private Color lineColor = new Color(1f, 0.5f, 0f, 0.8f);
    
    [Tooltip("线条宽度")]
    [SerializeField] private float lineWidth = 0.1f;
    
    [Tooltip("使用渐变颜色")]
    [SerializeField] private bool useGradient = true;
    
    [Tooltip("起点颜色（使用渐变时）")]
    [SerializeField] private Color startColor = new Color(1f, 0.8f, 0f, 0.8f);
    
    [Tooltip("终点颜色（使用渐变时）")]
    [SerializeField] private Color endColor = new Color(1f, 0.2f, 0f, 0.5f);
    
    [Header("动画效果")]
    [Tooltip("是否启用流动动画")]
    [SerializeField] private bool enableFlowAnimation = false;
    
    [Tooltip("流动速度")]
    [SerializeField] private float flowSpeed = 1f;
    
    // 组件引用
    private LineRenderer lineRenderer;
    private float animationTime = 0f;
    
    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        InitializeLineRenderer();
    }
    
    void Start()
    {
        // 初始隐藏
        //Hide();
    }
    
    void Update()
    {
        // 调试：每30帧输出一次状态
        if (Time.frameCount % 30 == 0 && gameObject.activeSelf)
        {
            Debug.Log($"ParabolicIndicator Update - active:{gameObject.activeSelf}, startPoint:{(startPoint != null ? startPoint.name : "null")}, endPoint:{(endPoint != null ? endPoint.name : "null")}");
        }
        
        if (startPoint != null && endPoint != null && gameObject.activeSelf)
        {
            UpdateParabolicLine();
            
            if (enableFlowAnimation)
            {
                UpdateFlowAnimation();
            }
        }
    }
    
    /// <summary>
    /// 初始化LineRenderer
    /// </summary>
    private void InitializeLineRenderer()
    {
        if (lineRenderer == null) return;
        
        lineRenderer.positionCount = resolution;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        
        // 设置材质（使用默认的LineRenderer材质）
        if (lineRenderer.material == null)
        {
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        UpdateLineColors();
    }
    
    /// <summary>
    /// 更新线条颜色
    /// </summary>
    private void UpdateLineColors()
    {
        if (lineRenderer == null) return;
        
        if (useGradient)
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(startColor, 0f), 
                    new GradientColorKey(endColor, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(startColor.a, 0f), 
                    new GradientAlphaKey(endColor.a, 1f) 
                }
            );
            lineRenderer.colorGradient = gradient;
        }
        else
        {
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
        }
    }
    
    /// <summary>
    /// 更新抛物线
    /// </summary>
    private void UpdateParabolicLine()
    {
        if (lineRenderer == null || startPoint == null || endPoint == null) return;
        
        // 直接读取两个Transform的世界坐标
        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;
        
        // 每30帧输出一次位置信息
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"ParabolicIndicator UpdateLine - 起点:{startPoint.name} at {start}, 终点:{endPoint.name} at {end}");
        }
        
        for (int i = 0; i < resolution; i++)
        {
            float t = i / (float)(resolution - 1);
            Vector3 position = CalculateParabolicPoint(start, end, t);
            lineRenderer.SetPosition(i, position);
        }
    }
    
    /// <summary>
    /// 计算抛物线上的点
    /// </summary>
    /// <param name="start">起点</param>
    /// <param name="end">终点</param>
    /// <param name="t">插值参数 (0-1)</param>
    /// <returns>抛物线上的点</returns>
    private Vector3 CalculateParabolicPoint(Vector3 start, Vector3 end, float t)
    {
        // 线性插值基础位置
        Vector3 linearPoint = Vector3.Lerp(start, end, t);
        
        // 计算抛物线高度偏移（使用二次函数）
        float heightOffset = arcHeight * (1f - 4f * (t - 0.5f) * (t - 0.5f));
        
        // 添加高度偏移（向上）
        linearPoint += Vector3.up * heightOffset;
        
        return linearPoint;
    }
    
    /// <summary>
    /// 更新流动动画
    /// </summary>
    private void UpdateFlowAnimation()
    {
        animationTime += Time.deltaTime * flowSpeed;
        
        // 可以在这里实现材质的UV偏移或其他动画效果
        if (lineRenderer.material != null)
        {
            lineRenderer.material.SetFloat("_AnimationTime", animationTime);
        }
    }
    
    /// <summary>
    /// 显示指示器
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        if (lineRenderer != null)
        {
            lineRenderer.enabled = true;
        }
    }
    
    /// <summary>
    /// 隐藏指示器
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
        
        // 清除引用
        startPoint = null;
        endPoint = null;
    }
    
    /// <summary>
    /// 重置指示器（清理引用）
    /// </summary>
    public void Reset()
    {
        Hide();
        startPoint = null;
        endPoint = null;
        useFixedStartPoint = false;
        fixedStartPosition = Vector3.zero;
    }
    
    /// <summary>
    /// 设置起点和终点
    /// </summary>
    public void SetPoints(Transform start, Transform end)
    {
        startPoint = start;
        endPoint = end;
    }
    
    /// <summary>
    /// 设置抛物线高度
    /// </summary>
    public void SetArcHeight(float height)
    {
        arcHeight = height;
    }
    
    /// <summary>
    /// 启用固定起点模式
    /// </summary>
    public void EnableFixedStartPoint(bool enable)
    {
        useFixedStartPoint = enable;
        if (enable && startPoint != null)
        {
            fixedStartPosition = startPoint.position;
        }
    }
    
    /// <summary>
    /// 设置固定起点位置
    /// </summary>
    public void SetFixedStartPosition(Vector3 position)
    {
        useFixedStartPoint = true;
        fixedStartPosition = position;
    }
    
    /// <summary>
    /// 设置线条颜色
    /// </summary>
    public void SetColor(Color color)
    {
        lineColor = color;
        useGradient = false;
        UpdateLineColors();
    }
    
    /// <summary>
    /// 设置渐变颜色
    /// </summary>
    public void SetGradientColors(Color start, Color end)
    {
        startColor = start;
        endColor = end;
        useGradient = true;
        UpdateLineColors();
    }
    
    /// <summary>
    /// 设置透明度
    /// </summary>
    public void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        
        if (useGradient)
        {
            startColor.a = alpha;
            endColor.a = alpha;
        }
        else
        {
            lineColor.a = alpha;
        }
        
        UpdateLineColors();
    }
    
    /// <summary>
    /// 设置线条宽度
    /// </summary>
    public void SetLineWidth(float width)
    {
        lineWidth = width;
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = width;
            lineRenderer.endWidth = width;
        }
    }
    
    // Gizmos调试绘制
    void OnDrawGizmos()
    {
        if (endPoint != null)
        {
            // 获取起点位置
            Vector3 start;
            if (useFixedStartPoint)
            {
                start = fixedStartPosition;
            }
            else if (startPoint != null)
            {
                start = startPoint.position;
            }
            else
            {
                return;
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(start, 0.2f);
            Gizmos.DrawWireSphere(endPoint.position, 0.2f);
            
            // 绘制简化的抛物线
            Vector3 lastPos = start;
            for (int i = 1; i <= 10; i++)
            {
                float t = i / 10f;
                Vector3 pos = CalculateParabolicPoint(start, endPoint.position, t);
                Gizmos.DrawLine(lastPos, pos);
                lastPos = pos;
            }
        }
    }
}

