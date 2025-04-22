using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("UI")]
    public GameObject StartUI;
    public GameObject GameUI;
    public GameObject EndUI;

    [Header("Button")]
    public Button button1;
    public Button button2;

    private void Awake()
    {
        button1.onClick.AddListener(TransitionEvent);
        button2.onClick.AddListener(EndButtonEvent);
    }
    void Start()
    {
        UIActiveControl(true, false, false);
        GameManage.Instance.PlayerObject.SetActive(false);
        GameManage.Instance.StartEnemy.SetActive(false);
        GameManage.Instance.Npc.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TransitionEvent()//开始界面到游戏中的中间过渡
    {
        UIActiveControl(false, false, false);
        GameManage.Instance.PlayerObject.SetActive(true);
        GameManage.Instance.PlayerObject.GetComponent<PlayerController>().SetCanControl(true);
        GameManage.Instance.StartEnemy.SetActive(true);
        GameManage.Instance.StartEnemy.transform.position = new Vector3(0, 1, 0);
    }

    public void StartButtonEvent()//过渡-》进入游戏中
    {
        UIActiveControl(false, true, false);
        GameManage.Instance.Npc.SetActive(true);
    }

    public void IsGameEvent()//游戏中-》结束界面
    {
        UIActiveControl(false, false, true);
        GameManage.Instance.Npc.SetActive(false);
        GameManage.Instance.PlayerObject.GetComponent<PlayerController>().SetCanControl(false);
    }

    public void EndButtonEvent()//结束界面-》开始界面
    {
        GameManage.Instance.ClearAllEnemies();
        UIActiveControl(true, false, false);
    }

    public void UIActiveControl(bool StartU,bool EndU,bool GameU)
    {
        StartUI.SetActive(StartU);
        GameUI.SetActive(EndU);
        EndUI.SetActive(GameU);
    }

    public void ExitClick()//退出游戏
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
