using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    
    [Header("地面效果设置")]
    [SerializeField] private float minGroundVFXScale = 0.3f; // 最小地面效果缩放
    [SerializeField] private float maxGroundVFXScale = 2f; // 最大地面效果缩放
    [SerializeField] private LayerMask groundLayer; // 地面层
    
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
    
    private void Update()
    {
        // 始终面向玩家
        LookAtTarget();
        
        // 检查是否到达发射时间
        if (Time.time >= nextFireTime)
        {
            FireAtTarget();
            SetNextFireTime();
        }
    }
    
    private void LookAtTarget()
    {
        if (target != null)
        {
            // 计算朝向玩家的方向（忽略Y轴）
            Vector3 targetPosition = new Vector3(target.position.x, target.position.y, target.position.z);
            transform.LookAt(targetPosition);
        }
    }
    
    private void FireAtTarget()
    {
        if (target == null || fireballPrefab == null) return;

        Vector3 PlayerXZ = new Vector3(target.position.x, target.position.y - (PlayerHight / 2), target.position.z);
        // 计算发射方向
        Vector3 fireDirection = (PlayerXZ - firePoint.position).normalized;
        
        // 创建火球
        GameObject fireball = Instantiate(fireballPrefab, firePoint.position, Quaternion.identity);
        
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
                    if (playerHealth != null)
                    {
                        playerHealth.TakeDamage(fireballDamage);
                        Debug.Log("火球击中玩家，造成 " + fireballDamage + " 点伤害");
                    }
                    
                    // 提前销毁火球
                    Destroy(fireball);
                    if (groundVFX != null) Destroy(groundVFX);
                    yield break;
                }
            }
            
            yield return null;
        }
        
        // 火球到达地面，销毁火球和地面效果
        if (fireball != null) Destroy(fireball);
        if (groundVFX != null) Destroy(groundVFX);
    }
    
    private void SetNextFireTime()
    {
        // 设置下一次发射时间
        nextFireTime = Time.time + Random.Range(minFireInterval, maxFireInterval);
    }
    
    private void OnDrawGizmosSelected()
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