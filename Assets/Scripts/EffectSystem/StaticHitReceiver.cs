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
    [Tooltip("如果为 true，则使用 WallHit 计算器计算旋转角度和位置偏移，并写入 MMF_Player")]
    public bool useWallHitCalculators = true;

    [Tooltip("旋转计算器（通常挂在 hitEffectPlayer 同一个对象上，留空则自动查找）")]
    public WallHitRotationController rotationController;

    [Tooltip("位置偏移计算器（通常挂在 hitEffectPlayer 同一个对象上，留空则自动查找）")]
    public WallHitPositionController positionController;

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

        Vector3 dir3D = sourcePos - hitPos3D; // 从墙指向球
        dir3D.y = 0f;
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

        // 将世界 XZ 投影到计算器的 XY 平面（逻辑保持一致，坐标系从 XY 换成 XZ）
        Vector3 calcPos = new Vector3(hitPos3D.x, hitPos3D.z, 0f);
        Vector3 calcNormal = new Vector3(normal3D.x, normal3D.z, 0f);
        Vector3 calcDir = new Vector3(dir3D.x, dir3D.z, 0f);

        float speed = evt.Velocity;

        if (useWallHitCalculators && hitEffectPlayer != null)
        {
            var rotCtrl = rotationController != null
                ? rotationController
                : hitEffectPlayer.GetComponent<WallHitRotationController>();

            var posCtrl = positionController != null
                ? positionController
                : hitEffectPlayer.GetComponent<WallHitPositionController>();

            // 旋转角度
            if (rotCtrl != null)
            {
                float angle = rotCtrl.CalculateRotationAngle(calcPos, calcNormal, speed);
                MMFPlayerParameterSetter.SetRotationEffect(hitEffectPlayer, angle);
            }

            // 位置偏移（计算器返回的是 XY，转换回 XZ）
            if (posCtrl != null)
            {
                Vector3 offset2D = posCtrl.CalculatePositionOffset(calcPos, calcNormal, calcDir, speed);
                Vector3 offset3D = new Vector3(offset2D.x, 0f, offset2D.y);
                MMFPlayerParameterSetter.SetPositionSpringEffect(hitEffectPlayer, offset3D);
            }
        }

        // 播放特效（等价于在 Inspector 中点击 Play）
        hitEffectPlayer.PlayFeedbacks();
    }
}


