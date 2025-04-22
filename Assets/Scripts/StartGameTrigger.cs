using UnityEngine;
using UnityEngine.Playables;
using DG.Tweening;

public class StartGameTrigger : MonoBehaviour
{
    public bool isStartrOrReStart = true;
    public PlayableDirector playableDirector;
    public CameraControl cameraControl;
    public Transform targetPoint; // 玩家移动的目标点
    public float moveSpeed = 5f; // 玩家移动速度

    public GameObject LightVFX;
    public Animator animator;
    
    private Health health;
    private bool hasTriggered = false;
    private bool playerInRange = false;
    private GameObject player;
    private PlayerController playerController;
    private bool isMoving = false;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
        }
        else
        {
            Debug.LogError("StartGameTrigger需要Health组件！");
        }
        
        // 获取玩家引用
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
        }
    }

    private void OnEnable()
    {
        health = GetComponent<Health>();
        if (health != null)
        {
            health.onDamaged.AddListener(OnDamaged);
        }
        LightVFX.SetActive(true);
        hasTriggered = false;
        playerInRange = false;
        isMoving = false;
        animator.Play("Idle");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
        }
    }

    private void OnDamaged()
    {
        if (hasTriggered || !playerInRange || isMoving) return;

        if (player != null && playerController != null && targetPoint != null)
        {
            isMoving = true;
            playerController.SetCanControl(false);
            if (cameraControl!=null)
            {
                cameraControl.enabled = false;
            }
            // 使用DOTween移动玩家到目标点
            player.transform.DOMove(targetPoint.position, 1f).OnComplete(() => {
                isMoving = false;
                ExecuteStartLogic();
            });
        }
        else
        {
            ExecuteStartLogic();
        }
    }

    private void ExecuteStartLogic()
    {
        hasTriggered = true;

        if (isStartrOrReStart)
        {
            playableDirector.Play();

            animator.Play("Die");

            Invoke("FinishInvoke", (float)playableDirector.duration);
        }
        else
        {
            if (playerController != null)
            {
                playerController.SetCanControl(true);
            }
            GameManage.Instance.StartGame();
            Destroy(gameObject);
        }
    }

    private void FinishInvoke()
    {
        if (playerController != null)
        {
            playerController.SetCanControl(true);
        }
        cameraControl.enabled = true;
        GameManage.Instance.StartGame();
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.onDamaged.RemoveListener(OnDamaged);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.onDamaged.RemoveListener(OnDamaged);
        }
    }
}