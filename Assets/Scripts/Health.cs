using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    public UnityEvent onDeath;
    public UnityEvent<int, int> onHealthChanged; // 参数：当前生命值，最大生命值

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // 触发生命值变化事件
        onHealthChanged?.Invoke(currentHealth, maxHealth);

        // 如果是敌人，通知状态机进入受击状态
        EnemyFSM enemyFSM = GetComponent<EnemyFSM>();
        if (enemyFSM != null)
        {
            enemyFSM.TakeDamage();
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // 触发死亡事件
        onDeath?.Invoke();
        
        // 可以在这里添加死亡逻辑，比如播放死亡动画、销毁对象等
        Debug.Log(gameObject.name + " 已死亡");
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        
        // 触发生命值变化事件
        onHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // 添加获取当前生命值的方法
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
}