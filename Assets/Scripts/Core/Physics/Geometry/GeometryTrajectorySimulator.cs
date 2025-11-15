using System.Collections.Generic;
using UnityEngine;

namespace Core.Physics.Geometry
{
    public struct GeometryTrajectoryResult
    {
        public List<Vector3> Points;
        public List<Vector3> CollisionPoints;
        public GameObject FirstHitObject;
    }
    
    /// <summary>
    /// 纯几何轨迹模拟器，可被实战（BallPhysics）与影子/瞄准线共用
    /// </summary>
    public static class GeometryTrajectorySimulator
    {
        public static GeometryTrajectoryResult Simulate(
            GeometrySimulationConfig config,
            Vector3 startPosition,
            Vector3 initialDirection,
            float initialSpeed,
            float fixedDeltaTime,
            int maxSteps,
            float sampleDistance)
        {
            var result = new GeometryTrajectoryResult
            {
                Points = new List<Vector3>(),
                CollisionPoints = new List<Vector3>(),
                FirstHitObject = null
            };
            
            Vector3 position = startPosition;
            Vector3 direction = initialDirection;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }
            direction.Normalize();
            
            float speed = Mathf.Max(0f, initialSpeed);
            float elapsedTime = 0f;
            float distanceAccumulator = 0f;
            
            result.Points.Add(position);
            
            for (int step = 0; step < maxSteps; step++)
            {
                if (speed <= config.MinSpeedThreshold)
                {
                    break;
                }
                
                Vector3 displacement = direction * speed * fixedDeltaTime;
                float distance = displacement.magnitude;
                if (distance <= 0f)
                {
                    break;
                }
                
                float sphereRadius = config.SphereRadius > 0f
                    ? config.SphereRadius
                    : 0.5f;
                
                Ray ray = new Ray(position, direction);
                
                bool hitBall = UnityEngine.Physics.SphereCast(ray, sphereRadius, out RaycastHit ballHit, distance, config.BallMask, QueryTriggerInteraction.Ignore);
                if (hitBall && IsSelfCollider(config.SourceTransform, config.SourceCollider, ballHit.collider))
                {
                    hitBall = false;
                }
                
                bool hitWall = UnityEngine.Physics.SphereCast(ray, sphereRadius, out RaycastHit wallHit, distance, config.WallMask, QueryTriggerInteraction.Ignore);
                if (hitWall && IsSelfCollider(config.SourceTransform, config.SourceCollider, wallHit.collider))
                {
                    hitWall = false;
                }
                
                if (!hitBall && !hitWall)
                {
                    position += displacement;
                    distanceAccumulator += distance;
                    if (distanceAccumulator >= sampleDistance)
                    {
                        result.Points.Add(position);
                        distanceAccumulator = 0f;
                    }
                    ApplyDamping(ref speed, ref elapsedTime, fixedDeltaTime, config);
                    continue;
                }
                
                float ballT = hitBall ? Mathf.Clamp01(ballHit.distance / distance) : float.PositiveInfinity;
                float wallT = hitWall ? Mathf.Clamp01(wallHit.distance / distance) : float.PositiveInfinity;
                
                bool resolveBall = ballT < wallT;
                RaycastHit hitInfo = resolveBall ? ballHit : wallHit;
                float travelT = Mathf.Min(ballT, wallT);
                
                float travelDistance = float.IsInfinity(travelT) ? distance : travelT * distance;
                position += direction * travelDistance;
                result.Points.Add(position);
                result.CollisionPoints.Add(position);
                if (result.FirstHitObject == null && hitInfo.collider != null)
                {
                    result.FirstHitObject = hitInfo.collider.gameObject;
                }
                
                if (resolveBall)
                {
                    HandleBallCollision(hitInfo, ref position, ref direction, ref speed, config);
                }
                else
                {
                    HandleWallCollision(hitInfo, ref direction, ref speed, config);
                }
                
                ApplyDamping(ref speed, ref elapsedTime, fixedDeltaTime * Mathf.Max(0f, 1f - travelT), config);
            }
            
            return result;
        }
        
        private static void HandleWallCollision(RaycastHit hitInfo, ref Vector3 direction, ref float speed, GeometrySimulationConfig config)
        {
            Vector3 normal = hitInfo.normal;
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = -direction;
            }
            normal.Normalize();
            
            Vector3 reflected = Vector3.Reflect(direction, normal);
            reflected.y = 0f;
            if (reflected.sqrMagnitude < 0.0001f)
            {
                reflected = -direction;
            }
            direction = reflected.normalized;
            speed *= config.WallBounceFactor;
            
            if (config.ShowDebug)
            {
                Debug.DrawRay(hitInfo.point, normal, Color.yellow, 0.2f);
                Debug.DrawRay(hitInfo.point, direction * 0.5f, config.DebugColor, 0.2f);
            }
        }
        
        private static void HandleBallCollision(RaycastHit hitInfo, ref Vector3 position, ref Vector3 direction, ref float speed, GeometrySimulationConfig config)
        {
            Collider collider = hitInfo.collider;
            if (collider == null)
            {
                HandleWallCollision(hitInfo, ref direction, ref speed, config);
                return;
            }
            
            var other = collider.GetComponentInParent<BallPhysics>();
            if (other == null || other == config.SourceTransform?.GetComponent<BallPhysics>())
            {
                HandleWallCollision(hitInfo, ref direction, ref speed, config);
                return;
            }
            
            Vector3 normal = (other.transform.position - position);
            normal.y = 0f;
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = hitInfo.normal;
                normal.y = 0f;
            }
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = -direction;
            }
            normal.Normalize();
            
            Vector3 v1 = direction * speed;
            Vector3 v2 = Vector3.zero; // 预测阶段假设其它球静止
            
            float v1n = Vector3.Dot(v1, normal);
            float v2n = Vector3.Dot(v2, normal);
            
            Vector3 v1t = v1 - v1n * normal;
            Vector3 v2t = v2 - v2n * normal;
            
            Vector3 v1After = v1t + v2n * normal;
            Vector3 v2After = v2t + v1n * normal;
            
            float otherBounce = other.GetGeometryBallBounceFactor();
            float otherKnockback = other.GetGeometryKnockbackScale();
            
            v1After *= config.BallBounceFactor * config.KnockbackScale;
            v2After *= otherBounce * otherKnockback;
            
            Vector3 newDir = v1After;
            newDir.y = 0f;
            float newSpeed = newDir.magnitude;
            if (newSpeed > 0.0001f)
            {
                direction = newDir.normalized;
                speed = newSpeed;
            }
            else
            {
                speed = 0f;
            }
            
            if (config.ShowDebug)
            {
                Debug.DrawRay(hitInfo.point, normal, Color.magenta, 0.2f);
            }
        }
        
        private static void ApplyDamping(ref float speed, ref float elapsedTime, float dt, GeometrySimulationConfig config)
        {
            if (dt <= 0f || speed <= 0f)
            {
                return;
            }
            
            elapsedTime += dt;
            float damping = elapsedTime < config.HighSpeedPhaseDuration
                ? config.HighPhaseDamping
                : config.LowPhaseDamping;
            
            speed = Mathf.Max(0f, speed - damping * dt);
        }
        
        private static bool IsSelfCollider(Transform sourceTransform, Collider sourceCollider, Collider other)
        {
            if (other == null) return true;
            if (sourceCollider != null && other == sourceCollider) return true;
            if (sourceTransform != null && other.transform == sourceTransform) return true;
            return false;
        }
    }
}

