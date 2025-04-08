using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [Header("跟随设置")]
    [SerializeField] private Transform target; // 跟随的目标（玩家）
    [SerializeField] private float followDistance = 5f; // 摄像机与玩家的距离
    [SerializeField] private float followHeight = 2f; // 摄像机高度
    [SerializeField] private float followSmoothness = 0.1f; // 跟随平滑度

    [Header("旋转设置")]
    [SerializeField] private float rotationAngle = 45f; // 旋转角度
    [SerializeField] private float rotationSmoothness = 0.2f; // 旋转平滑度

    private Vector3 cameraOffset; // 摄像机相对于玩家的偏移量
    private float currentYRotation = 0f; // 当前Y轴旋转角度
    private float targetYRotation = 0f; // 目标Y轴旋转角度
    private float defaultYRotation = 0f; // 默认Y轴旋转角度

    private void Start()
    {
        if (target == null)
        {
            // 尝试查找玩家
            target = GameObject.FindGameObjectWithTag("Player")?.transform;
            
            if (target == null)
            {
                Debug.LogError("摄像机控制脚本未找到目标，请手动设置目标！");
                enabled = false;
                return;
            }
        }

        // 初始化摄像机位置
        cameraOffset = new Vector3(0, followHeight, -followDistance);
        transform.position = target.position + cameraOffset;
        transform.LookAt(target.position + Vector3.up * followHeight * 0.5f);
        
        // 保存默认旋转角度
        defaultYRotation = transform.eulerAngles.y;
        currentYRotation = defaultYRotation;
        targetYRotation = defaultYRotation;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // 处理旋转输入
        HandleRotationInput();

        // 计算目标旋转
        Quaternion rotation = Quaternion.Euler(0, targetYRotation, 0);
        
        // 计算目标位置
        Vector3 targetPosition = target.position + rotation * cameraOffset;
        
        // 平滑移动摄像机
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSmoothness);
        
        // 平滑旋转摄像机
        currentYRotation = Mathf.Lerp(currentYRotation, targetYRotation, rotationSmoothness);
        transform.rotation = Quaternion.Euler(
            transform.rotation.eulerAngles.x,
            currentYRotation,
            transform.rotation.eulerAngles.z
        );
        
        // 确保摄像机始终看向玩家
        transform.LookAt(target.position + Vector3.up * followHeight * 0.5f);
    }

    private void HandleRotationInput()
    {
        // 检查是否按住Q键（向左旋转）
        if (Input.GetKey(KeyCode.Q))
        {
            targetYRotation = defaultYRotation - rotationAngle;
        }
        // 检查是否按住E键（向右旋转）
        else if (Input.GetKey(KeyCode.E))
        {
            targetYRotation = defaultYRotation + rotationAngle;
        }
        // 如果都没有按下，恢复默认角度
        else
        {
            targetYRotation = defaultYRotation;
        }
    }
}
