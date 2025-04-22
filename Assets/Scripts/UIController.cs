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

    public void TransitionEvent()//��ʼ���浽��Ϸ�е��м����
    {
        UIActiveControl(false, false, false);
        GameManage.Instance.PlayerObject.SetActive(true);
        GameManage.Instance.PlayerObject.GetComponent<PlayerController>().SetCanControl(true);
        GameManage.Instance.StartEnemy.SetActive(true);
        GameManage.Instance.StartEnemy.transform.position = new Vector3(0, 1, 0);
    }

    public void StartButtonEvent()//����-��������Ϸ��
    {
        UIActiveControl(false, true, false);
        GameManage.Instance.Npc.SetActive(true);
    }

    public void IsGameEvent()//��Ϸ��-����������
    {
        UIActiveControl(false, false, true);
        GameManage.Instance.Npc.SetActive(false);
        GameManage.Instance.PlayerObject.GetComponent<PlayerController>().SetCanControl(false);
    }

    public void EndButtonEvent()//��������-����ʼ����
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

    public void ExitClick()//�˳���Ϸ
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
