using UnityEngine;
using System.Collections;

/// <summary>
/// 统一的时间缩放管理类
/// 提供类似Unity Time的接口，但自动处理时间缩放
/// </summary>
public static class ScaledTime
{
    /// <summary>
    /// 获取受TimeManager管理的DeltaTime
    /// </summary>
    public static float deltaTime
    {
        get
        {
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.GetEnemyDeltaTime();
            }
            return Time.deltaTime;
        }
    }
    
    /// <summary>
    /// 获取受TimeManager管理的时间缩放
    /// </summary>
    public static float timeScale
    {
        get
        {
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.GetEnemyTimeScale();
            }
            return 1f;
        }
    }
    
    /// <summary>
    /// 获取受TimeManager管理的累计时间
    /// </summary>
    public static float time
    {
        get
        {
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.GetScaledTime();
            }
            return Time.time;
        }
    }
    
    /// <summary>
    /// 等待指定秒数（使用缩放时间）
    /// </summary>
    public static IEnumerator WaitForSeconds(float seconds)
    {
        float timer = 0f;
        while (timer < seconds)
        {
            timer += deltaTime;
            yield return null;
        }
    }
    
    /// <summary>
    /// 等待直到条件满足（使用缩放时间）
    /// </summary>
    public static IEnumerator WaitUntil(System.Func<bool> predicate)
    {
        while (!predicate())
        {
            yield return null;
        }
    }
    
    /// <summary>
    /// 等待直到条件不满足（使用缩放时间）
    /// </summary>
    public static IEnumerator WaitWhile(System.Func<bool> predicate)
    {
        while (predicate())
        {
            yield return null;
        }
    }
}
