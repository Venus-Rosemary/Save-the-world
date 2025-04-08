using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
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
    [SerializeField] private float jumpDuration = 0.5f;
    [SerializeField] private AnimationCurve jumpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool canAttack = true;
    private bool isJumping = false;
    private Vector3 originalPosition;
    private float jumpStartTime;

    private void Update()
    {
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
            
            Debug.Log("攻击命中: " + enemy.name);
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
        jumpStartTime = Time.time;
    }

    private void UpdateJump()
    {
        float jumpProgress = (Time.time - jumpStartTime) / jumpDuration;
        
        if (jumpProgress >= 1f)
        {
            // 跳跃结束
            isJumping = false;
            return;
        }

        // 使用动画曲线计算当前高度
        float heightFactor = jumpCurve.Evaluate(jumpProgress);
        Vector3 currentPosition = transform.position;
        currentPosition.y = originalPosition.y + (jumpHeight * heightFactor);
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
}