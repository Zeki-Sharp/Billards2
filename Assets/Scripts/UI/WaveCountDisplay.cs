using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 波次显示组件（独立于 TopBarController）
/// - 职责：显示 x/y（x=剩余波次，y=总波次），在进入关卡后读取场景中的 WaveConfigProvider
/// - 事件：监听 OnLevelStarted 初始化；监听 OnWaveEnemiesSpawnComplete 刷新
/// - 解耦：不依赖 TopBarController 的生命周期，避免初始化顺序导致的数据源为空
/// </summary>
public class WaveCountDisplay : MonoBehaviour
{
	[Header("UI")]
	[SerializeField]
	[Tooltip("显示波次（x/y）的文本")]
	private TextMeshProUGUI waveText;
	
	[SerializeField]
	[Tooltip("可选：通过 CanvasGroup 控制可见性")]
	private CanvasGroup canvasGroup;
	
	[Header("场景显示控制")]
	[SerializeField]
	[Tooltip("在地图场景是否显示")]
	private bool showInMapScene = false;
	
	[SerializeField]
	[Tooltip("在关卡场景是否显示")]
	private bool showInLevelScene = true;
	
	[SerializeField]
	[Tooltip("地图场景名称（包含判断），例如：MapScene / WorldMap")]
	private string[] mapSceneNames = { "MapScene" };
	
	[Header("调试")]
	[SerializeField] private bool showDebugInfo = false;
	
	// 简单计数：y=总波次数（波次数+初始波次），x=当前剩余波次；仅在“清空当前波次”时减一
	private int totalWaves = -1;
	private int remainingWaves = -1;
	private bool loopWaves = false;
	private bool waveInProgress = false; // 是否正在进行一波（有敌人存活）
	
	void Awake()
	{
		if (waveText == null)
		{
			waveText = GetComponent<TextMeshProUGUI>();
		}
		if (canvasGroup == null)
		{
			canvasGroup = GetComponent<CanvasGroup>();
		}
	}
	
	void OnEnable()
	{
		GameEventBus.OnLevelStarted += OnLevelStarted;
		GameEventBus.OnWaveEnemiesSpawnComplete += OnWaveEnemiesSpawnComplete;
		GameEventBus.OnInitialWaveSpawnComplete += OnInitialWaveSpawnComplete;
		GameEventBus.OnDeath += OnAnyDeath;
		SceneManager.sceneLoaded += OnSceneLoaded;
		
		// 进入激活态时尝试刷新一次（例如在场景已加载的情况下）
		UpdateVisibilityByScene(SceneManager.GetActiveScene().name);
		RecomputeTotalsFromProviderIfPossible();
		UpdateText();
	}
	
	void OnDisable()
	{
		GameEventBus.OnLevelStarted -= OnLevelStarted;
		GameEventBus.OnWaveEnemiesSpawnComplete -= OnWaveEnemiesSpawnComplete;
		GameEventBus.OnInitialWaveSpawnComplete -= OnInitialWaveSpawnComplete;
		GameEventBus.OnDeath -= OnAnyDeath;
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}
	
	private void OnLevelStarted(int levelIndex, LevelConfig levelConfig)
	{
		// 初始化简单计数（只依赖 LevelConfig）
		if (levelConfig == null) return;
		int waves = levelConfig.GetTotalWaveCount(); // 不含初始
		bool hasInitial = levelConfig.generateInitialEnemies && levelConfig.initialEnemies != null && levelConfig.initialEnemies.Count > 0;
		totalWaves = Mathf.Max(0, waves + (hasInitial ? 1 : 0));
		remainingWaves = totalWaves;
		loopWaves = levelConfig.loopWaves;
		waveInProgress = false;
		
		if (showDebugInfo)
		{
			Debug.Log($"[WaveCountDisplay] OnLevelStarted => total={totalWaves}, remaining={remainingWaves}, loop={loopWaves}");
		}
		
		UpdateText();
	}
	
	private void OnWaveEnemiesSpawnComplete()
	{
		// 开始新的一波：标记进入进行中状态（等待清空后扣减）
		waveInProgress = true;
		UpdateText();
	}
	
	private void OnInitialWaveSpawnComplete()
	{
		// 初始波次生成完成，也视为开始一波
		waveInProgress = true;
		UpdateText();
	}
	
	private void OnAnyDeath(DeathData deathData)
	{
		// 仅在敌人死亡时检查是否清空当前波次
		if (deathData.DeadObjectTag == "Enemy" || deathData.DeathType == "EnemyDeath")
		{
			StartCoroutine(CheckWaveClearedNextFrame());
		}
	}
	
	private System.Collections.IEnumerator CheckWaveClearedNextFrame()
	{
		yield return null;
		
		var enemyMgr = EnemyManager.Instance;
		if (enemyMgr == null) yield break;
		
		if (waveInProgress && remainingWaves > 0 && enemyMgr.ActiveEnemyCount == 0)
		{
			remainingWaves = Mathf.Max(0, remainingWaves - 1);
			waveInProgress = false;
			if (showDebugInfo)
			{
				Debug.Log($"[WaveCountDisplay] Wave cleared -> remaining={remainingWaves}");
			}
			UpdateText();
		}
	}
	
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		UpdateVisibilityByScene(scene.name);
	}
	
	/// <summary>
	/// 刷新显示文本（基于 simple counter）
	/// </summary>
	private void UpdateText()
	{
		if (waveText == null)
		{
			return;
		}
		
		if (totalWaves <= 0)
		{
			waveText.text = "-/-";
			return;
		}
		
		if (loopWaves)
		{
			waveText.text = $"关卡波次:∞/{totalWaves}";
			return;
		}
		
		waveText.text = $"关卡波次:{Mathf.Clamp(remainingWaves, 0, totalWaves)}/{totalWaves}";
	}
	
	/// <summary>
	/// 若已能获取 Provider/LevelConfig，则据此初始化总波次数；仅在 totals 尚未初始化时调用
	/// </summary>
	private void RecomputeTotalsFromProviderIfPossible()
	{
		var provider = Object.FindFirstObjectByType<WaveConfigProvider>();
		if (provider == null) return;
		
		LevelConfig levelConfig = provider.GetCurrentLevelConfig();
		if (levelConfig == null) return;
		
		if (totalWaves < 0 || remainingWaves < 0)
		{
			int waves = provider.GetTotalWaveCount(); // 不含初始
			bool hasInitial = levelConfig.generateInitialEnemies && levelConfig.initialEnemies != null && levelConfig.initialEnemies.Count > 0;
			totalWaves = Mathf.Max(0, waves + (hasInitial ? 1 : 0));
			remainingWaves = totalWaves;
			loopWaves = levelConfig.loopWaves;
			
			if (showDebugInfo)
			{
				Debug.Log($"[WaveCountDisplay] Init from Provider => total={totalWaves}, remaining={remainingWaves}, loop={loopWaves}");
			}
		}
	}
	
	#region 可见性控制（按场景）
	private void UpdateVisibilityByScene(string sceneName)
	{
		// 判断是否地图场景（名称包含判断）
		bool isMapScene = false;
		if (mapSceneNames != null)
		{
			for (int i = 0; i < mapSceneNames.Length; i++)
			{
				if (!string.IsNullOrEmpty(mapSceneNames[i]) && sceneName.Contains(mapSceneNames[i]))
				{
					isMapScene = true;
					break;
				}
			}
		}
		
		if (isMapScene)
		{
			SetVisible(showInMapScene);
		}
		else
		{
			SetVisible(showInLevelScene);
		}
	}
	
	private void SetVisible(bool visible)
	{
		if (canvasGroup != null)
		{
			canvasGroup.alpha = visible ? 1f : 0f;
			canvasGroup.interactable = visible;
			canvasGroup.blocksRaycasts = visible;
		}
		else if (waveText != null)
		{
			waveText.gameObject.SetActive(visible);
		}
		else
		{
			gameObject.SetActive(visible);
		}
	}
	#endregion
	
	/// <summary>
	/// 外部可调用的手动刷新接口
	/// </summary>
	public void RefreshDisplay()
	{
		UpdateText();
	}
}

