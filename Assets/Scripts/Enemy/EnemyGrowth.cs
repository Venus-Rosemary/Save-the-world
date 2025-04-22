using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class EnemyGrowth : MonoBehaviour
{
    [System.Serializable]
    public class GrowthStage
    {
        public float scale;//缩放尺寸
        public int health;//对应形态血量
        public Vector2 duration; // x: 最小持续时间, y: 最大持续时间
        public float attackRangeToModel;//对应模型的攻击距离
        public GameObject model; // 对应阶段的模型
        public RuntimeAnimatorController animatorController; // 动画控制器
        public Avatar avatar; // 模型Avatar
    }

    [Header("成长阶段设置")]
    [SerializeField] private List<GrowthStage> growthStages = new List<GrowthStage>();
    private Animator animator;
    private GameObject currentModel;

    [Header("最终形态设置")]
    [SerializeField] private List<Transform> finalPositions = new List<Transform>();//最终形态移动到的位置------5个
    [SerializeField] private float moveSpeed = 5f;

    private int currentStage = 0;//目前阶段
    private Health healthComponent;//血量脚本组件
    private EnemyFSM enemyFSM;//状态机脚本组件
    private Vector3 originalScale;//最初缩放
    private Transform targetPosition;//目标位置
    private bool isMovingToFinalPosition = false;//完全体移动到最后位置标志
    private bool isEndPos = false;

    public Outline outL;

    private void Awake()
    {
        //获取组件和缩放
        healthComponent = GetComponent<Health>();
        enemyFSM = GetComponent<EnemyFSM>();
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;//new Vector3(1,1,1)
    }

    private void Start()
    {
        foreach (var stage in growthStages)
        {
            stage.model.SetActive(false);
        }
        // 设置初始阶段
        SetGrowthStage(0);
        
        // 开始成长过程
        StartCoroutine(GrowthProcess());
    }

    private void Update()
    {
        // 如果处于最终形态并且正在移动到目标位置
        if (isMovingToFinalPosition && targetPosition != null)
        {
            // 计算移动方向和距离
            Vector3 TargetP = new Vector3(targetPosition.position.x, transform.position.y, targetPosition.position.z);
            Vector3 direction = (TargetP - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.LookAt(TargetP);

            // 如果已经接近目标位置，则停止移动
            if (Vector2Distance(transform.position, targetPosition.position) < 0.1f)
            {
                transform.position = TargetP;
                isMovingToFinalPosition = false;
                isEndPos = true;
            }
            //始终看向玩家(没法放这里)
        }

        if (isEndPos)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 playerPosition = player.transform.position;
                Vector3 directionToPlayer = playerPosition - transform.position;
                directionToPlayer.y = 0; // 忽略y轴，保持水平旋转
                
                // 平滑旋转面向玩家
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
            }
        }
    }

    private IEnumerator GrowthProcess()
    {
        // 遍历除最后一个阶段外的所有成长阶段
        for (int i = 0; i < growthStages.Count - 1; i++)
        {
            // 等待当前阶段的随机持续时间
            float stageDuration = Random.Range(growthStages[i].duration.x, growthStages[i].duration.y);
            yield return new WaitForSeconds(stageDuration);
            
            // 进入下一个成长阶段
            SetGrowthStage(i + 1);
        }
        
        // 进入最终形态
        EnterFinalStage();
    }

    private void SetGrowthStage(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= growthStages.Count)
            return;

        currentStage = stageIndex;
        GrowthStage stage = growthStages[currentStage];

        // 切换模型
        if (currentModel != null)
        {
            currentModel.SetActive(false);
        }
        currentModel = stage.model;
        if (currentModel != null)
        {
            currentModel.SetActive(true);
        }
        if (enemyFSM!=null)
        {
            enemyFSM.SetAttackRangeToModel(stage.attackRangeToModel);
        }

        // 切换动画控制器和Avatar
        if (animator != null && stage.animatorController != null)
        {
            animator.runtimeAnimatorController = stage.animatorController;
            animator.avatar = stage.avatar;

            animator.Play("Idle");
        }

        if (stageIndex == 0)
        {
            transform.localScale = originalScale * stage.scale;
        }
        else
        {
            transform.DOScale(originalScale * stage.scale, 1f);

            if (stageIndex == 2)
            {
                GameManage.Instance.NPCEventTrigger(7);
            }

            outL.enabled = true;
            DOVirtual.DelayedCall(1, () => outL.enabled = false);
        }

        if (healthComponent != null)
        {
            healthComponent.SetMaxHealth(stage.health);
        }
    }

    private void EnterFinalStage()
    {
        GameManage.Instance.NPCEventTrigger(8);

        // 禁用敌人状态机
        if (enemyFSM != null)
        {
            enemyFSM.ChangeState(EnemyState.Idle);
            enemyFSM.Restorematerial();
            enemyFSM.enabled = false;
            Destroy(enemyFSM);
        }
        if (healthComponent!=null)
        {
            healthComponent.enabled = false;
            Destroy(healthComponent);
        }
        
        // 查找可用的最终位置
        FindAvailableFinalPosition();

        GameManage.Instance.OnOtherScriptUse(gameObject);

        // 如果找到了可用位置，开始移动
        if (targetPosition != null)
        {
            isMovingToFinalPosition = true;
            Debug.Log($"{gameObject.name} 进入最终形态，正在移动到位置: {targetPosition.name}");
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} 进入最终形态，但没有可用的最终位置");
        }
    }

    // 添加到EnemyGrowth.cs中
    
    // 设置最终位置列表
    public void SetFindAFP(List<Transform> positions)
    {
        finalPositions = positions;
    }
    
    // 检查是否处于最终位置
    public bool IsInFinalPosition()
    {
        return isEndPos;
    }

    private void FindAvailableFinalPosition()
    {
        if (finalPositions.Count == 0)
            return;
            
        // 查找所有怪物
        EnemyGrowth[] allEnemies = FindObjectsOfType<EnemyGrowth>();
        
        // 检查每个最终位置是否已被占用
        foreach (Transform position in finalPositions)
        {
            bool isOccupied = false;
            
            foreach (EnemyGrowth enemy in allEnemies)
            {
                // 跳过自己
                if (enemy == this)
                    continue;
                    
                // 如果敌人已经在最终位置或正在移动到该位置
                if (enemy.targetPosition == position || 
                    (enemy.isMovingToFinalPosition && Vector3.Distance(enemy.transform.position, position.position) < 1f))
                {
                    isOccupied = true;
                    break;
                }
            }
            
            // 如果位置未被占用，选择该位置
            if (!isOccupied)
            {
                targetPosition = position;
                return;
            }
        }
    }

    // 计算两点在xz平面上的距离（忽略y轴）
    private float Vector2Distance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}