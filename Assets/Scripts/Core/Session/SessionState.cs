using UnityEngine;

/// <summary>
/// 会话状态数据
/// 记录游戏会话的状态信息
/// </summary>
[System.Serializable]
public class SessionState
{
    #region 地图系统状态
    
    /// <summary>
    /// 是否来自地图系统
    /// </summary>
    public bool fromMapSystem = false;
    
    /// <summary>
    /// 当前地图层级
    /// </summary>
    public int currentMapLayer = -1;
    
    #endregion
    
    #region 关卡状态
    
    /// <summary>
    /// 当前关卡ID/名称
    /// </summary>
    public string currentLevelID = "";
    
    /// <summary>
    /// 当前难度
    /// </summary>
    public int currentDifficulty = 0;
    
    #endregion
    
    #region 玩家状态
    
    /// <summary>
    /// 选择的角色ID
    /// </summary>
    public string selectedCharacterID = "";
    
    #endregion
    
    #region 管理方法
    
    /// <summary>
    /// 设置地图系统状态
    /// </summary>
    public void SetMapSystemState(bool fromMap, int layer)
    {
        fromMapSystem = fromMap;
        currentMapLayer = layer;
    }
    
    /// <summary>
    /// 清除地图系统标记
    /// </summary>
    public void ClearMapSystemFlag()
    {
        fromMapSystem = false;
    }
    
    /// <summary>
    /// 重置所有状态
    /// </summary>
    public void Clear()
    {
        fromMapSystem = false;
        currentMapLayer = -1;
        currentLevelID = "";
        currentDifficulty = 0;
        selectedCharacterID = "";
    }
    
    /// <summary>
    /// 检查是否有地图层级数据
    /// </summary>
    public bool HasMapLayerData()
    {
        return currentMapLayer >= 0;
    }
    
    #endregion
}

