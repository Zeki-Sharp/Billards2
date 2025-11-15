using UnityEngine;

namespace Core.Physics.Geometry
{
    /// <summary>
    /// 几何模拟配置（由 BallData + 组件配置组合而成）
    /// </summary>
    public struct GeometrySimulationConfig
    {
        public float MinSpeedThreshold;
        public float HighSpeedPhaseDuration;
        public float HighPhaseDamping;
        public float LowPhaseDamping;
        public float WallBounceFactor;
        public float BallBounceFactor;
        public float KnockbackScale;
        public float SphereRadius;
        public LayerMask WallMask;
        public LayerMask BallMask;
        public bool ShowDebug;
        public Color DebugColor;
        public Transform SourceTransform;
        public Collider SourceCollider;
    }
}

