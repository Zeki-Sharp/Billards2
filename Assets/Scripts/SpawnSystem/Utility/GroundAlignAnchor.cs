using UnityEngine;

/// <summary>
/// 脚底对齐锚点：用于在生成/摆放阶段将物体脚底贴合地面
/// </summary>
[DisallowMultipleComponent]
public class GroundAlignAnchor : MonoBehaviour
{
    [Header("脚底配置")]
    [Tooltip("脚底参考点（通常是子物体）。为空则使用自身 Transform。")]
    [SerializeField] private Transform footPoint;
    
    [Tooltip("向下射线长度")]
    [SerializeField] private float raycastDistance = 2f;
    
    [Tooltip("检测地面的 Layer")]
    [SerializeField] private LayerMask groundLayers = ~0;
    
    [Tooltip("额外的 Y 轴偏移（正值向上）")]
    [SerializeField] private float additionalYOffset = 0f;
    
    [Header("执行设置")]
    [Tooltip("Awake 时自动对齐")]
    [SerializeField] private bool alignOnAwake = true;
    
    [Tooltip("在编辑器修改属性时自动对齐（仅编辑器）")]
    [SerializeField] private bool alignInEditor = false;
    
    [Tooltip("打印调试日志")]
    [SerializeField] private bool enableDebugLog = false;
    
    private const float RaycastLift = 0.05f;
    private Collider cachedCollider;
    
    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        if (alignOnAwake)
        {
            AlignToGround(enableDebugLog);
        }
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying && alignInEditor)
        {
            AlignToGround(false);
        }
    }
#endif
    
    /// <summary>
    /// 主动执行脚底对齐
    /// </summary>
    [ContextMenu("Align Foot To Ground")]
    public bool AlignToGround() => AlignToGround(true);
    
    public bool AlignToGround(bool logResult)
    {
        if (!TryGetFootWorldPosition(out Vector3 footWorldPosition))
        {
            if (logResult || enableDebugLog)
            {
                Debug.LogWarning($"[GroundAlignAnchor] {name} 未能确定脚底位置（未设置 Foot Point 且无 Collider）", this);
            }
            return false;
        }
        
        Vector3 rayOrigin = footWorldPosition + Vector3.up * RaycastLift;
        
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, raycastDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 offset = transform.position - footWorldPosition;
            Vector3 newPosition = hit.point + offset + Vector3.up * additionalYOffset;
            transform.position = newPosition;
            
            if (logResult || enableDebugLog)
            {
                Debug.Log($"[GroundAlignAnchor] {name} 对齐成功 -> {newPosition}", this);
            }
            return true;
        }
        
        if (logResult || enableDebugLog)
        {
            Debug.LogWarning($"[GroundAlignAnchor] {name} 对齐失败：未检测到地面（LayerMask: {groundLayers})", this);
        }
        return false;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (TryGetFootWorldPosition(out Vector3 footPos))
        {
            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.8f);
            Gizmos.DrawSphere(footPos, 0.02f);
            
            Gizmos.color = Color.yellow;
            Vector3 start = footPos + Vector3.up * RaycastLift;
            Gizmos.DrawLine(start, start + Vector3.down * raycastDistance);
        }
    }
    
    private bool TryGetFootWorldPosition(out Vector3 footWorldPos)
    {
        if (footPoint != null)
        {
            footWorldPos = footPoint.position;
            return true;
        }
        
        if (cachedCollider == null)
        {
            cachedCollider = GetComponent<Collider>();
        }
        
        if (cachedCollider != null)
        {
            Bounds bounds = cachedCollider.bounds;
            footWorldPos = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            return true;
        }
        
        footWorldPos = Vector3.zero;
        return false;
    }
}

