using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class NPCController : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private Vector3 initialPosition; // NPC初始位置
    [SerializeField] private Transform helpPositions; // NPC可以移动到的特定点
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackDelay = 1f;

    public Animator animator;

    private int enemiesDefeated = 0;
    private bool isHelping = false;
    private GameManage gameManager;
    private Transform player; //玩家引用

    private void Start()
    {
        gameManager = GameManage.Instance;
        initialPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform; // 获取玩家引用
        // 监听NPC帮助事件
        if (gameManager != null)
        {
            gameManager.onNpcHelp.AddListener(StartHelping);
        }
    }

    private void OnEnable()
    {
    }

    private void Update()
    {
        // 非帮助状态时看向玩家
        if (!isHelping && player != null)
        {
            Vector3 directionToPlayer = player.position - transform.position;
            directionToPlayer.y = 0; // 保持Y轴不变
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    private void StartHelping(int helpCount)
    {
        Debug.Log(isHelping);
        if (!isHelping)
        {
            isHelping = true;
            enemiesDefeated = 0;
            StartCoroutine(HelpRoutine());
        }
    }

    private IEnumerator HelpRoutine()
    {
        yield return MoveToPosition(initialPosition);
        transform.position= initialPosition;
        // 先转向目标位置
        Vector3 direction = (helpPositions.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(direction);

        yield return MoveToPosition(helpPositions.position);

        animator.Play("Idle");

        yield return new WaitForSeconds(0.5f);
        while (enemiesDefeated < 3 && isHelping)
        {

            // 2. 寻找最近的敌人
            GameObject nearestEnemy = FindNearestEnemy();
            if (nearestEnemy != null)
            {
                // 3. 移动到敌人附近
                yield return MoveToPosition(nearestEnemy.transform.position + new Vector3(0, 1, 0) + Vector3.right * attackRange);
                
                // 4. 攻击敌人
                yield return AttackEnemy(nearestEnemy);
                
                enemiesDefeated++;
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                enemiesDefeated = 3;
                isHelping = false;
            }
            
            yield return new WaitForSeconds(0.5f);
        }

        // 5. 返回初始位置
        yield return MoveToPosition(initialPosition);
        animator.Play("Idle");
        isHelping = false;
    }

    private IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        // 计算移动时间
        float moveDuration = Vector3.Distance(transform.position, targetPosition) / moveSpeed;

        animator.Play("Run");

        // 使用DOTween实现平滑移动和旋转
        transform.DOMove(targetPosition, moveDuration)
            .SetEase(Ease.InOutQuad);

        // 在移动过程中更新朝向
        float elapsed = 0;
        Vector3 startPos = transform.position;
        
        while (elapsed < moveDuration)
        {
            // 计算当前移动方向
            Vector3 currentDirection = (targetPosition - transform.position).normalized;
            if (currentDirection != Vector3.zero)
            {
                // 平滑旋转到移动方向
                Quaternion targetRotation = Quaternion.LookRotation(currentDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private GameObject FindNearestEnemy()
    {
        GameObject nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (GameObject enemy in gameManager.OnAcquireActiveEnemies())
        {
            if (enemy != null)
            {
                float distance = Vector3.Distance(transform.position, enemy.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = enemy;
                }
            }
        }

        return nearestEnemy;
    }

    private IEnumerator AttackEnemy(GameObject enemy)
    {
        if (enemy != null)
        {
            // 可以添加攻击动画
            transform.DOLookAt(enemy.transform.position, 0.2f);


            animator.Play("Attack");
            yield return new WaitForSeconds(attackDelay);
            
            // 获取敌人的Health组件并造成伤害
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(enemyHealth.GetCurrentHealth(), "NPC"); // 添加来源标识
            }
        }
    }

    private void OnDisable()
    {
        // 清理DOTween
        DOTween.Kill(transform);
        isHelping = false;
        transform.position = initialPosition;

    }
}