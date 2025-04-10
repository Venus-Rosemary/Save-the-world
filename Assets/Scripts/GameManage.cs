using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using TMPro;

public class GameManage : Singleton<GameManage>
{
    [Header("游戏设置")]
    [SerializeField] private float gameDuration = 300f; // 游戏时长5分钟
    [SerializeField] private float blackHoleInterval = 30f; // 黑洞生成间隔
    [SerializeField] private float respawnDelay = 5f; // 清理完怪物后的重生延迟
    [SerializeField] private int scoreToWin = 50; // 胜利所需分数

    [Header("预制体")]
    public GameObject PlayerObject;//玩家
    public GameObject BlackHolePrefabVFX; // 黑洞视觉效果预制体
    public GameObject[] EnemyPrefabs; // 敌人预制体数组，支持多种敌人

    [Header("生成区域")]
    [SerializeField] private Vector2 CreatBoundaryMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 CreatBoundaryMax = new Vector2(20f, 20f);

    [Header("最终形态位置")]
    public List<Transform> finalPos = new List<Transform>();

    [Header("事件")]
    public UnityEvent onGameStart;
    public UnityEvent onGameWin;
    public UnityEvent onGameLose;
    public UnityEvent<int> onScoreChanged;
    public UnityEvent<float> onTimeChanged;

    [Header("UI面板")]
    public TMP_Text timeUI;

    // 私有变量
    private GameObject currentBlackHole;
    private bool isEarlyTermination = false;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private int currentScore = 0;
    private float remainingTime;//倒计时当前时间
    private bool isGameActive = false;
    private Coroutine gameTimerCoroutine;
    private Coroutine blackHoleSpawnCoroutine;



    private void Start()
    {
        // 检查预制体设置
        if (EnemyPrefabs == null || EnemyPrefabs.Length == 0)
        {
            Debug.LogError("未设置敌人预制体！");
        }
        // 游戏不会自动开始，需要调用StartGame方法
    }

    private void Update()
    {
        if (currentBlackHole!=null)
        {
            isEarlyTermination = true;
        }
        // 检查是否所有敌人都被清理
        if (isGameActive && activeEnemies.Count == 0 && currentBlackHole == null && isEarlyTermination)
        {
            // 提前生成下一波黑洞
            Debug.Log("111");
            StartCoroutine(SpawnBlackHoleDelayed(respawnDelay));
            isEarlyTermination = false;
        }

        // 检查最终形态位置是否已满
        if (isGameActive && CheckAllFinalPositionsOccupied())
        {
            EndGame(false);
        }
    }

    private void OnEnable()
    {
        // 注册事件

        onGameStart.AddListener(SetGameStartEvent);
        onTimeChanged.AddListener(SetonTimeChangedEvent);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    // 公共方法：开始游戏
    public void StartGame()
    {
        //if (isGameActive) return;

        // 重置游戏状态
        currentScore = 0;
        remainingTime = gameDuration;
        isGameActive = true;
        
        // 清理场景中的敌人
        ClearAllEnemies();
        
        // 触发游戏开始事件
        onGameStart?.Invoke();
        
        // 更新UI
        onScoreChanged?.Invoke(currentScore);
        onTimeChanged?.Invoke(remainingTime);
        
        // 开始游戏计时器
        gameTimerCoroutine = StartCoroutine(GameTimer());
        
        // 生成第一个黑洞
        SpawnBlackHole();
    }

    // 游戏计时器
    private IEnumerator GameTimer()
    {
        while (remainingTime > 0 && isGameActive)
        {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
            onTimeChanged?.Invoke(remainingTime);
        }
        
        // 时间结束，检查是否达成胜利条件
        if (isGameActive)
        {
            EndGame(currentScore >= scoreToWin);
        }
    }

    #region 设置游戏开始事件
    private void SetGameStartEvent()
    {
        PlayerObject.SetActive(true);
    }
    #endregion

    #region 设置时间改变事件
    private void SetonTimeChangedEvent(float a)
    {
        timeUI.text = $"time:  {FormatTime(a)}";
    }
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
    #endregion


    // 生成黑洞
    private void SpawnBlackHole()
    {
        if (!isGameActive) return;
        
        // 随机位置
        float rangePosX = Random.Range(CreatBoundaryMin.x, CreatBoundaryMax.x);
        float rangePosZ = Random.Range(CreatBoundaryMin.y, CreatBoundaryMax.y);
        Vector3 rangePos = new Vector3(rangePosX, 1.5f, rangePosZ);
        
        // 实例化黑洞
        currentBlackHole = Instantiate(BlackHolePrefabVFX, rangePos, Quaternion.identity);
        currentBlackHole.transform.DOScale(0.3f, 1).From();
        
        // 开始生成敌人
        StartCoroutine(SpawnEnemiesFromBlackHole());
        
        // 设置下一次黑洞生成
        blackHoleSpawnCoroutine = StartCoroutine(SpawnBlackHoleDelayed(blackHoleInterval));
    }

    // 延迟生成黑洞
    private IEnumerator SpawnBlackHoleDelayed(float delay)
    {
        // 取消之前的生成计划
        if (blackHoleSpawnCoroutine != null)
        {
            StopCoroutine(blackHoleSpawnCoroutine);
        }
        
        yield return new WaitForSeconds(delay);
        
        // 如果游戏仍在进行，生成新的黑洞
        if (isGameActive)
        {
            SpawnBlackHole();
        }
    }

    // 从黑洞生成敌人
    private IEnumerator SpawnEnemiesFromBlackHole()
    {
        if (currentBlackHole == null) yield break;
        
        // 随机决定生成敌人的数量
        int enemyCount = Random.Range(3, 6);
        
        for (int i = 0; i < enemyCount; i++)
        {
            yield return new WaitForSeconds(3f);
            
            if (!isGameActive || currentBlackHole == null) break;
            
            // 随机选择敌人类型
            int enemyTypeIndex = Random.Range(0, EnemyPrefabs.Length);
            GameObject enemyPrefab = EnemyPrefabs[enemyTypeIndex];
            
            // 生成敌人
            GameObject enemy = Instantiate(enemyPrefab, currentBlackHole.transform.position, Quaternion.identity);
            enemy.GetComponent<EnemyFSM>()?.ChangeState(EnemyState.Patrol);
            
            // 设置最终位置
            EnemyGrowth enemyGrowth = enemy.GetComponent<EnemyGrowth>();
            if (enemyGrowth != null)
            {
                enemyGrowth.SetFindAFP(finalPos);
                
                // 注册敌人死亡事件
                Health enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null)
                {
                    enemyHealth.onDeath.AddListener(() => OnEnemyDeath(enemy));
                }
            }
            
            // 添加到活跃敌人列表
            activeEnemies.Add(enemy);
        }
        
        // 黑洞消失
        yield return new WaitForSeconds(2f);
        
        if (currentBlackHole != null)
        {
            currentBlackHole.transform.DOScale(0f, 1f);
            yield return new WaitForSeconds(1f);
            Destroy(currentBlackHole);
            currentBlackHole = null;
        }
    }

    // 敌人死亡回调
    private void OnEnemyDeath(GameObject enemy)
    {
        // 从活跃敌人列表中移除
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
        
        // 增加分数
        currentScore++;
        onScoreChanged?.Invoke(currentScore);
        
        // 检查是否达到胜利条件
        if (currentScore >= scoreToWin && isGameActive)
        {
            EndGame(true);
        }
    }

    // 检查所有最终位置是否被占用
    private bool CheckAllFinalPositionsOccupied()
    {
        if (finalPos.Count == 0) return false;
        
        int occupiedCount = 0;
        
        foreach (GameObject enemy in activeEnemies)
        {
            EnemyGrowth growth = enemy.GetComponent<EnemyGrowth>();
            if (growth != null && growth.IsInFinalPosition())
            {
                occupiedCount++;
            }
        }
        
        return occupiedCount >= finalPos.Count;
    }

    // 清理所有敌人
    private void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        
        activeEnemies.Clear();
        
        if (currentBlackHole != null)
        {
            Destroy(currentBlackHole);
            currentBlackHole = null;
        }
    }

    // 结束游戏
    private void EndGame(bool isWin)
    {
        if (!isGameActive) return;
        
        isGameActive = false;
        
        // 停止所有计时器
        if (gameTimerCoroutine != null)
        {
            StopCoroutine(gameTimerCoroutine);
        }
        
        if (blackHoleSpawnCoroutine != null)
        {
            StopCoroutine(blackHoleSpawnCoroutine);
        }
        
        // 触发相应事件
        if (isWin)
        {
            Debug.Log("游戏胜利！得分：" + currentScore);
            onGameWin?.Invoke();
        }
        else
        {
            Debug.Log("游戏失败！得分：" + currentScore);
            onGameLose?.Invoke();
        }
    }

    private void OnDrawGizmos()
    {
        // 绘制生成边界
        Gizmos.color = Color.green;
        Vector3 boundaryCenter = new Vector3(
            (CreatBoundaryMin.x + CreatBoundaryMax.x) / 2,
            0.5f,
            (CreatBoundaryMin.y + CreatBoundaryMax.y) / 2
        );
        Vector3 boundarySize = new Vector3(
            CreatBoundaryMax.x - CreatBoundaryMin.x,
            0.1f,
            CreatBoundaryMax.y - CreatBoundaryMin.y
        );
        Gizmos.DrawWireCube(boundaryCenter, boundarySize);
    }
}
