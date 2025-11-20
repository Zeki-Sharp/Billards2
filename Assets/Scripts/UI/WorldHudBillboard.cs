using UnityEngine;

/// <summary>
/// 世界空间 HUD 广告牌：
/// - 位置：始终等于目标敌人的世界坐标 + 一个固定世界偏移（不跟随敌人自身旋转）
/// - 朝向：始终面向摄像机，可选只绕 Y 轴旋转
/// 
/// 使用方式：
/// - 把血条/状态栏的 World Space Canvas 放在场景任意位置（不需要作为敌人的子物体）
/// - 挂上本脚本，将 target 指向敌人（或你希望对齐的挂点，例如 Enemy.enemyItem）
/// - 调整 worldOffset 让 HUD 出现在目标上方/下方合适的位置
/// </summary>
public class WorldHudBillboard : MonoBehaviour
{
    [Header("跟随目标")]
    [Tooltip("需要跟随的世界空间目标（通常是敌人或敌人的可视物体 enemyItem）")]
    public Transform target;

    [Header("世界偏移")]
    [Tooltip("在世界坐标系下相对于目标的固定偏移（不随目标旋转变化）")]
    public Vector3 worldOffset = new Vector3(0f, 1.0f, 0f);

    [Header("朝向设置")]
    [Tooltip("是否只绕 Y 轴朝向摄像机（推荐勾选，避免 HUD 上下翻转）")]
    public bool onlyRotateAroundY = true;

    private Camera _camera;

    private void Awake()
    {
        if (_camera == null)
        {
            _camera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (target == null || _camera == null)
        {
            return;
        }

        // 1. 位置：目标世界坐标 + 固定世界偏移（不依赖目标旋转）
        transform.position = target.position + worldOffset;

        // 2. 朝向：始终面向摄像机
        Vector3 forward = _camera.transform.forward;

        if (onlyRotateAroundY)
        {
            // 只保留水平分量，避免 HUD 随摄像机俯仰而翻转
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = Vector3.forward;
            }
        }

        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    /// <summary>
    /// 运行时动态设置跟随目标
    /// </summary>
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}


