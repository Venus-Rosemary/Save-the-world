using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManage : MonoBehaviour
{
    public GameObject BlackHolePrefabVFX;//�ڶ���������ЧԤ����
    private GameObject BlackHole;
    public GameObject EnemyPrefab;//����Ԥ����
    private GameObject Enemy;
    private List<GameObject> AllCanAttackEnemy = new List<GameObject>();

    [SerializeField] private Vector2 CreatBoundaryMin = new Vector2(-20f, -20f);
    [SerializeField] private Vector2 CreatBoundaryMax = new Vector2(20f, 20f);
    void Start()
    {
        OneCreat();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    void OneCreat()//�ڶ�����
    {
        float RangePosX = Random.Range(CreatBoundaryMin.x, CreatBoundaryMax.x);
        float RangePosZ = Random.Range(CreatBoundaryMin.y, CreatBoundaryMax.y);
        Vector3 RangePos = new Vector3(RangePosX, 1.5F, RangePosZ);
        BlackHole = Instantiate(BlackHolePrefabVFX, RangePos, Quaternion.identity);
        StartCoroutine(CreatEnemy());
    }

    void TwoCreat()//�����ںڶ�������
    {
        Enemy = Instantiate(EnemyPrefab, BlackHole.transform.position, Quaternion.identity);
        Enemy.GetComponent<EnemyFSM>().ChangeState(EnemyState.Patrol);
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

    }

    private void OnDrawGizmos()
    {
        // �����ƶ��߽�
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
