using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// 轨迹预测器 - 使用物理模拟预测球的运动轨迹
/// 
/// 【核心职责】：
/// - 在影子场景中执行物理模拟
/// - 记录轨迹点
/// - 检测碰撞
/// - 判断停止条件
/// 
/// 【设计原则】：
/// - 使用真实Unity物理引擎
/// - 与BallPhysics协同工作
/// - 输出标准List<Vector3>格式（兼容现有渲染系统）
/// 
/// 【执行顺序】：COMPONENT (100)，确保 SimulationManager 先初始化
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.COMPONENT)]
public class TrajectoryPredictor : MonoBehaviour
{
    #region 配置
    
    [Header("模拟参数")]
    [Tooltip("最大模拟步数")]
    [SerializeField] private int maxSimulationSteps = 500;
    
    [Tooltip("轨迹点采样间隔（米）")]
    [SerializeField] private float sampleDistance = 0.1f;
    
    [Tooltip("最大轨迹点数")]
    [SerializeField] private int maxTrajectoryPoints = 200;
    
    // ✅ 多角色系统改造：不再需要手动配置引用
    // [Header("组件引用")]
    // [Tooltip("场景管理器")]
    // [SerializeField] private TrajectorySimulationManager simulationManager;
    
    // [Tooltip("对象复制器")]
    // [SerializeField] private SimulationObjectReplicator objectReplicator;
    
    [Header("调试")]
    [Tooltip("是否显示调试日志")]
    [SerializeField] private bool showDebugLog = true;
    
    [Tooltip("是否显示详细步进日志")]
    [SerializeField] private bool showStepLog = false;
    
    #endregion
    
    #region 私有字段
    
    // 物理场景引用
    private PhysicsScene2D simulationPhysicsScene;
    
    // 模拟球引用（影子场景中的玩家球）
    private GameObject simulatedPlayerBall;
    private Rigidbody2D simulatedRb;
    private BallPhysics simulatedBallPhysics;
    
    // 轨迹数据
    private List<Vector3> trajectoryPoints = new List<Vector3>();
    private List<Vector3> collisionPoints = new List<Vector3>();
    
    // ✅ 多角色系统改造：通过 Instance 获取场景管理器
    private TrajectorySimulationManager simulationManager;
    
    #endregion
    
    #region Unity生命周期
    
    // ✅ 移除 Awake，改为延迟初始化
    // 避免在 Awake 中访问 Instance 导致自动创建问题
    
    /// <summary>
    /// 确保 SimulationManager 已初始化
    /// </summary>
    void EnsureSimulationManager()
    {
        if (simulationManager == null)
        {
            simulationManager = TrajectorySimulationManager.Instance;
            
            if (simulationManager == null)
            {
                Debug.LogError("TrajectoryPredictor: 无法获取 TrajectorySimulationManager 单例！");
            }
            else
            {
                if (showDebugLog)
                {
                    Debug.Log($"TrajectoryPredictor [{transform.parent?.name}]: 已连接到 TrajectorySimulationManager");
                    Debug.Log($"  场景有效性: {simulationManager.IsSceneValid()}");
                }
                
                // ✅ 如果场景无效，尝试创建
                if (!simulationManager.IsSceneValid())
                {
                    Debug.LogWarning($"TrajectoryPredictor [{transform.parent?.name}]: 影子场景未创建，触发创建...");
                    simulationManager.CreateSimulationScene();
                    
                    // 创建后需要复制对象
                    simulationManager.UpdateDynamicObjects();
                }
            }
        }
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 预测轨迹
    /// </summary>
    /// <param name="startPosition">起始位置</param>
    /// <param name="initialVelocity">初始速度</param>
    /// <returns>轨迹点列表</returns>
    public List<Vector3> PredictTrajectory(Vector3 startPosition, Vector2 initialVelocity)
    {
        // ✅ 延迟初始化：第一次调用时才获取 SimulationManager
        EnsureSimulationManager();
        
        if (showDebugLog)
        {
            Debug.Log($"TrajectoryPredictor: 开始预测轨迹 - 起点:{startPosition}, 速度:{initialVelocity}");
        }
        
        // 准备模拟环境
        if (!PrepareSimulation())
        {
            Debug.LogError("TrajectoryPredictor: 模拟环境准备失败！");
            return new List<Vector3>();
        }
        
        // 设置模拟球的初始状态
        SetupSimulatedBall(startPosition, initialVelocity);
        
        // 执行物理模拟
        List<Vector3> result = SimulatePhysics();
        
        if (showDebugLog)
        {
            Debug.Log($"TrajectoryPredictor: 预测完成 - 轨迹点:{result.Count}, 碰撞:{collisionPoints.Count}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取碰撞点列表
    /// </summary>
    public List<Vector3> GetCollisionPoints()
    {
        return new List<Vector3>(collisionPoints);
    }
    
    #endregion
    
    #region 私有方法 - 模拟准备
    
    /// <summary>
    /// 准备模拟环境
    /// </summary>
    private bool PrepareSimulation()
    {
        // 确保影子场景存在
        if (simulationManager == null)
        {
            Debug.LogError("TrajectoryPredictor: SimulationManager 为 null！");
            return false;
        }
        
        if (!simulationManager.IsSceneValid())
        {
            Debug.LogError($"TrajectoryPredictor: 影子场景无效！isSceneCreated={simulationManager != null}");
            Debug.LogError($"  场景信息: {(simulationManager != null ? simulationManager.GetSceneInfo() : "Manager为null")}");
            return false;
        }
        
        // 获取物理场景引用
        simulationPhysicsScene = simulationManager.GetPhysicsScene();
        if (!simulationPhysicsScene.IsValid())
        {
            Debug.LogError("TrajectoryPredictor: 物理场景无效！");
            return false;
        }
        
        // ✅ 多角色系统改造：预测前先更新影子场景中的动态对象（玩家、敌人）
        // 因为玩家球体是在角色选择后才生成的，必须先复制到影子场景
        simulationManager.UpdateDynamicObjects();
        
        if (showDebugLog)
        {
            Debug.Log($"TrajectoryPredictor [{transform.parent?.name}]: 已更新影子场景动态对象");
        }
        
        Scene simScene = simulationManager.GetSimulationScene();
        
        // ✅ 多角色系统改造：通过名字精确匹配找到对应的模拟球
        GameObject[] rootObjects = simScene.GetRootGameObjects();
        simulatedPlayerBall = null;
        
        // 获取父物体名字（当前正在操作的球）
        string parentName = transform.parent != null ? transform.parent.name : "";
        string targetSimName = "Sim_" + parentName;  // 复制后的名字格式
        
        foreach (GameObject obj in rootObjects)
        {
            if (obj.name == targetSimName)
            {
                simulatedPlayerBall = obj;
                
                if (showDebugLog)
                {
                    Debug.Log($"TrajectoryPredictor [{parentName}]: ✅ 找到对应的模拟球 {obj.name}");
                }
                break;
            }
        }
        
        // 🔍 调试：如果没找到，列出所有 Player 对象
        if (simulatedPlayerBall == null && showDebugLog)
        {
            int playerCount = 0;
            Debug.LogWarning($"TrajectoryPredictor [{parentName}]: ❌ 未找到匹配的模拟球！目标名字: {targetSimName}");
            Debug.LogWarning("  影子场景中的 Player 对象：");
            foreach (GameObject obj in rootObjects)
            {
                if (obj.name.Contains("Player"))
                {
                    Debug.LogWarning($"    - {obj.name}");
                    playerCount++;
                }
            }
            Debug.LogWarning($"  共 {playerCount} 个 Player 对象");
        }
        
        if (simulatedPlayerBall == null)
        {
            Debug.LogError("TrajectoryPredictor: 找不到模拟球（Player）！");
            return false;
        }
        
        // 获取组件引用
        simulatedRb = simulatedPlayerBall.GetComponent<Rigidbody2D>();
        simulatedBallPhysics = simulatedPlayerBall.GetComponent<BallPhysics>();
        
        if (simulatedRb == null)
        {
            Debug.LogError("TrajectoryPredictor: 模拟球没有 Rigidbody2D！");
            return false;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"TrajectoryPredictor: 模拟环境准备完成 - 球:{simulatedPlayerBall.name}");
            
            // 🔍 诊断：检查场景中所有对象
            Debug.Log($"  【场景诊断】模拟场景中共有{rootObjects.Length}个根对象:");
            foreach (GameObject obj in rootObjects)
            {
                Collider2D col = obj.GetComponent<Collider2D>();
                string colliderInfo = col != null ? $"Collider2D:{col.GetType().Name}, Enabled:{col.enabled}" : "无Collider";
                Debug.Log($"    - {obj.name} (Tag:{obj.tag}) {colliderInfo}");
            }
            
            // 🔍 诊断：检查模拟球的collider
            Collider2D playerCol = simulatedPlayerBall.GetComponent<Collider2D>();
            if (playerCol != null)
            {
                Debug.Log($"  【球体诊断】模拟球Collider: {playerCol.GetType().Name}, Enabled:{playerCol.enabled}");
            }
            else
            {
                Debug.LogWarning("  【球体诊断】模拟球没有Collider2D！");
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// 设置模拟球的初始状态
    /// </summary>
    private void SetupSimulatedBall(Vector3 position, Vector2 velocity)
    {
        // 设置位置
        simulatedPlayerBall.transform.position = position;
        
        // 设置速度
        simulatedRb.linearVelocity = velocity;
        simulatedRb.angularVelocity = 0f;
        
        // 如果有BallPhysics，初始化模拟状态
        if (simulatedBallPhysics != null && simulatedBallPhysics.isSimulationMode)
        {
            simulatedBallPhysics.InitializeSimulationState();
        }
        
        if (showDebugLog)
        {
            Debug.Log($"TrajectoryPredictor: 模拟球初始化 - 位置:{position}, 速度:{velocity}");
            Debug.Log($"  【位置确认】当前transform.position: {simulatedPlayerBall.transform.position}");
            
            // 🔍 诊断：列出敌人和墙壁的位置
            Scene simScene = simulationManager.GetSimulationScene();
            GameObject[] allObjs = simScene.GetRootGameObjects();
            foreach (GameObject obj in allObjs)
            {
                if (obj.tag == "Enemy" || obj.tag == "Wall")
                {
                    Vector3 objPos = obj.transform.position;
                    float distance = Vector3.Distance(position, objPos);
                    Debug.Log($"  【对象位置】{obj.name} (Tag:{obj.tag}) at {objPos}, 距离起点: {distance:F2}m");
                }
            }
        }
    }
    
    #endregion
    
    #region 私有方法 - 物理模拟
    
    /// <summary>
    /// 执行物理模拟并记录轨迹
    /// </summary>
    private List<Vector3> SimulatePhysics()
    {
        trajectoryPoints.Clear();
        collisionPoints.Clear();
        
        float simulationTime = 0f;
        Vector2 lastVelocity = simulatedRb.linearVelocity;
        Vector3 lastSamplePosition = simulatedPlayerBall.transform.position;
        float distanceSinceLastSample = 0f;
        
        // 记录起始点
        trajectoryPoints.Add(simulatedPlayerBall.transform.position);
        
        // 获取停止阈值
        float stopThreshold = 0.01f;
        if (simulatedBallPhysics != null && simulatedBallPhysics.ballData != null)
        {
            stopThreshold = simulatedBallPhysics.ballData.stopThreshold;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"TrajectoryPredictor: 开始模拟循环 - 最大步数:{maxSimulationSteps}, 停止阈值:{stopThreshold}");
        }
        
        // 模拟循环
        for (int step = 0; step < maxSimulationSteps; step++)
        {
            // 执行物理模拟（一个固定时间步）
            simulationPhysicsScene.Simulate(Time.fixedDeltaTime);
            simulationTime += Time.fixedDeltaTime;
            
            // 如果有BallPhysics，手动更新动态物理参数
            if (simulatedBallPhysics != null && simulatedBallPhysics.isSimulationMode)
            {
                simulatedBallPhysics.ManualPhysicsUpdate(simulationTime);
            }
            
            // 获取当前状态
            Vector3 currentPosition = simulatedPlayerBall.transform.position;
            Vector2 currentVelocity = simulatedRb.linearVelocity;
            float currentSpeed = currentVelocity.magnitude;
            
            // 检测碰撞（速度方向突变）
            if (lastVelocity.sqrMagnitude > 0.01f)
            {
                float velocityChange = Vector2.Angle(lastVelocity, currentVelocity);
                if (velocityChange > 5f) // 角度变化超过5度认为发生了碰撞
                {
                    collisionPoints.Add(currentPosition);
                }
            }
            
            // 采样轨迹点（按距离采样）
            float distanceFromLast = Vector3.Distance(currentPosition, lastSamplePosition);
            distanceSinceLastSample += distanceFromLast;
            
            if (distanceSinceLastSample >= sampleDistance)
            {
                trajectoryPoints.Add(currentPosition);
                lastSamplePosition = currentPosition;
                distanceSinceLastSample = 0f;
                
                // 检查是否超过最大点数
                if (trajectoryPoints.Count >= maxTrajectoryPoints)
                {
                    if (showDebugLog)
                    {
                        Debug.Log($"TrajectoryPredictor: 达到最大轨迹点数 {maxTrajectoryPoints}");
                    }
                    break;
                }
            }
            
            // 详细步进日志
            if (showStepLog && step % 10 == 0)
            {
                Debug.Log($"  [步骤{step}] 位置:{currentPosition}, 速度:{currentSpeed:F2}");
            }
            
            // 🔍 重叠检测（每10步检测一次）
            if (step % 10 == 0 && simulatedRb != null)
            {
                Collider2D playerCollider = simulatedPlayerBall.GetComponent<Collider2D>();
                if (playerCollider != null)
                {
                    Collider2D[] overlaps = new Collider2D[10];
                    ContactFilter2D filter = new ContactFilter2D();
                    filter.useTriggers = false;
                    
                    int count = playerCollider.Overlap(filter, overlaps);
                    if (count > 0)
                    {
                        Debug.Log($"  [步骤{step}] 🔍 检测到{count}个重叠Collider:");
                        for (int i = 0; i < count; i++)
                        {
                            if (overlaps[i] != null)
                            {
                                Debug.Log($"      - {overlaps[i].gameObject.name} (Tag:{overlaps[i].tag})");
                            }
                        }
                    }
                }
            }
            
            // 检查停止条件（使用BallData的stopThreshold）
            if (currentSpeed < stopThreshold)
            {
                // 添加最终位置
                if (Vector3.Distance(trajectoryPoints[trajectoryPoints.Count - 1], currentPosition) > 0.01f)
                {
                    trajectoryPoints.Add(currentPosition);
                }
                
                if (showDebugLog)
                {
                    Debug.Log($"TrajectoryPredictor: 球停止 - 步骤:{step}, 时间:{simulationTime:F2}s, 最终速度:{currentSpeed:F3}");
                }
                break;
            }
            
            // 更新上一帧速度
            lastVelocity = currentVelocity;
        }
        
        return trajectoryPoints;
    }
    
    #endregion
    
    #region Inspector测试方法
    
    /// <summary>
    /// [测试] 预测直线轨迹
    /// </summary>
    [ContextMenu("测试/预测直线轨迹")]
    private void Test_PredictStraightLine()
    {
        Debug.Log("========== 测试：预测直线轨迹 ==========");
        
        Vector3 startPos = Vector3.zero;
        Vector2 velocity = new Vector2(5f, 0f);
        
        List<Vector3> trajectory = PredictTrajectory(startPos, velocity);
        
        Debug.Log($"轨迹点数: {trajectory.Count}");
        if (trajectory.Count > 0)
        {
            Debug.Log($"  起点: {trajectory[0]}");
            Debug.Log($"  终点: {trajectory[trajectory.Count - 1]}");
            Debug.Log($"  总距离: {CalculatePathLength(trajectory):F2}m");
        }
        
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// [测试] 预测45度轨迹
    /// </summary>
    [ContextMenu("测试/预测45度轨迹")]
    private void Test_Predict45Degree()
    {
        Debug.Log("========== 测试：预测45度轨迹 ==========");
        
        Vector3 startPos = Vector3.zero;
        Vector2 velocity = new Vector2(5f, 5f);
        
        List<Vector3> trajectory = PredictTrajectory(startPos, velocity);
        
        Debug.Log($"轨迹点数: {trajectory.Count}");
        Debug.Log($"碰撞点数: {collisionPoints.Count}");
        
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// [测试] 预测低速轨迹
    /// </summary>
    [ContextMenu("测试/预测低速轨迹")]
    private void Test_PredictLowSpeed()
    {
        Debug.Log("========== 测试：预测低速轨迹 ==========");
        
        Vector3 startPos = Vector3.zero;
        Vector2 velocity = new Vector2(2f, 0f);
        
        List<Vector3> trajectory = PredictTrajectory(startPos, velocity);
        
        Debug.Log($"轨迹点数: {trajectory.Count}");
        Debug.Log($"总距离: {CalculatePathLength(trajectory):F2}m");
        
        Debug.Log("======================================");
    }
    
    /// <summary>
    /// 计算路径总长度
    /// </summary>
    private float CalculatePathLength(List<Vector3> path)
    {
        float length = 0f;
        for (int i = 1; i < path.Count; i++)
        {
            length += Vector3.Distance(path[i - 1], path[i]);
        }
        return length;
    }
    
    #endregion
}

