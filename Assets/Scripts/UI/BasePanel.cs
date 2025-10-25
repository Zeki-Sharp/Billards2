using UnityEngine;

/// <summary>
/// UI面板基类
/// 所有UI面板都应该继承此类
/// 
/// 【核心职责】：
/// - 定义面板标准生命周期
/// - 提供统一的显示/隐藏接口
/// - 管理面板状态和类型
/// 
/// 【设计原则】：
/// - 面板只负责UI显示和更新
/// - 不主动订阅游戏事件（由UIController管理）
/// - 通过UIController显示/隐藏
/// </summary>
public abstract class BasePanel : MonoBehaviour
{
    [Header("面板配置")]
    [SerializeField] protected UIPanelType panelType = UIPanelType.Popup;
    [SerializeField] protected bool pauseGameOnShow = true;
    
    [Header("调试")]
    [SerializeField] protected bool showDebugInfo = true;
    
    // 面板状态
    private bool isInitialized = false;
    private bool isVisible = false;
    
    #region 生命周期方法
    
    /// <summary>
    /// 面板初始化（只调用一次）
    /// 子类重写此方法进行初始化操作
    /// </summary>
    public virtual void OnInit()
    {
        if (isInitialized)
            return;
        
        isInitialized = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"{GetType().Name}: 初始化完成");
        }
    }
    
    /// <summary>
    /// 面板显示时调用
    /// 子类重写此方法更新UI数据
    /// </summary>
    /// <param name="data">传递给面板的数据</param>
    public virtual void OnShow(UIPanelData data = null)
    {
        // 确保已初始化
        if (!isInitialized)
        {
            OnInit();
        }
        
        // 显示面板
        gameObject.SetActive(true);
        isVisible = true;
        
        if (showDebugInfo)
        {
            Debug.Log($"{GetType().Name}: 显示面板");
        }
    }
    
    /// <summary>
    /// 面板隐藏时调用
    /// 子类重写此方法进行清理操作
    /// </summary>
    public virtual void OnHide()
    {
        // 隐藏面板
        gameObject.SetActive(false);
        isVisible = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"{GetType().Name}: 隐藏面板");
        }
    }
    
    /// <summary>
    /// 更新面板数据
    /// 子类重写此方法更新UI显示
    /// </summary>
    /// <param name="data">新的数据</param>
    public virtual void OnUpdate(UIPanelData data)
    {
        // 子类实现具体的更新逻辑
    }
    
    #endregion
    
    #region 公共属性和方法
    
    /// <summary>
    /// 获取面板类型
    /// </summary>
    public UIPanelType PanelType => panelType;
    
    /// <summary>
    /// 是否在显示时暂停游戏
    /// </summary>
    public bool PauseGameOnShow => pauseGameOnShow;
    
    /// <summary>
    /// 面板是否已初始化
    /// </summary>
    public bool IsInitialized => isInitialized;
    
    /// <summary>
    /// 面板当前是否可见
    /// </summary>
    public bool IsVisible => isVisible;
    
    #endregion
}

