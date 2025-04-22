using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using TMPro;

public class GameManage : Singleton<GameManage>
{
    [Header("干扰怪设置")]
    [SerializeField] private GameObject interferenceEnemyPrefab; // 干扰怪预制体
    [SerializeField] private List<Transform> interferencePositions; // 干扰怪目标位置
    [SerializeField] private float[] interferenceSpawnTimes = new float[] { 30f, 90f, 150f }; // 生成时间点
    private List<float> remainingSpawnTimes = new List<float>(); // 剩余生成时间点
    private List<Transform> availablePositions = new List<Transform>(); // 可用的目标位置

    [Header("游戏设置")]
    [SerializeField] private float gameDuration = 300f; // 游戏时长5分钟
    [SerializeField] private float blackHoleInterval = 30f; // 黑洞生成间隔
    [SerializeField] private float respawnDelay = 5f; // 清理完怪物后的重生延迟
    [SerializeField] private int scoreToWin = 50; // 胜利所需分数
    [SerializeField] private GameObject RestartEnemy; // 重新开始的怪物
    [SerializeField] private NPCManager NpcM;

    [Header("预制体")]
    public GameObject PlayerObject;//玩家
    public GameObject StartEnemy;//开始游戏的怪
    public GameObject BlackHolePrefabVFX; // 黑洞视觉效果预制体
    public GameObject[] EnemyPrefabs; // 敌人预制体数组，支持多种敌人
    public GameObject Npc;

    [Header("生成区域")]
    [SerializeField] private Vector2 CreatBoundaryMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 CreatBoundaryMax = new Vector2(20f, 20f);

    [Header("最终形态位置")]
    public List<Transform> finalPos = new List<Transform>();

    [Header("UI面板")]
    public UIController UIController;//UI管理脚本
    public TMP_Text timeUI;//时间UI
    public TMP_Text scoreUI;//分数UI
    public TMP_Text resultUI;//结果UI

    [Header("私有变量")]
    private List<GameObject> currentBlackHole = new List<GameObject>();//存放每波的黑洞
    private GameObject currentBlackHoleNumberTwo;//存放干扰怪的黑洞
    private bool isEarlyTermination = false;//提前出下波黑洞
    [SerializeField] private List<GameObject> activeEnemies = new List<GameObject>();//存储所有场景中活跃敌人------只包括可被攻击的敌人
    [SerializeField] private List<GameObject> EnemyObjects = new List<GameObject>();//存储所有活跃物体------包括干扰、完全体等
    private int currentScore = 0;//当前分数
    private float remainingTime;//倒计时当前时间
    private bool isGameActive = false;//正在游戏中
    //协程
    private Coroutine gameTimerCoroutine;
    private Coroutine blackHoleSpawnCoroutine;

    private List<float> recentKillTimes = new List<float>(); //记录最近击杀时间
    private const float FAST_CLEAR_TIME_WINDOW = 10f; //快速清理的时间窗口
    private const int FAST_CLEAR_KILL_COUNT = 5; //快速清理所需击杀数

    [Header("事件")]
    public UnityEvent onGameStart;//游戏开始的事件处理
    public UnityEvent onGameWin;//游戏胜利事件
    public UnityEvent onGameLose;//游戏失败事件
    public UnityEvent<int> onScoreChanged;//得分事件
    public UnityEvent<float> onTimeChanged;//改变时间

    public UnityEvent<int> onEnemyKilled; // 击杀敌人事件（1次）
    public UnityEvent<int> onWaveCleared; // 清理波次事件（1次）
    public UnityEvent onMoreEnemy;//更多怪事件（每波生成时）
    public UnityEvent<int> onSecondWave;//第二波事件（1次）
    public UnityEvent onFastClear;//快速清理事件（重复）
    public UnityEvent onFlyHitTarget;//干扰怪发射火球攻击事件（概率触发）
    public UnityEvent onPlayerDamageTaken;//玩家被命中事件（概率触发）
    public UnityEvent<int> onAppearFirstStage3;//出现的第一只3阶段怪（1次）
    public UnityEvent<int> onAppearFirstStage4;//出现的第一只完全体（1次）
    public UnityEvent<int> onNpcHelp;//npc帮助事件（1次）
    private int totalKills = 0;
    private int currentWave = 0;
    private int SecondWave = 0;
    private int AppearFirstStage3 = 0;
    private int AppearFirstStage4 = 0;
    public int NpcHelp = 0;



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
        if (currentBlackHole.Count!=0)
        {
            isEarlyTermination = true;
        }
        // 检查是否所有敌人都被清理
        if (isGameActive && activeEnemies.Count == 0 && currentBlackHole.Count == 0 && isEarlyTermination)
        {
            // 提前生成下一波黑洞
            if (currentWave==0)
            {
                NPCEventTrigger(1);
            }
            else
            {
                NPCEventTrigger(3);
            }
            StartCoroutine(SpawnBlackHoleDelayed(respawnDelay));
            isEarlyTermination = false;
        }

        if (activeEnemies.Count>=10 && NpcHelp==0)
        {
            NPCEventTrigger(9);
        }

        // 检查最终形态位置是否已满
        if (isGameActive && CheckAllFinalPositionsOccupied())
        {
            EndGame(false);
        }
    }

    // 注册事件
    private void OnEnable()
    {
        onGameStart.AddListener(SetGameStartEvent);
        onTimeChanged.AddListener(SetonTimeChangedEvent);
        onScoreChanged.AddListener(SetonScoreChanged);
        onGameWin.AddListener(SetonGameWinEvent);
        onGameLose.AddListener(SetonGameLoseEvent);
    }

    //关闭
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    // 公共方法：开始游戏
    public void StartGame()
    {
        //if (isGameActive) return;
        StopAllCoroutines();
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
        
        // 重置干扰怪生成时间
        remainingSpawnTimes = new List<float>(interferenceSpawnTimes);
        availablePositions = new List<Transform>(interferencePositions);
    }



    //-------------------------------------------------------------------设置事件-------------------------------------------------------------------

    #region 设置游戏开始事件
    private void SetGameStartEvent()
    {
        //PlayerObject.SetActive(true);
        if (UIController!=null)
        {
            UIController.StartButtonEvent();
        }
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

    #region 设置分数改变事件
    private void SetonScoreChanged(int a)
    {
        scoreUI.text = $"Target Score: {a}/{scoreToWin}";
    }
    #endregion

    #region 设置游戏成功结束事件
    private void SetonGameWinEvent()
    {
        resultUI.text = $"You win , Score : {currentScore}";
    }
    #endregion

    #region 设置游戏失败结束事件
    private void SetonGameLoseEvent()
    {
        Instantiate(RestartEnemy, new Vector3(0, 1, 0), Quaternion.identity);
        resultUI.text = $"You lose , Score : {currentScore}";
    }
    #endregion

    //事件调用
    public void NPCEventTrigger(int index)
    {
        switch (index)
        {
            case 0://击败第一只怪（√）
                totalKills++;
                onEnemyKilled?.Invoke(totalKills);
                break;
            case 1://第一波清完（√）
                currentWave++;
                onWaveCleared?.Invoke(currentWave);
                break;
            case 2://刷新新的波次（√）
                onMoreEnemy?.Invoke();
                break;
            case 3://第二波清完（√）
                SecondWave++;
                onSecondWave?.Invoke(SecondWave);
                break;
            case 4://10s内清完5只（√）
                onFastClear?.Invoke();
                break;
            case 5://干扰怪发射火球（√）
                onFlyHitTarget?.Invoke();
                break;
            case 6://玩家被击中（√）
                onPlayerDamageTaken?.Invoke();
                break;
            case 7://出现第一只3阶段（√）
                AppearFirstStage3++;
                onAppearFirstStage3?.Invoke(AppearFirstStage3);
                break;
            case 8://出现第一只完全体（√）
                AppearFirstStage4++;
                onAppearFirstStage4?.Invoke(AppearFirstStage4);
                break;
            case 9://npc帮助玩家
                NpcHelp++;
                onNpcHelp?.Invoke(NpcHelp);
                break;
        }
    }

    //数值还原
    public void NPCEventValueRestore()
    {
        totalKills = 0;
        currentWave = 0;
        SecondWave = 0;
        AppearFirstStage3 = 0;
        AppearFirstStage4 = 0;
        NpcHelp = 0;
        NpcM.ResetDialogues();
    }

    //-------------------------------------------------------------------管理设置-------------------------------------------------------------------

    #region 计时器-----30、90、150生成干扰怪
    private IEnumerator GameTimer()
    {
        while (remainingTime > 0 && isGameActive)
        {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
            onTimeChanged?.Invoke(remainingTime);

            // 检查是否需要生成干扰怪
            CheckInterferenceSpawn();
        }

        // 时间结束，检查是否达成胜利条件
        if (isGameActive)
        {
            EndGame(currentScore >= scoreToWin);
        }
    }
    #endregion

    #region 干扰怪生成
    private void CheckInterferenceSpawn()
    {
        if (remainingSpawnTimes.Count == 0 || availablePositions.Count == 0) return;

        float gameTime = gameDuration - remainingTime;

        for (int i = remainingSpawnTimes.Count - 1; i >= 0; i--)
        {
            if (gameTime >= remainingSpawnTimes[i])
            {
                SpawnInterferenceEnemy();
                remainingSpawnTimes.RemoveAt(i);
                break;
            }
        }
    }

    private void SpawnInterferenceEnemy()
    {
        if (availablePositions.Count == 0) return;

        // 随机选择一个可用位置
        int randomIndex = Random.Range(0, availablePositions.Count);
        Transform targetPosition = availablePositions[randomIndex];
        availablePositions.RemoveAt(randomIndex);

        currentBlackHoleNumberTwo = Instantiate(BlackHolePrefabVFX, new Vector3(0, 4, 0), Quaternion.identity);
        currentBlackHoleNumberTwo.transform.DOScale(0.3f, 1).From();

        // 从黑洞位置生成干扰怪
        Vector3 spawnPos = currentBlackHoleNumberTwo.transform.position;
        GameObject interferenceEnemy = Instantiate(interferenceEnemyPrefab, spawnPos, Quaternion.identity);

        // 设置目标位置
        FireballShooter interference = interferenceEnemy.GetComponent<FireballShooter>();
        if (interference != null)
        {
            interference.SetTargetPosition(targetPosition);
        }


        DOVirtual.DelayedCall(2, () => currentBlackHoleNumberTwo.transform.DOScale(0.3f, 1)); // 延迟关闭
        DOVirtual.DelayedCall(2.5F, () => Destroy(currentBlackHoleNumberTwo));
        DOVirtual.DelayedCall(2.6f, () => currentBlackHoleNumberTwo = null);


        // 添加到活跃敌人列表
        //activeEnemies.Add(interferenceEnemy);
        EnemyObjects.Add(interferenceEnemy);
    }
    #endregion

    #region 黑洞生成，从黑洞生成怪
    // 生成黑洞
    private void SpawnBlackHole()
    {
        if (!isGameActive) return;
        
        // 随机决定生成黑洞的数量
        int blackHoleCount = Random.Range(2, 4); // 2-3个黑洞
        
        for (int i = 0; i < blackHoleCount; i++)
        {
            // 随机位置
            float rangePosX = Random.Range(CreatBoundaryMin.x, CreatBoundaryMax.x);
            float rangePosZ = Random.Range(CreatBoundaryMin.y, CreatBoundaryMax.y);
            Vector3 rangePos = new Vector3(rangePosX, 2.3f, rangePosZ);
            
            // 实例化黑洞
            GameObject newBlackHole = Instantiate(BlackHolePrefabVFX, rangePos, Quaternion.identity);
            newBlackHole.transform.DOScale(0.3f, 1).From();
            currentBlackHole.Add(newBlackHole);

            // 开始生成敌人
            StartCoroutine(SpawnEnemiesFromBlackHole(newBlackHole));
        }
        
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
            NPCEventTrigger(2);
            SpawnBlackHole();
        }
    }

    // 从黑洞生成敌人
    private IEnumerator SpawnEnemiesFromBlackHole(GameObject blackHole)
    {
        if (blackHole == null) yield break;
        
        // 随机决定生成敌人的数量
        int enemyCount = Random.Range(2, 4); // 2-3个敌人
        
        for (int i = 0; i < enemyCount; i++)
        {
            yield return new WaitForSeconds(3f);
            
            if (!isGameActive || blackHole == null) break;
            
            // 随机选择敌人类型
            int enemyTypeIndex = Random.Range(0, EnemyPrefabs.Length);
            GameObject enemyPrefab = EnemyPrefabs[enemyTypeIndex];

            Vector3 newPos = Vector3.zero;

            // 生成敌人
            if (enemyTypeIndex == 0)
            {
                newPos = blackHole.transform.position;
            }
            else if (enemyTypeIndex == 1)
            {
                newPos = new Vector3(blackHole.transform.position.x, blackHole.transform.position.y + 1.7f, blackHole.transform.position.z);
            }
            GameObject enemy = Instantiate(enemyPrefab, newPos, Quaternion.identity);
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
            EnemyObjects.Add(enemy);
        }
        
        // 黑洞消失
        yield return new WaitForSeconds(2f);
        
        if (blackHole != null)
        {
            blackHole.transform.DOScale(0f, 1f);
            yield return new WaitForSeconds(1f);
            if (currentBlackHole.Contains(blackHole))
            {
                currentBlackHole.Remove(blackHole);
            }
            Destroy(blackHole);
        }
    }

    #endregion

    #region 敌人死亡回调
    // 敌人死亡回调
    private void OnEnemyDeath(GameObject enemy)
    {
        // 从活跃敌人列表中移除
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
            EnemyObjects.Remove(enemy);
        }

        // 检查是否是由NPC击杀的
        bool isKilledByNPC = enemy.GetComponent<Health>()?.lastDamageSource == "NPC";
        if (!isKilledByNPC)
        {
            NPCEventTrigger(0);
            // 记录击杀时间并检查快速清理
            CheckFastClear();
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
    #endregion

    #region 检查快速清理成就
    private void CheckFastClear()
    {
        if (!isGameActive) return;

        float currentTime = Time.time;
        recentKillTimes.Add(currentTime);

        // 移除超出时间窗口的击杀记录
        while (recentKillTimes.Count > 0 && currentTime - recentKillTimes[0] > FAST_CLEAR_TIME_WINDOW)
        {
            recentKillTimes.RemoveAt(0);
        }

        // 检查是否在时间窗口内达到击杀数
        if (recentKillTimes.Count >= FAST_CLEAR_KILL_COUNT)
        {
            NPCEventTrigger(4);
            recentKillTimes.Clear(); // 触发后清空记录，避免重复触发
        }
    }
    #endregion

    #region 外部调用处理列表管理

    //完全体需要将自己从怪物表中移除
    public void OnOtherScriptUse(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);
        }
    }

    //所有场景活跃物体存放位置处理
    public void OnAddEnemyObjects(GameObject enemy)
    {
        EnemyObjects.Add(enemy);
    }
    public void OnRemoveEnemyObjects(GameObject enemy)
    {
        if (EnemyObjects.Contains(enemy))
        {
            EnemyObjects.Remove(enemy);
        }
    }

    public List<GameObject> OnAcquireActiveEnemies()
    {
        return activeEnemies;
    }
    #endregion

    #region 检查所有完全体最终位置是否被占用
    // 检查所有最终位置是否被占用
    private bool CheckAllFinalPositionsOccupied()
    {
        if (finalPos.Count == 0) return false;
        
        int occupiedCount = 0;
        
        foreach (GameObject enemy in EnemyObjects)
        {
            EnemyGrowth growth = enemy.GetComponent<EnemyGrowth>();
            if (growth != null && growth.IsInFinalPosition())
            {
                occupiedCount++;
            }
        }
        
        return occupiedCount >= finalPos.Count;
    }
    #endregion

    #region 结束的清理等处理
    // 清理所有敌人
    public void ClearAllEnemies()
    {

        NPCEventValueRestore();

        foreach (GameObject enemy in EnemyObjects)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        
        activeEnemies.Clear();
        EnemyObjects.Clear();

        recentKillTimes.Clear();

        if (currentBlackHole.Count != 0)
        {
            foreach(GameObject enemy in currentBlackHole)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            currentBlackHole.Clear();
        }

        if (currentBlackHoleNumberTwo!=null)
        {
            Destroy(currentBlackHoleNumberTwo);
            currentBlackHoleNumberTwo = null;
        }

        remainingSpawnTimes = new List<float>(interferenceSpawnTimes);
        availablePositions = new List<Transform>(interferencePositions);
    }
    #endregion

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
            // 清理所有非完全体敌人
            List<GameObject> enemiesToDestroy = new List<GameObject>();
            foreach (GameObject enemy in EnemyObjects)
            {
                if (enemy != null)
                {
                    EnemyGrowth growth = enemy.GetComponent<EnemyGrowth>();
                    // 如果不是完全体或不在最终位置，则销毁
                    if (growth == null || !growth.IsInFinalPosition())
                    {
                        enemiesToDestroy.Add(enemy);
                    }
                }
            }

            // 销毁收集到的敌人
            foreach (GameObject enemy in enemiesToDestroy)
            {
                if (activeEnemies.Contains(enemy))
                {
                    activeEnemies.Remove(enemy);
                }
                if (EnemyObjects.Contains(enemy))
                {
                    EnemyObjects.Remove(enemy);
                }
                Destroy(enemy);
            }

            Debug.Log("游戏胜利！得分：" + currentScore);
            DOVirtual.DelayedCall(3f, () => UIController.IsGameEvent());
            onGameWin?.Invoke();
        }
        else
        {
            // 清理所有非完全体敌人
            List<GameObject> enemiesToDestroy = new List<GameObject>();
            foreach (GameObject enemy in EnemyObjects)
            {
                if (enemy != null)
                {
                    EnemyGrowth growth = enemy.GetComponent<EnemyGrowth>();
                    // 如果不是完全体或不在最终位置，则销毁
                    if (growth == null || !growth.IsInFinalPosition())
                    {
                        enemiesToDestroy.Add(enemy);
                    }
                }
            }

            // 销毁收集到的敌人
            foreach (GameObject enemy in enemiesToDestroy)
            {
                if (activeEnemies.Contains(enemy))
                {
                    activeEnemies.Remove(enemy);
                }
                if (EnemyObjects.Contains(enemy))
                {
                    EnemyObjects.Remove(enemy);
                }
                Destroy(enemy);
            }

            Debug.Log("游戏失败！得分：" + currentScore);
            DOVirtual.DelayedCall(3f, () => UIController.IsGameEvent());
            onGameLose?.Invoke();
        }
    }

    #region scene视图区域绘制，方便调试
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
    #endregion
}
