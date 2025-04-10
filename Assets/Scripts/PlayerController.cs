using System.Collections;
using UnityEngine;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    [Header("控制设置")]
    [SerializeField] private bool canControl = true; // 控制玩家是否可以操作角色
    
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Vector2 moveBoundaryMin = new Vector2(-10f, -10f);
    [SerializeField] private Vector2 moveBoundaryMax = new Vector2(10f, 10f);
    [SerializeField] private Color boundaryColor = Color.red;

    [Header("攻击设置")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int attackDamage = 10;

    [Header("跳跃设置")]
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float playerGravity = 20f; // 玩家重力
    private float verticalVelocity = 0f; // 垂直速度

    [Header("无敌帧设置")]
    [SerializeField] private float invincibleDuration = 1f; // 无敌时间
    [SerializeField] private float blinkInterval = 0.1f; // 闪烁间隔
    [SerializeField] private Color invincibleColor = new Color(1f, 1f, 1f, 0.5f); // 无敌时的颜色

    private bool canAttack = true;
    private bool isJumping = false;
    private Vector3 originalPosition;
    private float jumpStartTime;
    private bool isInvincible = false; // 是否处于无敌状态
    private Health playerHealth; // 玩家的Health组件
    private Outline playerOutLine; // 玩家的渲染器
    private Color originalColor; // 原始颜色

    private void Awake()
    {
        playerHealth = GetComponent<Health>();
        playerOutLine = GetComponent<Outline>();

        if (playerOutLine != null)
        {
            playerOutLine.enabled = false;
        }
        
        // 注册受伤事件
        if (playerHealth != null)
        {
            playerHealth.onDamaged.AddListener(OnPlayerDamaged);
        }
        else
        {
            Debug.LogWarning("未找到Health组件，无敌帧功能可能无法正常工作");
        }
    }

    // 当玩家受伤时调用
    private void OnPlayerDamaged()
    {
        if (!isInvincible)
        {
            StartCoroutine(InvincibleState());
        }
    }

    // 无敌状态协程(后面可以换成屏幕闪红？)
    private IEnumerator InvincibleState()
    {
        isInvincible = true;
        
        // 设置玩家为无敌状态
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(true);
        }
        
        // 闪烁效果
        if (playerOutLine != null)
        {
            float endTime = Time.time + invincibleDuration;
            bool visible = false;
            
            while (Time.time < endTime)
            {
                visible = !visible;

                playerOutLine.enabled = visible ? false : true;
                yield return new WaitForSeconds(blinkInterval);
            }

            // 恢复原始颜色
            playerOutLine.enabled = false;
        }
        else
        {
            // 如果没有渲染器，只等待无敌时间
            yield return new WaitForSeconds(invincibleDuration);
        }
        
        // 取消无敌状态
        if (playerHealth != null)
        {
            playerHealth.SetInvincible(false);
        }
        
        isInvincible = false;
    }

    private void Update()
    {
        // 如果不能控制，直接返回
        if (!canControl) return;

        // 处理移动
        HandleMovement();

        // 处理攻击
        if (Input.GetMouseButtonDown(0) && canAttack)
        {
            Attack();
        }

        // 处理跳跃
        if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
        {
            StartJump();
        }

        // 更新跳跃
        if (isJumping)
        {
            UpdateJump();
        }
    }

    private void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 movement = new Vector3(horizontal, 0.0f, vertical).normalized;
        transform.position += movement * Time.deltaTime * moveSpeed;

        // 限制移动范围
        float clampedX = Mathf.Clamp(transform.position.x, moveBoundaryMin.x, moveBoundaryMax.x);
        float clampedZ = Mathf.Clamp(transform.position.z, moveBoundaryMin.y, moveBoundaryMax.y);
        transform.position = new Vector3(clampedX, transform.position.y, clampedZ);

        // 如果有移动输入，调整朝向
        //if (movement != Vector3.zero)
        //{
        //    transform.forward = new Vector3(horizontal, 0f, vertical).normalized;
        //}
    }

    private void Attack()
    {
        // 开始攻击冷却
        StartCoroutine(AttackCooldown());

        // 播放攻击动画（如果有）
        // animator.SetTrigger("Attack");

        // 检测攻击范围内的敌人
        Collider[] hitEnemies = Physics.OverlapSphere(attackPoint.position, attackRange, enemyLayers);

        // 对每个敌人造成伤害
        foreach (Collider enemy in hitEnemies)
        {
            // 如果敌人有Health组件，则造成伤害
            Health enemyHealth = enemy.GetComponent<Health>();
            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(attackDamage);
            }
            //Camera.main.transform.DOShakePosition(0.3f, 0.6f);//摄像机晃动
        }
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;
        yield return new WaitForSeconds(attackCooldown);
        canAttack = true;
    }

    private void StartJump()
    {
        isJumping = true;
        originalPosition = transform.position;
        
        // 使用物理公式计算初始向上速度
        verticalVelocity = Mathf.Sqrt(jumpHeight * 2 * playerGravity);
    }

    private void UpdateJump()
    {
        // 应用重力
        verticalVelocity -= playerGravity * Time.deltaTime;
        
        // 更新位置
        Vector3 currentPosition = transform.position;
        currentPosition.y += verticalVelocity * Time.deltaTime;
        
        // 检查是否落回地面
        if (currentPosition.y <= originalPosition.y)
        {
            currentPosition.y = originalPosition.y;
            isJumping = false;
            verticalVelocity = 0f;
        }
        
        transform.position = currentPosition;
    }

    private void OnDrawGizmos()
    {
        // 绘制移动边界
        Gizmos.color = boundaryColor;
        Vector3 center = new Vector3(
            (moveBoundaryMin.x + moveBoundaryMax.x) / 2,
            1f,
            (moveBoundaryMin.y + moveBoundaryMax.y) / 2
        );
        Vector3 size = new Vector3(
            moveBoundaryMax.x - moveBoundaryMin.x,
            0.1f,
            moveBoundaryMax.y - moveBoundaryMin.y
        );
        Gizmos.DrawWireCube(center, size);

        // 绘制攻击范围
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
    
    // 公开方法：设置玩家是否可以控制
    public void SetCanControl(bool value)
    {
        canControl = value;
        
        // 如果禁用控制，可能需要停止当前动作
        if (!canControl)
        {
            // 停止跳跃
            isJumping = false;
            
            // 停止攻击冷却
            StopCoroutine(nameof(AttackCooldown));
            canAttack = true;
        }
    }
    
    // 公开方法：获取当前控制状态
    public bool GetCanControl()
    {
        return canControl;
    }
}