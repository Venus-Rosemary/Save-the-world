using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameManage : MonoBehaviour
{
    public GameObject BlackHolePrefabVFX;//黑洞传送门特效预制体
    private GameObject BlackHole;
    public GameObject EnemyPrefab;//敌人预制体
    private GameObject Enemy;
    private List<GameObject> AllCanAttackEnemy = new List<GameObject>();

    [SerializeField] private Vector2 CreatBoundaryMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 CreatBoundaryMax = new Vector2(20f, 20f);

    [Header("最终形态设置")]
    public List<Transform> finalPos = new List<Transform>();
    void Start()
    {
        OneCreat();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    void OneCreat()//黑洞生成
    {
        float RangePosX = Random.Range(CreatBoundaryMin.x, CreatBoundaryMax.x);
        float RangePosZ = Random.Range(CreatBoundaryMin.y, CreatBoundaryMax.y);
        Vector3 RangePos = new Vector3(RangePosX, 1.5F, RangePosZ);
        BlackHole = Instantiate(BlackHolePrefabVFX, RangePos, Quaternion.identity);
        //BlackHole.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        BlackHole.transform.DOScale(0.3f, 1).From();
        StartCoroutine(CreatEnemy());
    }

    void TwoCreat()//敌人在黑洞处生成
    {
        Enemy = Instantiate(EnemyPrefab, BlackHole.transform.position, Quaternion.identity);
        Enemy.GetComponent<EnemyFSM>().ChangeState(EnemyState.Patrol);
        EnemyGrowth enemyGrowth = Enemy.GetComponent<EnemyGrowth>();
        enemyGrowth.SetFindAFP(finalPos);
        AllCanAttackEnemy.Add(Enemy);
    }

    IEnumerator CreatEnemy()
    {
        int a = 5;
        for (int i = 0; i < a; i++)
        {
            yield return new WaitForSeconds(3);
            TwoCreat();
        }
        yield return new WaitForSeconds(2);

        BlackHole.transform.DOScale(0.3f, 1);
        yield return new WaitForSeconds(1);
        Destroy(BlackHole);
    }

    private void OnDrawGizmos()
    {
        // 绘制移动边界
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
