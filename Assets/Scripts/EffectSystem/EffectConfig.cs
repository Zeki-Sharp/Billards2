using UnityEngine;
using MoreMountains.Feedbacks;

namespace DeepSpaceLabs.SAM
{
    /// <summary>
    /// 特效配置类 - 用于在 Inspector 中配置特效引用
    /// 支持直接拖拽 MMF_Player 组件进行配置
    /// </summary>
    [System.Serializable]
    public class EffectConfig
    {
        [Header("特效配置")]
        [Tooltip("特效键名，用于标识特效")]
        public string key;
        
        [Tooltip("MMF Player 组件引用，直接拖拽 Inspector 中的 MMF_Player 组件")]
        public MMF_Player mmfPlayer;
        
        [Header("调试信息")]
        [Tooltip("是否启用此特效的调试日志")]
        public bool enableDebugLog = true;
        
        /// <summary>
        /// 检查配置是否有效
        /// </summary>
        /// <returns>配置是否有效</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(key) && mmfPlayer != null;
        }
        
        /// <summary>
        /// 获取调试信息
        /// </summary>
        /// <returns>调试信息字符串</returns>
        public string GetDebugInfo()
        {
            if (string.IsNullOrEmpty(key))
                return "无效配置: key 为空";
                
            if (mmfPlayer == null)
                return $"无效配置: {key} 的 MMF Player 为 null";
                
            return $"有效配置: {key} -> {mmfPlayer.name}";
        }
    }
}
