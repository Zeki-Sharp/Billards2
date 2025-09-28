using UnityEngine;
using MoreMountains.Feedbacks;

namespace DeepSpaceLabs.SAM
{
    /// <summary>
    /// MMF Player 参数设置工具类
    /// 提供各种特效参数设置方法，供 EffectManager 和其他组件使用
    /// </summary>
    public static class MMFPlayerParameterSetter
    {
        /// <summary>
        /// 设置特效位置
        /// </summary>
        /// <param name="mmfPlayer">MMF Player 组件</param>
        /// <param name="position">基础位置</param>
        /// <param name="useWallOffset">是否使用墙壁偏移</param>
        /// <param name="wallOffset">墙壁偏移量</param>
        public static void SetEffectPosition(MMF_Player mmfPlayer, Vector3 position, bool useWallOffset = false, Vector3 wallOffset = default)
        {
            if (mmfPlayer == null) return;
            
            var positionFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_Position>();
            if (positionFeedbacks == null || positionFeedbacks.Count == 0) return;
            
            Vector3 finalPosition = useWallOffset ? position + wallOffset : position;
            
            foreach (var positionFeedback in positionFeedbacks)
            {
                positionFeedback.DestinationPosition = finalPosition;
                positionFeedback.InitialPosition = finalPosition;
            }
        }
        
        /// <summary>
        /// 设置墙壁撞击特效参数
        /// </summary>
        /// <param name="mmfPlayer">MMF Player 组件</param>
        /// <param name="attackData">攻击数据</param>
        public static void SetWallHitParameters(MMF_Player mmfPlayer, AttackData attackData)
        {
            if (mmfPlayer == null || attackData.HitNormal == Vector3.zero) return;
            
            SetPositionSpringEffect(mmfPlayer, attackData.WallHitPositionOffset);
            SetRotationEffect(mmfPlayer, attackData.WallHitRotationAngle);
            SetScaleEffect(mmfPlayer, attackData.HitSpeed);
        }
        
        /// <summary>
        /// 设置位置弹簧效果
        /// </summary>
        /// <param name="mmfPlayer">MMF Player 组件</param>
        /// <param name="positionOffset">位置偏移量</param>
        public static void SetPositionSpringEffect(MMF_Player mmfPlayer, Vector3 positionOffset)
        {
            var positionSpringFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_PositionSpring>();
            if (positionSpringFeedbacks == null || positionSpringFeedbacks.Count == 0) return;
            
            foreach (var springFeedback in positionSpringFeedbacks)
            {
                springFeedback.BumpPositionMin = Vector3.zero;
                springFeedback.BumpPositionMax = positionOffset;
            }
        }
        
        /// <summary>
        /// 设置旋转效果
        /// </summary>
        /// <param name="mmfPlayer">MMF Player 组件</param>
        /// <param name="rotationAngle">旋转角度</param>
        public static void SetRotationEffect(MMF_Player mmfPlayer, float rotationAngle)
        {
            var rotationFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_Rotation>();
            if (rotationFeedbacks == null || rotationFeedbacks.Count == 0) return;
            
            foreach (var rotationFeedback in rotationFeedbacks)
            {
                rotationFeedback.RemapCurveOne = rotationAngle;
            }
        }
        
        /// <summary>
        /// 设置缩放效果
        /// </summary>
        /// <param name="mmfPlayer">MMF Player 组件</param>
        /// <param name="hitSpeed">撞击速度</param>
        public static void SetScaleEffect(MMF_Player mmfPlayer, float hitSpeed)
        {
            var scaleFeedbacks = mmfPlayer.GetFeedbacksOfType<MMF_Scale>();
            if (scaleFeedbacks == null || scaleFeedbacks.Count == 0) return;
            
            float speedRatio = Mathf.Clamp01(hitSpeed / 50f);
            float scaleMultiplier = Mathf.Lerp(0.5f, 2.0f, speedRatio);
            Vector3 scale = Vector3.one * scaleMultiplier;
            
            foreach (var scaleFeedback in scaleFeedbacks)
            {
                try
                {
                    var scaleField = scaleFeedback.GetType().GetField("DestinationScale");
                    scaleField?.SetValue(scaleFeedback, scale);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"MMFPlayerParameterSetter: 设置缩放参数失败 - {e.Message}");
                }
            }
        }
        
        /// <summary>
        /// 设置特效方向
        /// </summary>
        /// <param name="mmfPlayer">MMF Player 组件</param>
        /// <param name="direction">方向向量</param>
        public static void SetEffectDirection(MMF_Player mmfPlayer, Vector3 direction)
        {
            if (mmfPlayer == null || direction == Vector3.zero) return;
            
            mmfPlayer.transform.rotation = Quaternion.LookRotation(direction);
        }
        
        /// <summary>
        /// 判断是否为墙壁撞击特效
        /// </summary>
        /// <param name="effectKey">特效键名</param>
        /// <returns>是否为墙壁撞击特效</returns>
        public static bool IsWallHitEffect(string effectKey)
        {
            return effectKey != null && (
                effectKey.Contains("Wall") || 
                effectKey.Contains("Hit") || 
                effectKey.Contains("Be Hit") ||
                effectKey.ToLower().Contains("wallhit")
            );
        }
    }
}
