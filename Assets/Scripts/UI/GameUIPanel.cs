using UnityEngine;
using UnityEngine.UI;

public class GameUIPanel : MonoBehaviour
{
    [Header("UI元素")]
    public Image playerHealthBar;
    
    private PlayerCore playerCore;
    
    void Start()
    {
        // 查找玩家核心组件
        playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore != null)
        {
            playerCore.OnHealthChanged += UpdateHealthBar;
        }
        
        // 延迟初始化UI，确保PlayerCore已经完成初始化
        StartCoroutine(InitializeUIAfterDelay());
    }
    
    System.Collections.IEnumerator InitializeUIAfterDelay()
    {
        // 等待一帧确保所有组件都完成初始化
        yield return null;
        
        // 初始化UI
        UpdateHealthBar(playerCore != null ? playerCore.GetHealthPercentage() : 1f);
        
        Debug.Log($"GameUIPanel: UI初始化完成 - 血量: {playerCore?.GetHealthPercentage()}");
    }
    
    
    void UpdateHealthBar(float healthPercentage)
    {
        if (playerHealthBar != null)
        {
            playerHealthBar.fillAmount = healthPercentage;
        }
    }
    
    void OnDestroy()
    {
        if (playerCore != null)
        {
            playerCore.OnHealthChanged -= UpdateHealthBar;
        }
    }
}
