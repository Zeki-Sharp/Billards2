using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("血条设置")]
    public Image healthBarFill; // 血条填充图片
    public Image healthBarBackground; // 血条背景图片
    
    [Header("血条显示设置")]
    public bool alwaysVisible = false; // 是否始终显示
    public float hideDelay = 2f; // 血条隐藏延迟时间
    
    private float hideTimer = 0f;
    private bool isVisible = true;
    
    void Start()
    {
        // 设置初始状态
        if (!alwaysVisible)
        {
            SetVisible(false);
        }
    }
    
    void Update()
    {
            
            // 处理血条显示/隐藏
            if (!alwaysVisible)
            {
                if (isVisible)
                {
                    hideTimer -= Time.deltaTime;
                    if (hideTimer <= 0)
                    {
                        SetVisible(false);
                    }
                }
            }
    }
    
    
    public void UpdateHealth(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            float healthPercentage = Mathf.Clamp01(currentHealth / maxHealth);
            healthBarFill.fillAmount = healthPercentage;
            
            // 不修改颜色，使用预制体中设置的颜色
        }
        
        // 显示血条
        if (!alwaysVisible)
        {
            SetVisible(true);
            hideTimer = hideDelay;
        }
    }
    
    void SetVisible(bool visible)
    {
        isVisible = visible;
        gameObject.SetActive(visible);
    }
    
    public void Show()
    {
        SetVisible(true);
        if (!alwaysVisible)
        {
            hideTimer = hideDelay;
        }
    }
    
    public void Hide()
    {
        SetVisible(false);
    }
}
