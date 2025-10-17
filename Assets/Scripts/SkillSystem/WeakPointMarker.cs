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
    private int currentSector;  // 当前弱点扇区
    
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
    /// <param name="sector">弱点扇区（0=上, 1=右, 2=下, 3=左）</param>
    public void Initialize(Transform enemy, Vector2 offset, int sector)
    {
        enemyTransform = enemy;
        localOffset = offset;
        currentSector = sector;
        
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
        
        // 根据扇区设置箭头旋转
        UpdateArrowRotation();
        
        // 播放出现动画
        if (animator != null)
        {
            animator.SetTrigger("Show");
        }
        
        Debug.Log($"[WeakPointMarker] 初始化 - 敌人: {enemy.name}, 偏移: {offset}, 扇区: {sector}");
    }
    
    /// <summary>
    /// 更新弱点位置
    /// </summary>
    /// <param name="newOffset">新的局部坐标偏移</param>
    /// <param name="newSector">新的弱点扇区</param>
    public void UpdatePosition(Vector2 newOffset, int newSector)
    {
        localOffset = newOffset;
        currentSector = newSector;
        transform.localPosition = localOffset;
        
        // 根据扇区设置箭头旋转
        UpdateArrowRotation();
        
        // 播放刷新动画
        if (animator != null)
        {
            animator.SetTrigger("Refresh");
        }
        
        Debug.Log($"[WeakPointMarker] 刷新位置: {newOffset}, 扇区: {newSector}");
    }
    
    /// <summary>
    /// 根据扇区更新箭头旋转
    /// 扇区0=-90度，扇区1=0度，扇区2=90度，扇区3=180度
    /// </summary>
    private void UpdateArrowRotation()
    {
        if (markerImage == null)
            return;
        
        // 根据扇区计算旋转角度
        float rotationAngle = (currentSector - 1) * 90f;
        
        // 设置箭头旋转
        markerImage.localRotation = Quaternion.Euler(0, 0, rotationAngle);
        
        Debug.Log($"[WeakPointMarker] 扇区: {currentSector}, 箭头旋转: {rotationAngle}°");
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

