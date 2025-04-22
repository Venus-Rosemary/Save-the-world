using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using static UnityEngine.GraphicsBuffer;

public class FireballShooter : MonoBehaviour
{
    [Header("目标设置")]
    [SerializeField] private Transform target; // 目标（玩家）
    [SerializeField] private float PlayerHight;//模型高度

    [Header("发射设置")]
    [SerializeField] private float minFireInterval = 10f; // 最小发射间隔
    [SerializeField] private float maxFireInterval = 15f; // 最大发射间隔
    [SerializeField] private Transform firePoint; // 发射点
    [SerializeField] private float fireballSpeed = 10f; // 火球速度
    [SerializeField] private int fireballDamage = 20; // 火球伤害

    [Header("预制体")]
    [SerializeField] private GameObject fireballPrefab; // 火球预制体
    [SerializeField] private GameObject groundVFXPrefab; // 地面效果预制体
    [SerializeField] private GameObject fireHitPlayerVFX;//火球击中玩家特效
    [SerializeField] private GameObject fireHitGroundVFX;//火球击中地面特效

    [Header("地面效果设置")]
    [SerializeField] private float minGroundVFXScale = 0.3f; // 最小地面效果缩放
    [SerializeField] private float maxGroundVFXScale = 2f; // 最大地面效果缩放
    [SerializeField] private LayerMask groundLayer; // 地面层

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float arrivalDistance = 0.1f;

    [Header("动画设置")]
    [SerializeField] private Animator animator;
    private bool isRun = true;


    private Transform targetPosition;
    private bool hasArrived = false;


    private float nextFireTime;
    
    private void Start()
    {
        // 如果没有指定目标，尝试查找玩家
        if (target == null)
        {
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            if (target == null)
            {
                Debug.LogError("FireballShooter未找到目标，请手动设置目标！");
                enabled = false;
                return;
            }
        }
        
        // 如果没有指定发射点，使用自身位置
        if (firePoint == null)
        {
            firePoint = transform;
        }
        
        // 设置第一次发射时间
        SetNextFireTime();
    }

    //设置移动目标点
    public void SetTargetPosition(Transform target)
    {
        targetPosition = target;
    }

    private void Update()
    {
        // 检查是否到达发射时间
        if (Time.time >= nextFireTime)
        {
            FireAtTarget();
            SetNextFireTime();
        }


        if (targetPosition != null && !hasArrived)
        {
            // 移动向目标位置
            if (isRun)
            {
                animator.Play("Run");
                isRun= false;
            }
            Vector3 direction = (targetPosition.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.LookAt(targetPosition);
        }
        // 检查是否到达目标位置
        if (Vector3.Distance(transform.position, targetPosition.position) < arrivalDistance)
        {
            transform.position = targetPosition.position;
            hasArrived = true;
            LookAtTarget();
        }

    }
    
    private void LookAtTarget()// 始终面向玩家
    {
        if (target != null)
        {
            // 计算朝向玩家的方向（忽略Y轴）
            Vector3 targetPosition = new Vector3(target.position.x, target.position.y, target.position.z);
            transform.LookAt(targetPosition);
        }
    }
    
    private void FireAtTarget()//向目标位置发射火球
    {
        if (target == null || fireballPrefab == null) return;

        //npc警示
        GameManage.Instance.NPCEventTrigger(5);

        Vector3 PlayerXZ = new Vector3(target.position.x, target.position.y - (PlayerHight / 2), target.position.z);
        // 计算发射方向
        Vector3 fireDirection = (PlayerXZ - firePoint.position).normalized;
        animator.Play("Attack");
        // 创建火球
        GameObject fireball = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        
        GameManage.Instance.OnAddEnemyObjects(fireball);

        // 计算火球射线与地面的交点
        Ray ray = new Ray(firePoint.position, fireDirection);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            // 启动火球飞行协程
            StartCoroutine(FireballFlight(fireball, fireDirection, hit.point));
        }
        else
        {
            // 如果没有检测到地面，直接销毁火球
            GameManage.Instance.OnRemoveEnemyObjects(fireball);
            Destroy(fireball);
            Debug.LogWarning("未检测到地面，火球已销毁");
        }
    }
    
    private IEnumerator FireballFlight(GameObject fireball, Vector3 direction, Vector3 groundHitPoint)
    {
        // 火球飞行距离
        float totalDistance = Vector3.Distance(fireball.transform.position, groundHitPoint);
        float currentDistance = 0f;
        
        // 创建地面效果
        GameObject groundVFX = null;
        if (groundVFXPrefab != null)
        {
            groundVFX = Instantiate(groundVFXPrefab, groundHitPoint, Quaternion.identity);
            groundVFX.transform.localScale = Vector3.one * minGroundVFXScale;
            GameManage.Instance.OnAddEnemyObjects(groundVFX);
        }
        
        // 火球飞行
        while (fireball != null && currentDistance < totalDistance)
        {
            // 移动火球
            fireball.transform.position += direction * fireballSpeed * Time.deltaTime;
            
            // 更新当前距离
            currentDistance = Vector3.Distance(firePoint.position, fireball.transform.position);
            
            // 计算到地面的距离比例
            float distanceRatio = currentDistance / totalDistance;
            
            // 更新地面效果缩放
            if (groundVFX != null)
            {
                float scale = Mathf.Lerp(minGroundVFXScale, maxGroundVFXScale, distanceRatio);
                groundVFX.transform.localScale = Vector3.one * scale;
            }
            
            // 检查是否击中玩家
            Collider[] hitColliders = Physics.OverlapSphere(fireball.transform.position, 0.5f);
            foreach (Collider collider in hitColliders)
            {
                if (collider.CompareTag("Player"))
                {
                    Health playerHealth = collider.GetComponent<Health>();
                    PlayerController playerController = collider.GetComponent<PlayerController>();

                    GameObject aa=null;

                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(fireballDamage);
                        Debug.Log("火球击中玩家，造成 " + fireballDamage + " 点伤害");
                        aa = Instantiate(fireHitPlayerVFX,collider.transform.position,Quaternion.identity);
                        GameManage.Instance.OnAddEnemyObjects(aa);
                    }
                    
                    // 禁用玩家控制1秒
                    if (playerController != null)
                    {
                        StartCoroutine(DisablePlayerControl(playerController));
                    }

                    // 提前销毁火球
                    if (groundVFX != null) 
                    {
                        GameManage.Instance.OnRemoveEnemyObjects(groundVFX);
                        Destroy(groundVFX); 
                    }
                    if (aa != null)
                    {
                        GameManage.Instance.OnRemoveEnemyObjects(aa);
                        DOVirtual.DelayedCall(1, () => Destroy(aa)); // 延迟关闭
                    }
                    GameManage.Instance.OnRemoveEnemyObjects(fireball);
                    Destroy(fireball);
                    yield break;
                }
            }
            
            yield return null;
        }
        GameObject hitGorundVFX = Instantiate(fireHitGroundVFX, groundHitPoint, Quaternion.identity);
        // 火球到达地面，销毁火球和地面效果
        GameManage.Instance.OnAddEnemyObjects(hitGorundVFX);


        GameManage.Instance.OnRemoveEnemyObjects(fireball);
        if (fireball != null) Destroy(fireball);

        GameManage.Instance.OnRemoveEnemyObjects(groundVFX);
        if (groundVFX != null) Destroy(groundVFX);
        if (hitGorundVFX!=null)
        {
            GameManage.Instance.OnRemoveEnemyObjects(hitGorundVFX);
            DOVirtual.DelayedCall(1, () => Destroy(hitGorundVFX)); // 延迟关闭
        }
    }
    
    // 禁用玩家控制的协程
    private IEnumerator DisablePlayerControl(PlayerController playerController)
    {
        playerController.SetCanControl(false);
        yield return new WaitForSeconds(1f);
        playerController.SetCanControl(true);
    }
    
    private void SetNextFireTime()
    {
        // 设置下一次发射时间
        nextFireTime = Time.time + Random.Range(minFireInterval, maxFireInterval);
    }
    
    private void OnDrawGizmos()//绘制线条方便调试
    {
        // 绘制发射点
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
        
        // 绘制到目标的线
        if (target != null && firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(firePoint.position, target.position);
        }
    }
}