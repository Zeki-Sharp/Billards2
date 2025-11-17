using System.Collections.Generic;
using Core.Physics.Geometry;
using UnityEngine;

/// <summary>
/// 3D 轨迹预测器（几何版）：与 BallPhysics 使用同一套几何模拟，保证“预测=实战”。
/// </summary>
public class TrajectoryPredictor : MonoBehaviour
{
    [Header("几何模拟参数")]
    [SerializeField] private int maxSimulationSteps = 400;
    [SerializeField] private float sampleDistance = 0.12f;
    [SerializeField] private bool showDebugLog = false;
    [SerializeField] private bool drawGizmos = false;
    
    private BallPhysics ballPhysics;
    private readonly List<Vector3> trajectoryPoints = new List<Vector3>();
    private readonly List<Vector3> collisionPoints = new List<Vector3>();
    private GeometryTrajectoryResult lastResult;
    
    private void Awake()
    {
        ballPhysics = GetComponentInParent<BallPhysics>();
        if (ballPhysics == null)
        {
            Debug.LogError("TrajectoryPredictor: 当前玩家对象缺少 BallPhysics，轨迹预测不可用。");
        }
    }
    
    public List<Vector3> PredictTrajectory(Vector3 startPosition, Vector3 initialVelocity)
    {
        trajectoryPoints.Clear();
        collisionPoints.Clear();
        lastResult = default;
        
        if (ballPhysics == null || ballPhysics.ballData == null)
        {
            trajectoryPoints.Add(startPosition);
            return new List<Vector3>(trajectoryPoints);
        }
        
        // 确保方向在 XZ 平面上（Y=0）
        Vector3 direction = new Vector3(initialVelocity.x, 0f, initialVelocity.z);
        float speed = direction.magnitude;
        if (speed <= 0.001f)
        {
            trajectoryPoints.Add(startPosition);
            return new List<Vector3>(trajectoryPoints);
        }
        direction.Normalize();
        
        GeometrySimulationConfig config = ballPhysics.CreateGeometryConfig();
        config.ShowDebug |= showDebugLog;
        
        lastResult = GeometryTrajectorySimulator.Simulate(
            config,
            startPosition,
            direction,
            speed,
            Time.fixedDeltaTime,
            maxSimulationSteps,
            Mathf.Max(0.02f, sampleDistance));
        
        trajectoryPoints.AddRange(lastResult.Points);
        collisionPoints.AddRange(lastResult.CollisionPoints);
        
        if (drawGizmos)
        {
            DrawDebug();
        }
        
        if (showDebugLog)
        {
            Debug.Log($"TrajectoryPredictor: 预测完成 -> 点数:{trajectoryPoints.Count}, 碰撞:{collisionPoints.Count}, 首击:{lastResult.FirstHitObject}");
        }
        
        return new List<Vector3>(trajectoryPoints);
    }
    
    public List<Vector3> GetCollisionPoints()
    {
        return new List<Vector3>(collisionPoints);
    }
    
#if UNITY_EDITOR
    private void DrawDebug()
    {
        if (trajectoryPoints.Count < 2) return;
        for (int i = 1; i < trajectoryPoints.Count; i++)
        {
            Debug.DrawLine(trajectoryPoints[i - 1], trajectoryPoints[i], Color.green, 0.05f);
        }
        
        foreach (var hit in collisionPoints)
        {
            Debug.DrawRay(hit, Vector3.up * 0.3f, Color.red, 0.05f);
        }
    }
#endif
}

