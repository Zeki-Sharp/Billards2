using UnityEngine;

public class AttackRange : MonoBehaviour
{
    [Header("颜色设置")]
    public Color previewColor = new Color(1f, 0f, 0f, 0.3f); // 预览颜色
    public Color attackColor = new Color(1f, 0f, 0f, 0.8f);  // 攻击颜色
    
    private SpriteRenderer spriteRenderer;
    private Player targetPlayer;
    private Vector2 attackDirection; // 攻击方向（在预览阶段确定）
    private bool isPreviewActive = false; // 是否正在预览状态
    
    [Header("子对象引用")]
    public Transform imageTransform; // Image子对象的引用
    
    void Start()
    {
        // 自动查找Image子对象
        if (imageTransform == null)
        {
            imageTransform = transform.Find("Image");
            if (imageTransform == null)
            {
                Debug.LogError($"AttackRange {name}: 未找到Image子对象，请确保AttackArea下有Image子对象");
                return;
            }
        }
        
        // 从Image子对象获取SpriteRenderer组件
        spriteRenderer = imageTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"AttackRange {name}: Image子对象上未找到SpriteRenderer组件");
            return;
        }
        
        // 查找玩家
        targetPlayer = FindAnyObjectByType<Player>();
        
        // 设置初始状态
        SetVisible(false);
        
        Debug.Log($"AttackRange {name}: 初始化完成，Image子对象: {imageTransform.name}");
    }
    
    
    public void SetAttackDirection(Vector2 direction)
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 设置攻击范围的旋转
        transform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 保存攻击方向
        attackDirection = direction;
        
        Debug.Log($"AttackRange对齐: direction={direction}, angle={angle:F1}°");
    }
    
    /// <summary>
    /// 获取攻击方向（用于Enemy.cs调用）
    /// </summary>
    public Vector2 GetAttackDirection()
    {
        return attackDirection;
    }
    
    public void ShowPreview()
    {
        Debug.Log($"AttackRange {name} 开始显示预览，spriteRenderer={spriteRenderer}, isPreviewActive={isPreviewActive}");
        
        // 检查Image子对象是否存在
        if (imageTransform == null)
        {
            Debug.LogError($"AttackRange {name}: Image子对象为空，无法显示预览！");
            return;
        }
        
        SetVisible(true);
        if (spriteRenderer != null)
        {
            spriteRenderer.color = previewColor;
            Debug.Log($"AttackRange {name} 设置颜色: {previewColor}, enabled={spriteRenderer.enabled}, sprite={spriteRenderer.sprite?.name}");
        }
        else
        {
            Debug.LogError($"AttackRange {name}: spriteRenderer为空！");
        }
        
        // 只在第一次进入预览状态时更新攻击方向
        if (!isPreviewActive && targetPlayer != null)
        {
            Vector2 currentDirection = (targetPlayer.transform.position - transform.parent.position).normalized;
            attackDirection = currentDirection; // 保存新的攻击方向
            SetAttackDirection(currentDirection);
            Debug.Log($"AttackRange {name} 更新攻击方向: {currentDirection}, 玩家位置: {targetPlayer.transform.position}, 敌人位置: {transform.parent.position}");
        }
        
        isPreviewActive = true;
        Debug.Log($"AttackRange {name} 显示攻击范围预览完成");
    }
    
    public void ShowAttack()
    {
        SetVisible(true);
        spriteRenderer.color = attackColor;
        
        // 攻击阶段：使用预览阶段确定的固定方向
        SetAttackDirection(attackDirection);
        
        Debug.Log("显示攻击范围攻击状态");
        
        // 闪烁一次红色
        StartCoroutine(FlashRed());
    }
    
    public void Hide()
    {
        SetVisible(false);
        isPreviewActive = false; // 重置预览状态
        Debug.Log("隐藏攻击范围");
    }
    
    void SetVisible(bool visible)
    {
        if (spriteRenderer == null)
        {
            // 如果spriteRenderer未初始化，先初始化
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }
        spriteRenderer.enabled = visible;
    }
    
    System.Collections.IEnumerator FlashRed()
    {
        // 闪烁效果
        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        spriteRenderer.color = originalColor;
    }
    
    public bool IsPlayerInRange()
    {
        if (targetPlayer == null) return false;
        
        // 使用预制体的实际大小进行检测
        Vector2 playerPos = targetPlayer.transform.position;
        
        // 获取预制体的实际边界
        Bounds attackBounds = GetAttackRangeBounds();
        
        // 检查玩家是否在攻击范围内
        bool inRange = attackBounds.Contains(playerPos);
        
        if (inRange)
        {
            Debug.Log($"玩家在攻击范围内: 玩家位置={playerPos}, 攻击范围边界={attackBounds}");
        }
        
        return inRange;
    }
    
    Bounds GetAttackRangeBounds()
    {
        // 使用Image子对象的实际大小
        if (imageTransform != null && spriteRenderer != null && spriteRenderer.sprite != null)
        {
            // 使用Image子对象的Sprite边界
            Bounds spriteBounds = spriteRenderer.sprite.bounds;
            Vector3 center = imageTransform.position;
            Vector3 size = Vector3.Scale(spriteBounds.size, imageTransform.lossyScale);
            return new Bounds(center, size);
        }
        else if (imageTransform != null)
        {
            // 如果没有Sprite，使用Image子对象的Transform scale
            Vector3 center = imageTransform.position;
            Vector3 size = imageTransform.lossyScale;
            return new Bounds(center, size);
        }
        else
        {
            // 回退到父对象
            Vector3 center = transform.position;
            Vector3 size = transform.lossyScale;
            return new Bounds(center, size);
        }
    }
    
}
