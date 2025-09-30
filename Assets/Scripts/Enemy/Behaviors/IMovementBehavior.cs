using UnityEngine;

/// <summary>
/// 移动行为接口
/// 定义敌人移动行为的标准契约
/// </summary>
public interface IMovementBehavior
{
    /// <summary>
    /// 执行移动行为
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="playerTransform">玩家Transform</param>
    /// <param name="enemyData">敌人数据</param>
    /// <returns>移动目标位置</returns>
    Vector2 ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData);
    
    /// <summary>
    /// 检查是否正在移动
    /// </summary>
    /// <returns>是否正在移动</returns>
    bool IsMoving();
    
    /// <summary>
    /// 获取移动方向
    /// </summary>
    /// <returns>移动方向向量</returns>
    Vector2 GetMovementDirection();
    
    /// <summary>
    /// 设置移动状态
    /// </summary>
    /// <param name="moving">是否正在移动</param>
    void SetMoving(bool moving);
    
    /// <summary>
    /// 获取当前移动速度
    /// </summary>
    /// <returns>当前移动速度</returns>
    float GetCurrentMoveSpeed();
}
