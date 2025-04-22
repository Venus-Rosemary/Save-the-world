using UnityEngine;
using TMPro;
using System.Collections.Generic;
using DG.Tweening;

public enum DialogueTriggerType
{
    FirstEnemyKilled,    // 击杀第一个敌人
    FirstWaveCleared,    // 清理第一波敌人
    // 在这里添加更多触发条件...

    RepeatMoreEnemy,
    FirstSecondWave,
    RepeatFastClear,
    ChanceFlyHitTarget,
    ChancePlayerDamageTaken,
    FirstAppearFirstStage3,
    FirstAppearFirstStage4,
    FirstNpcHelp,
}

[System.Serializable]
public class DialogueData
{
    public DialogueTriggerType triggerType;
    [TextArea(2, 5)]
    public string content;//说话内容
    public float displayDuration = 3f;
    public bool isCanRepeat = false;
    public bool hasTriggered = false;
}


public class NPCManager : MonoBehaviour
{
    [Header("UI设置")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    
    [Header("对话设置")]
    [SerializeField] private List<DialogueData> dialogueList = new List<DialogueData>();
    
    private static NPCManager instance;
    public static NPCManager Instance => instance;
    
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        // 初始化对话面板
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
            
        // 注册游戏事件
        RegisterGameEvents();
    }

    private void RegisterGameEvents()
    {
        // 获取GameManage实例
        GameManage gameManager = GameManage.Instance;
        if (gameManager != null)
        {
            // 监听击杀事件
            gameManager.onEnemyKilled.AddListener(OnEnemyKilled);
            // 监听波次清理事件
            gameManager.onWaveCleared.AddListener(OnWaveCleared);
            // 在这里添加更多事件监听...

            gameManager.onMoreEnemy.AddListener(OnMoreEnemy);

            gameManager.onSecondWave.AddListener(OnSecondWave);

            gameManager.onFastClear.AddListener(OnFastClear);

            gameManager.onFlyHitTarget.AddListener(OnFlyHitTarget);

            gameManager.onPlayerDamageTaken.AddListener(OnPlayerDamageTaken);

            gameManager.onAppearFirstStage3.AddListener(OnAppearFirstStage3);

            gameManager.onAppearFirstStage4.AddListener(OnAppearFirstStage4);

            gameManager.onNpcHelp.AddListener(OnNpcHelp);
        }
    }

    private void OnEnemyKilled(int killCount)
    {
        if (killCount == 1)
        {
            ShowDialogue(DialogueTriggerType.FirstEnemyKilled);
        }
    }

    private void OnWaveCleared(int waveCount)
    {
        if (waveCount == 1)
        {
            ShowDialogue(DialogueTriggerType.FirstWaveCleared);
        }
    }

    private void OnMoreEnemy()
    {
        ShowDialogue(DialogueTriggerType.RepeatMoreEnemy);
    }
    private void OnSecondWave(int SecondWave)
    {
        if (SecondWave == 1)
        {
            ShowDialogue(DialogueTriggerType.FirstSecondWave);
        }
    }
    private void OnFastClear()
    {
        ShowDialogue(DialogueTriggerType.RepeatFastClear);
    }
    private void OnFlyHitTarget()
    {
        int a = Random.Range(0, 2);
        if (a == 0)
        {
            ShowDialogue(DialogueTriggerType.ChanceFlyHitTarget);
        }
    }
    private void OnPlayerDamageTaken()
    {
        int a = Random.Range(0, 3);
        if (a == 0)
        {
            ShowDialogue(DialogueTriggerType.ChancePlayerDamageTaken);
        }
    }
    private void OnAppearFirstStage3(int AppearFirstStage3)
    {
        if (AppearFirstStage3 == 1)
        {
            ShowDialogue(DialogueTriggerType.FirstAppearFirstStage3);
        }
    }
    private void OnAppearFirstStage4(int AppearFirstStage4)
    {
        if (AppearFirstStage4 == 1)
        {
            ShowDialogue(DialogueTriggerType.FirstAppearFirstStage4);
        }
    }
    private void OnNpcHelp(int NpcHelp)
    {
        if (NpcHelp == 1)
        {
            ShowDialogue(DialogueTriggerType.FirstNpcHelp);
        }
    }
    private Tween tdoScale;
    private Tween tdoActive;
    public void ShowDialogue(DialogueTriggerType triggerType)
    {
        DialogueData dialogue = dialogueList.Find(d => d.triggerType == triggerType && !d.hasTriggered);
        if (dialogue == null) return;
        

        // 标记为已触发
        dialogue.hasTriggered = true;
        if (tdoScale!=null)
        {
            tdoScale.Kill();
        }
        if (tdoActive!=null)
        {
            tdoActive.Kill();
        }
        // 显示对话
        dialoguePanel.SetActive(true);
        dialogueText.text = dialogue.content;

        // 使用DOTween制作显示动画
        dialoguePanel.transform.localScale = Vector3.zero;
        tdoScale=dialoguePanel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);

        // 自动隐藏
        tdoActive=
        DOVirtual.DelayedCall(dialogue.displayDuration, () => {
            dialoguePanel.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack)
                .OnComplete(() => dialoguePanel.SetActive(false));
        });

        if (dialogue.isCanRepeat==true)
        {
            dialogue.hasTriggered = false;
        }
    }

    public void ResetDialogues()
    {
        foreach (var dialogue in dialogueList)
        {
            dialogue.hasTriggered = false;
        }
    }
}