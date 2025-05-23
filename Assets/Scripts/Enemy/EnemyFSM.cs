using DG.Tweening;
using System.Collections;
using UnityEngine;

public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Hit,
    Die
}

[RequireComponent(typeof(Health))]
public class EnemyFSM : MonoBehaviour
{
    [Header("状态设置")]
    [SerializeField] private EnemyState currentState = EnemyState.Idle;//当前状态
    [SerializeField] private float idleTime = 2f;//待机时间
    
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float patrolRadius = 10f;//巡逻半径
    [SerializeField] private Vector2 moveBoundaryMin = new Vector2(-20f, -20f);//限制范围
    [SerializeField] private Vector2 moveBoundaryMax = new Vector2(20f, 20f);
    [SerializeField] private float minPatrolDistance = 3f;//巡逻距离
    [SerializeField] private float maxPatrolDistance = 8f;
    
    [Header("检测设置")]
    [SerializeField] private float detectionRange = 8f;//检测范围
    [SerializeField] private float attackRange = 2f;//攻击范围
    [SerializeField] private LayerMask playerLayer;
    
    [Header("攻击设置")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackCooldown = 1.5f;//攻击冷却时间
    [SerializeField] private float attackDuration = 0.5f;//攻击持续时间
    [SerializeField] private float attackWindup = 1f; // 新增：攻击前摇时间
    
    [Header("受击设置")]
    [SerializeField] private float hitStunDuration = 0.5f;//受击昏迷持续时间
    [SerializeField] private GameObject hitVFX;//受击特效
    
    // 组件引用
    private Health health;
    private Transform player;
    private Renderer enemyRenderer;
    private Material defaultMaterial;
    private Animator animator;
    
    // 状态控制
    private float stateTimer;//状态计时器
    private Vector3 moveDirection;//移动方向向量
    private float patrolDistance;//巡逻距离
    private float distanceTraveled;//距离
    private bool canAttack = true;
    private Coroutine hitCoroutine;
    private Coroutine attackCoroutine; //攻击协程引用

    private void Awake()
    {
        health = GetComponent<Health>();
        enemyRenderer = GetComponentInChildren<Renderer>();
        animator=GetComponent<Animator>();
        if (enemyRenderer != null)
        {
            defaultMaterial = enemyRenderer.material;
        }
        

        // 初始化状态
        ChangeState(EnemyState.Idle);
    }

    public void Restorematerial()
    {
        if (enemyRenderer != null)
        {
            defaultMaterial = enemyRenderer.material;
        }
    }

    private void Start()
    {
        // 查找玩家
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        // 注册受击和死亡事件
        health.onDeath.AddListener(OnDeath);
        

    }

    private void Update()
    {
        if (currentState == EnemyState.Die) return;

        if (player == null && currentState == EnemyState.Chase) 
        {
            //玩家死亡，且为追逐状态，切换为巡逻状态
            ChangeState(EnemyState.Patrol);
            return;
        }
        
        // 根据当前状态执行相应的行为
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdleState();//待机
                break;
            case EnemyState.Patrol:
                UpdatePatrolState();//巡逻
                break;
            case EnemyState.Chase:
                UpdateChaseState();//追逐
                break;
            case EnemyState.Attack://攻击
                UpdateAttackState();
                break;
            case EnemyState.Hit:
                // 受击状态在协程中处理
                break;
            case EnemyState.Die:
                // 死亡状态不需要更新
                break;
        }
        
        // 检测玩家，检测到在警戒范围内，则进入追逐状态，除非当前正在受击或死亡
        if (currentState != EnemyState.Hit && currentState != EnemyState.Die && player != null)
        {
            DetectPlayer();
        }
    }

    private float Vector2Distance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    public void SetAttackRangeToModel(float a)
    {
        attackRange = a;
    }

    #region 状态更新方法

    private void UpdateIdleState()//待机状态->巡逻状态
    {
        stateTimer -= Time.deltaTime;
        animator.Play("Idle");
        if (stateTimer <= 0)
        {
            ChangeState(EnemyState.Patrol);
        }
    }
    
    private void UpdatePatrolState()//巡逻状态->待机状态
    {
        animator.Play("Run");
        // 沿当前方向移动
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        distanceTraveled += moveSpeed * Time.deltaTime;
        
        // 检查是否到达目标距离或超出边界
        if (distanceTraveled >= patrolDistance || IsOutOfBounds())
        {
            //Debug.Log($"{distanceTraveled >= patrolDistance}   {IsOutOfBounds()}");
            // 重新选择一个方向和距离
            ChangeState(EnemyState.Idle);
        }
    }
    
    private void UpdateChaseState()//追逐状态->巡逻/攻击状态
    {
        if (player == null) return;

        animator.Play("Run");

        // 计算朝向玩家的方向
        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0;
        directionToPlayer.Normalize();
        
        // 平滑旋转朝向玩家
        Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // 检查是否进入攻击范围
        float distanceToPlayer = Vector2Distance(transform.position, player.position);

        if (distanceToPlayer > attackRange)
        {
            // 向玩家移动
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
        }

        if (distanceToPlayer <= attackRange && canAttack)
        {
            ChangeState(EnemyState.Attack);

            animator.Play("Idle");
        }
        // 如果玩家超出检测范围，返回巡逻
        else if (distanceToPlayer > detectionRange * 1.2f)//超出警戒范围的1.2倍，变回巡逻状态
        {
            ChangeState(EnemyState.Patrol);
        }
    }
    
    private void UpdateAttackState()//攻击状态->追逐/巡逻
    {
        // 攻击状态在协程中处理
        stateTimer -= Time.deltaTime;
        
        if (stateTimer <= 0)
        {
            // 攻击结束后，根据与玩家的距离决定下一个状态
            if (player != null)
            {
                float distanceToPlayer = Vector2Distance(transform.position, player.position);
                if (distanceToPlayer <= detectionRange)
                {
                    if (distanceToPlayer<= attackRange && canAttack)
                    {
                        ChangeState(EnemyState.Attack);
                    }
                    else if(distanceToPlayer > attackRange)
                    {
                        ChangeState(EnemyState.Chase);
                    }
                }
                else
                {
                    ChangeState(EnemyState.Patrol);
                }
            }
            else
            {
                ChangeState(EnemyState.Patrol);
            }
        }
    }

    #endregion

    #region 状态转换和行为

    public void ChangeState(EnemyState newState)
    {
        // 退出当前状态
        ExitState(currentState);
        
        // 进入新状态
        currentState = newState;
        EnterState(newState);
        
        //Debug.Log($"{gameObject.name} 切换到 {newState} 状态");
    }
    
    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                stateTimer = idleTime;
                break;
                
            case EnemyState.Patrol:
                // 选择一个随机方向
                float randomAngle = Random.Range(0, 360);
                moveDirection = new Vector3(
                    Mathf.Sin(randomAngle * Mathf.Deg2Rad),
                    0,
                    Mathf.Cos(randomAngle * Mathf.Deg2Rad)
                );
                
                // 设置朝向
                transform.forward = moveDirection;
                
                // 选择一个随机距离
                patrolDistance = Random.Range(minPatrolDistance, maxPatrolDistance);
                distanceTraveled = 0f;


                break;
                
            case EnemyState.Chase:
                // 追踪状态在Update中处理
                break;
                
            case EnemyState.Attack:
                stateTimer = attackDuration;
                attackCoroutine = StartCoroutine(PerformAttack());
                break;
                
            case EnemyState.Hit:
                // 受击状态在TakeDamage中处理
                break;
                
            case EnemyState.Die:
                // 死亡状态在OnDeath中处理
                break;
        }
    }
    
    private void ExitState(EnemyState state)
    {
        // 退出状态时的清理工作
        switch (state)
        {
            case EnemyState.Idle:
                break;
                
            case EnemyState.Patrol:
                break;
                
            case EnemyState.Chase:
                break;
                
            case EnemyState.Attack:
                if (attackCoroutine != null)
                {
                    StopCoroutine(attackCoroutine);
                    canAttack = true;
                    attackCoroutine = null;
                }
                break;
                
            case EnemyState.Hit:
                // 恢复默认材质
                if (enemyRenderer != null && defaultMaterial != null)
                {
                    enemyRenderer.material = defaultMaterial;
                }
                break;
                
            case EnemyState.Die:
                break;
        }
    }
    
    #endregion
    
    #region 行为方法
    
    private bool IsOutOfBounds()
    {
        Vector3 position = transform.position;
        return position.x < moveBoundaryMin.x || position.x > moveBoundaryMax.x ||
               position.z < moveBoundaryMin.y || position.z > moveBoundaryMax.y;
    }
    
    private void DetectPlayer()//进入追逐状态
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2Distance(transform.position, player.position);
        
        // 如果玩家在检测范围内，切换到追踪状态
        if (distanceToPlayer <= detectionRange && currentState != EnemyState.Chase && currentState != EnemyState.Attack)
        {
            ChangeState(EnemyState.Chase);
        }
    }
    
    private IEnumerator PerformAttack()
    {
        canAttack = false;
        
        // 转向玩家
        if (player != null)
        {
            Vector3 directionToPlayer = player.position - transform.position;
            directionToPlayer.y = 0;
            transform.forward = directionToPlayer.normalized;
            
            // 等待攻击前摇时间
            float windupTimer = attackWindup;
            while (windupTimer > 0 && currentState == EnemyState.Attack)
            {
                // 在前摇期间持续更新朝向
                directionToPlayer = player.position - transform.position;
                directionToPlayer.y = 0;
                transform.forward = directionToPlayer.normalized;
                
                windupTimer -= Time.deltaTime;
                yield return null;
            }

            // 如果状态已经改变，取消攻击
            if (currentState != EnemyState.Attack)
            {
                canAttack = true;
                yield break;
            }
            
            // 检测玩家是否在攻击范围内
            if (Vector2Distance(transform.position, player.position) <= attackRange)
            {
                // 对玩家造成伤害
                Health playerHealth = player.GetComponent<Health>();
                if (playerHealth != null)
                {
                    // 延迟一小段时间再造成伤害，模拟攻击动画
                    animator.Play("Attack");
                    yield return new WaitForSeconds(attackDuration * 0.5f);
                    playerHealth.TakeDamage(attackDamage);
                    GameManage.Instance.NPCEventTrigger(6);
                    Debug.Log($"{gameObject.name} 攻击玩家，造成 {attackDamage} 点伤害");
                }
            }
        }
        
        // 攻击冷却
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }
    
    public void TakeDamage()
    {
        // 如果已经死亡，不再处理受击
        if (currentState == EnemyState.Die) return;


        Camera.main.transform.DOShakePosition(0.3f, 0.3f);//摄像机晃动

        // 取消之前的受击协程（如果有）
        if (hitCoroutine != null)
        {
            StopCoroutine(hitCoroutine);
        }
        hitVFX.SetActive(false);
        // 开始新的受击协程
        hitCoroutine = StartCoroutine(HitStun());
    }
    
    private IEnumerator HitStun()
    {
        // 切换到受击状态
        ChangeState(EnemyState.Hit);

        animator.Play("Hit");
        hitVFX.SetActive(true);
        
        // 受击硬直
        yield return new WaitForSeconds(hitStunDuration);

        // 如果生命值大于0，恢复到追踪状态或巡逻状态
        if (health.GetCurrentHealth() > 0)
        {
            if (player != null && Vector2Distance(transform.position, player.position) <= detectionRange)
            {
                if (Vector2Distance(transform.position, player.position) <= attackRange && canAttack)
                {
                    ChangeState(EnemyState.Attack);
                }
                else if (Vector2Distance(transform.position, player.position) > attackRange)
                {
                    ChangeState(EnemyState.Chase);
                }
            }
            else
            {
                ChangeState(EnemyState.Patrol);
            }
        }
    }
    
    private void OnDeath()
    {
        // 切换到死亡状态
        ChangeState(EnemyState.Die);

        animator.Play("Die");
        EnemyGrowth enemyGrowth=GetComponent<EnemyGrowth>();
        if (enemyGrowth != null)
        {
            enemyGrowth.enabled = false;
            Destroy(enemyGrowth);
            enemyGrowth=null;
        }

        gameObject.layer = LayerMask.NameToLayer("Default");

        // 禁用碰撞器
        Collider enemyCollider = GetComponent<Collider>();
        if (enemyCollider != null)
        {
            //enemyCollider.enabled = false;
        }
        
        // 可以在这里添加死亡动画或粒子效果
        
        // 延迟销毁对象
        Destroy(gameObject, 3f);
    }
    
    #endregion
    
    #region 调试辅助
    
    private void OnDrawGizmos()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Vector3 detectR = new Vector3(transform.position.x, 0.5f, transform.position.z);
        Gizmos.DrawWireSphere(detectR, detectionRange);
        
        // 绘制攻击范围
        Gizmos.color = Color.red;
        Vector3 attackR = new Vector3(transform.position.x, 0.5f, transform.position.z);
        Gizmos.DrawWireSphere(attackR, attackRange);
        
        // 绘制移动边界
        Gizmos.color = Color.blue;
        Vector3 boundaryCenter = new Vector3(
            (moveBoundaryMin.x + moveBoundaryMax.x) / 2,
            0.5f,
            (moveBoundaryMin.y + moveBoundaryMax.y) / 2
        );
        Vector3 boundarySize = new Vector3(
            moveBoundaryMax.x - moveBoundaryMin.x,
            0.1f,
            moveBoundaryMax.y - moveBoundaryMin.y
        );
        Gizmos.DrawWireCube(boundaryCenter, boundarySize);
    }
    
    #endregion
}