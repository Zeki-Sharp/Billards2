using UnityEngine;

/// <summary>
/// UI面板类型枚举
/// 用于区分不同类型面板的显示行为和互斥规则
/// </summary>
public enum UIPanelType
{
    /// <summary>
    /// 游戏内UI（HUD）
    /// 特点：始终显示，不互斥，不暂停游戏
    /// 例如：血条、技能CD等
    /// </summary>
    HUD,
    
    /// <summary>
    /// 全屏UI（FullScreen）
    /// 特点：全屏显示，互斥，通常暂停游戏
    /// 例如：技能选择界面、设置界面、商店界面
    /// </summary>
    FullScreen,
    
    /// <summary>
    /// 弹窗UI（Popup）
    /// 特点：弹出显示，互斥，通常暂停游戏
    /// 例如：胜利界面、失败界面、提示对话框
    /// </summary>
    Popup,
    
    /// <summary>
    /// 提示UI（Tips）
    /// 特点：短暂显示，不互斥，不暂停游戏
    /// 例如：伤害数字、飘字提示
    /// </summary>
    Tips
}

/// <summary>
/// UI面板数据
/// 用于在显示面板时传递数据
/// </summary>
public class UIPanelData
{
    // 基类为空，具体面板继承并扩展自己需要的数据
}

