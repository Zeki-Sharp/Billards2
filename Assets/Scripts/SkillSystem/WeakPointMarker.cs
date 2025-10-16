using UnityEngine;

/// <summary>
/// 弱点标记组件 - 显示在敌人身上的弱点UI
/// </summary>
public class WeakPointMarker : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform markerImage;
    [SerializeField] private Animator animator;
    
    private Transform enemyTransform;
    private Vector2 localOffset;
    
    void Awake()
    {
        // 自动查找组件
        if (canvas == null)
            canvas = GetComponentInChildren<Canvas>();
        
        if (markerImage == null)
        {
            var image = GetComponentInChildren<UnityEngine.UI.Image>();
            if (image != null)
                markerImage = image.GetComponent<RectTransform>();
        }
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }
    
    /// <summary>
    /// 初始化标记
    /// </summary>
    /// <param name="enemy">敌人Transform</param>
    /// <param name="offset">相对于敌人的局部坐标偏移</param>
    public void Initialize(Transform enemy, Vector2 offset)
    {
        enemyTransform = enemy;
        localOffset = offset;
        
        // 设置Canvas为World Space
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
        }
        
        // 设置为敌人子物体
        transform.SetParent(enemy, false);
        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
        
        // 播放出现动画
        if (animator != null)
        {
            animator.SetTrigger("Show");
        }
        
        Debug.Log($"[WeakPointMarker] 初始化 - 敌人: {enemy.name}, 偏移: {offset}");
    }
    
    /// <summary>
    /// 更新弱点位置
    /// </summary>
    public void UpdatePosition(Vector2 newOffset)
    {
        localOffset = newOffset;
        transform.localPosition = localOffset;
        
        // 播放刷新动画
        if (animator != null)
        {
            animator.SetTrigger("Refresh");
        }
        
        Debug.Log($"[WeakPointMarker] 刷新位置: {newOffset}");
    }
    
    /// <summary>
    /// 弱点被命中
    /// </summary>
    public void OnHit()
    {
        // 播放命中特效
        if (animator != null)
        {
            animator.SetTrigger("Hit");
        }
        
        Debug.Log($"[WeakPointMarker] 弱点命中！");
    }
    
    /// <summary>
    /// 隐藏标记
    /// </summary>
    public void Hide()
    {
        if (animator != null)
        {
            animator.SetTrigger("Hide");
            // 延迟销毁，等待动画播放完成
            Destroy(gameObject, 0.5f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Update()
    {
        // 如果敌人被销毁，自动清理标记
        if (enemyTransform == null)
        {
            Destroy(gameObject);
        }
    }
}

