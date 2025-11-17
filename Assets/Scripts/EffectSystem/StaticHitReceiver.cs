using UnityEngine;
using MoreMountains.Feedbacks;
using DeepSpaceLabs.SAM;

/// <summary>
/// 静态物体受击接收器（3D 几何物理版）
/// 不依赖 OnCollisionEnter，而是直接监听 GameEventBus.OnCollision。
/// 挂在任意静态物体（墙、障碍物等）上，引用一个 MMF_Player，
/// 当球（Player/Enemy）通过几何模拟撞到它或它的子碰撞体时，播放受击特效。
/// </summary>
public class StaticHitReceiver : MonoBehaviour
{
    [Header("触发来源过滤")]
    [Tooltip("是否只响应 Player 和 Enemy 的撞击")]
    public bool onlyPlayerAndEnemy = true;

    [Header("速度阈值")]
    [Tooltip("最小撞击速度，小于该值不触发特效")]
    public float minSpeed = 1f;

    [Header("冷却设置")]
    [Tooltip("同一来源再次触发的最小时间间隔")]
    public float cooldown = 0.2f;

    [Header("受击特效")]
    [Tooltip("直接拖入要播放的 MMF_Player")]
    public MMF_Player hitEffectPlayer;

    [Header("高级摇晃参数（可选）")]
    [Tooltip("如果为 true，则使用 3D 版 WallHit 计算器计算旋转角度和位置偏移，并写入 MMF_Player")]
    public bool useWallHitCalculators = true;

    [Header("3D 计算器")]
    [Tooltip("3D 旋转计算器（通常挂在 hitEffectPlayer 同一个对象上，留空则自动查找）")]
    public WallHitRotationCalculator3D rotationCalculator3D;

    [Tooltip("3D 位置偏移计算器（通常挂在 hitEffectPlayer 同一个对象上，留空则自动查找）")]
    public WallHitPositionCalculator3D positionCalculator3D;

    private float lastHitTime;

    private void OnEnable()
    {
        GameEventBus.OnCollision += OnCollisionEvent;
    }

    private void OnDisable()
    {
        GameEventBus.OnCollision -= OnCollisionEvent;
    }

    private void OnCollisionEvent(CollisionEvent evt)
    {
        if (hitEffectPlayer == null) return;
        if (evt.Source == null || evt.Target == null) return;

        // 判断 Target 是否是自己或自己的子物体
        if (evt.Target != gameObject && !evt.Target.transform.IsChildOf(transform))
        {
            return;
        }

        GameObject source = evt.Source;

        // 过滤来源
        if (onlyPlayerAndEnemy &&
            !source.CompareTag("Player") &&
            !source.CompareTag("Enemy"))
        {
            return;
        }

        // 速度阈值
        if (evt.Velocity < minSpeed)
        {
            return;
        }

        // 冷却
        float now = Time.time;
        if (now - lastHitTime < cooldown)
        {
            return;
        }
        lastHitTime = now;

        // 计算撞击点和方向（世界空间，投影到 XZ 平面）
        Vector3 hitPos3D = evt.ContactPoint3D.HasValue
            ? evt.ContactPoint3D.Value
            : new Vector3(evt.ContactPoint.x, 0f, evt.ContactPoint.y);

        Vector3 sourcePos = source.transform.position;

        // 撞击方向：优先使用 BallPhysics 的几何速度方向，其次退化为“从撞击点指向球心”
        Vector3 dir3D;
        BallPhysics ballPhysics = source.GetComponent<BallPhysics>();
        if (ballPhysics != null)
        {
            Vector2 v2 = ballPhysics.GetVelocity();
            dir3D = new Vector3(v2.x, 0f, v2.y);
        }
        else
        {
            dir3D = sourcePos - hitPos3D; // 从墙指向球
            dir3D.y = 0f;
        }

        if (dir3D.sqrMagnitude < 0.0001f)
        {
            dir3D = Vector3.forward;
        }
        dir3D.Normalize();

        Vector3 normal3D = new Vector3(evt.ContactNormal.x, 0f, evt.ContactNormal.y);
        if (normal3D.sqrMagnitude < 0.0001f)
        {
            normal3D = -dir3D; // 法线退化时使用反方向
        }
        normal3D.Normalize();

        float speed = evt.Velocity;

        if (useWallHitCalculators && hitEffectPlayer != null)
        {
            // 优先使用 3D 计算器（世界空间 → Wall 本地 XZ 由计算器内部处理）
            var rot3D = rotationCalculator3D != null
                ? rotationCalculator3D
                : hitEffectPlayer.GetComponent<WallHitRotationCalculator3D>();

            var pos3D = positionCalculator3D != null
                ? positionCalculator3D
                : hitEffectPlayer.GetComponent<WallHitPositionCalculator3D>();

            bool used3D = false;
            Transform wallRoot = transform;

            float angle3D = 0f;
            Vector3 offsetWorld = Vector3.zero;

            // 3D 旋转（只负责计算角度）
            if (rot3D != null)
            {
                used3D = true;
                // 使用法线作为力矩方向，使同一面墙 + 同一象限的符号稳定
                angle3D = rot3D.CalculateRotationAngle(wallRoot, hitPos3D, normal3D, speed);
            }

            // 3D 位移（只负责计算偏移向量）
            if (pos3D != null)
            {
                used3D = true;
                offsetWorld = pos3D.CalculatePositionOffset(wallRoot, hitPos3D, dir3D, speed);
            }

            // 统一通过 AttackData + SetWallHitParameters 传参（与 2D 方案保持一致）
            if (used3D)
            {
                AttackData attackData = new AttackData
                {
                    Position = hitPos3D,
                    Direction = dir3D,
                    Attacker = source,
                    Target = gameObject,
                    AttackTime = Time.time,
                    AttackerTag = source.tag,
                    TargetTag = gameObject.tag,
                    HitNormal = normal3D,
                    HitSpeed = speed,
                    WallHitRotationAngle = angle3D,
                    WallHitPositionOffset = offsetWorld
                };

                MMFPlayerParameterSetter.SetWallHitParameters(hitEffectPlayer, attackData);
            }
        }

        // 播放特效（等价于在 Inspector 中点击 Play）
        hitEffectPlayer.PlayFeedbacks();
    }
}


