using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瞄准线反射计算器 - 负责计算瞄准线的反射路径
/// 与TestAimController解耦，可以独立使用
/// </summary>
public class AimLineReflectionCalculator : MonoBehaviour
{
    [Header("反射设置")]
    [SerializeField] private float maxDistance = 20f;  // 最大瞄准距离（无碰撞时）
    [SerializeField] private float reflectionLength = 10f;  // 反射后线段固定长度
    [SerializeField] private LayerMask reflectionLayers = -1;  // 可反射的层
    [SerializeField] private LayerMask ignoreLayers = 2;  // 忽略的层（Layer 1 = TransparentFX，用于攻击范围）
    [SerializeField] private float reflectionOffset = 0.01f;  // 反射点偏移，避免重复碰撞
    [SerializeField] private string ballTag = "Player";  // 球体标签，射线检测时排除
    
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = false;  // 是否启用调试日志
    [SerializeField] private bool showDebugGizmos = true;  // 是否显示调试线框
    
    // 私有变量
    private List<Vector3> currentPathPoints = new List<Vector3>();
    private Vector2 lastAimDirection = Vector2.zero;
    private Vector3 lastStartPosition = Vector3.zero;
    private bool isPathValid = false;
    
    // 事件
    public System.Action<List<Vector3>> OnPathCalculated;
    
    void OnDrawGizmos()
    {
        if (!showDebugGizmos || !isPathValid || currentPathPoints.Count < 2)
            return;
            
        // 绘制反射路径
        Gizmos.color = Color.yellow;
        for (int i = 0; i < currentPathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(currentPathPoints[i], currentPathPoints[i + 1]);
        }
        
        // 绘制反射点
        Gizmos.color = Color.red;
        for (int i = 1; i < currentPathPoints.Count - 1; i++)
        {
            Gizmos.DrawWireSphere(currentPathPoints[i], 0.1f);
        }
    }
    
    /// <summary>
    /// 计算反射路径
    /// </summary>
    /// <param name="startPos">起始位置</param>
    /// <param name="direction">瞄准方向</param>
    /// <param name="ballRadius">球体半径，用于避免击中自身</param>
    /// <returns>反射路径点列表</returns>
    public List<Vector3> CalculateReflectionPath(Vector3 startPos, Vector2 direction, float ballRadius = 0.5f)
    {
        // 检查输入有效性
        if (direction.magnitude < 0.01f)
        {
            if (enableDebugLog)
                Debug.LogWarning("AimLineReflectionCalculator: 方向向量太小，无法计算反射路径");
            return new List<Vector3> { startPos };
        }
        
        // 检查是否需要重新计算（避免不必要的计算）
        if (isPathValid && 
            Vector2.Distance(lastAimDirection, direction) < 0.01f && 
            Vector3.Distance(lastStartPosition, startPos) < 0.01f)
        {
            return new List<Vector3>(currentPathPoints);
        }
        
        // 计算新的反射路径
        List<Vector3> pathPoints = new List<Vector3>();
        pathPoints.Add(startPos);
        
        // 从白球边缘开始射线检测，避免击中白球本身
        Vector3 ballOffset = direction.normalized * ballRadius;
        Vector3 currentPos = startPos + ballOffset;
        Vector2 currentDir = direction.normalized;
        
        // 计算实际检测的层：排除忽略的层（攻击范围层）
        LayerMask actualLayers = reflectionLayers & ~ignoreLayers;
        
        // 执行第一次射线检测，排除攻击范围层
        RaycastHit2D hit = Physics2D.Raycast(currentPos, currentDir, maxDistance, actualLayers);
        
        // 如果击中了白球，继续射线检测直到找到非白球碰撞
        while (hit.collider != null && hit.collider.CompareTag(ballTag))
        {
            // 从当前碰撞点继续射线检测
            Vector3 newStartPos = (Vector3)hit.point + (Vector3)currentDir * 0.1f; // 小偏移避免重复碰撞
            hit = Physics2D.Raycast(newStartPos, currentDir, maxDistance - Vector3.Distance(currentPos, newStartPos), actualLayers);
        }
        
        if (hit.collider != null)
        {
            // 有碰撞，发生反射
            Vector3 hitPoint = hit.point;
            pathPoints.Add(hitPoint);
            
            // 计算反射方向
            Vector2 normal = hit.normal;
            Vector2 oldDir = currentDir;
            currentDir = Vector2.Reflect(currentDir, normal);
            
            // 检查反射方向是否有效
            if (currentDir.magnitude > 0.01f)
            {
                // 从碰撞点开始，检查反射后是否还有第二次碰撞
                Vector3 reflectionStartPos = hitPoint + (Vector3)currentDir * reflectionOffset;
                RaycastHit2D secondHit = Physics2D.Raycast(reflectionStartPos, currentDir, reflectionLength, actualLayers);
                
                if (secondHit.collider != null)
                {
                    // 有第二次碰撞，直接到第二次碰撞位置结束
                    Vector3 secondHitPoint = secondHit.point;
                    pathPoints.Add(secondHitPoint);
                }
                else
                {
                    // 没有第二次碰撞，延伸到固定长度
                    Vector3 reflectionEndPoint = hitPoint + (Vector3)currentDir * reflectionLength;
                    pathPoints.Add(reflectionEndPoint);
                }
            }
        }
        else
        {
            // 没有碰撞，直接延伸到最大距离
            Vector3 endPoint = currentPos + (Vector3)currentDir * maxDistance;
            pathPoints.Add(endPoint);
            
        }
        
        // 更新缓存
        currentPathPoints = new List<Vector3>(pathPoints);
        lastAimDirection = direction;
        lastStartPosition = startPos;
        isPathValid = true;
        
        // 触发事件
        OnPathCalculated?.Invoke(new List<Vector3>(pathPoints));
        
        return pathPoints;
    }
    
    /// <summary>
    /// 获取当前缓存的路径点
    /// </summary>
    /// <returns>当前路径点列表</returns>
    public List<Vector3> GetCurrentPath()
    {
        return new List<Vector3>(currentPathPoints);
    }
    
    /// <summary>
    /// 检查路径是否有效
    /// </summary>
    /// <returns>路径是否有效</returns>
    public bool IsPathValid()
    {
        return isPathValid;
    }
    
    /// <summary>
    /// 清除当前路径缓存
    /// </summary>
    public void ClearPath()
    {
        currentPathPoints.Clear();
        isPathValid = false;
        lastAimDirection = Vector2.zero;
        lastStartPosition = Vector3.zero;
    }
    
    /// <summary>
    /// 设置反射参数
    /// </summary>
    /// <param name="maxDistance">最大距离（无碰撞时）</param>
    /// <param name="reflectionLength">反射后线段固定长度</param>
    /// <param name="reflectionLayers">反射层</param>
    /// <param name="ballTag">球体标签（射线检测时排除）</param>
    public void SetReflectionSettings(float maxDistance, float reflectionLength, LayerMask reflectionLayers, string ballTag)
    {
        this.maxDistance = maxDistance;
        this.reflectionLength = reflectionLength;
        this.reflectionLayers = reflectionLayers;
        this.ballTag = ballTag;
        
        // 清除缓存，强制重新计算
        ClearPath();
    }
    
    
    /// <summary>
    /// 获取反射统计信息
    /// </summary>
    /// <returns>反射统计信息</returns>
    public string GetReflectionStats()
    {
        if (!isPathValid)
            return "无有效路径";
            
        bool hasReflection = currentPathPoints.Count >= 3; // 起点 + 碰撞点 + 反射终点
        bool hasSecondHit = currentPathPoints.Count >= 4; // 起点 + 碰撞点 + 第二次碰撞点
        float totalDistance = 0f;
        
        for (int i = 0; i < currentPathPoints.Count - 1; i++)
        {
            totalDistance += Vector3.Distance(currentPathPoints[i], currentPathPoints[i + 1]);
        }
        
        string reflectionInfo;
        if (hasSecondHit)
            reflectionInfo = "有反射+第二次碰撞";
        else if (hasReflection)
            reflectionInfo = "有反射";
        else
            reflectionInfo = "无反射";
            
        return $"{reflectionInfo}, 总距离: {totalDistance:F2}, 路径点数: {currentPathPoints.Count}";
    }
}
