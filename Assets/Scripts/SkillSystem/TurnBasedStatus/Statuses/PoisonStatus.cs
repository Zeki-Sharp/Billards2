using UnityEngine;

/// <summary>
/// 中毒状态 - 依据叠层造成持续伤害，每个回合衰减层数
/// </summary>
public class PoisonStatus : TurnBasedStatusComponent
{
    private int decayPerTurn = 1;

    /// <summary>
    /// 由配置注入每层伤害与衰减参数
    /// </summary>
    public void Configure(int decayPerTurnValue)
    {
        decayPerTurn = Mathf.Max(0, decayPerTurnValue);
    }

    protected override void OnTurnTrigger()
    {
        if (currentStacks <= 0)
        {
            if (showDebugLog)
            {
                Debug.Log($"[中毒] {gameObject.name} 没有叠层，跳过伤害");
            }
            return;
        }

        var damageable = GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            Debug.LogWarning($"[中毒] {gameObject.name} 及其父级没有 IDamageable 组件，无法造成伤害");
            return;
        }

        if (!damageable.CanTakeDamage())
        {
            return;
        }

        float turnDamage = Mathf.Max(0f, currentStacks);
        SetDamagePerTurn(turnDamage);

        // ✅ 3D适配：使用XZ平面投影和真实3D位置
        Vector3 pos3D = transform.position;
        
        DamageEvent damageEvent = new DamageEvent
        {
            Source = source,
            Target = gameObject,
            FinalDamage = turnDamage,
            Type = DamageType.Magical,
            TriggerType = DamageTriggerType.Interval,
            HitPosition = new Vector2(pos3D.x, pos3D.z),  // XZ平面投影（向后兼容）
            HitPosition3D = pos3D,  // ✅ 真实3D位置（用于特效定位）
            HitDirection = Vector2.zero,  // 持续伤害无方向
            VelocityAtHit = 0f,
            KnockbackForce = 0f,
            StunDuration = 0f,
            CanBeBlocked = false,
            RuleName = "PoisonDamage",
            EventTime = Time.time
        };

        damageable.OnDamageReceived(damageEvent);

        if (showDebugLog)
        {
            Debug.Log($"[中毒] ☠️ {gameObject.name} 受到中毒伤害：{turnDamage}（层数 {currentStacks}）");
        }
    }

    public int DecayPerTurn => decayPerTurn;
}


