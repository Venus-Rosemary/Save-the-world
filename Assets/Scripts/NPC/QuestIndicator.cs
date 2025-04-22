using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestIndicator : MonoBehaviour
{
    public Image TextUI;
    public Transform target;
    Camera mainCamera => Camera.main;
    RectTransform indicator => TextUI.rectTransform;
    
    void Update()
    {
        if (target == null || mainCamera == null) return;
        Vector3 targetViewportPos = mainCamera.WorldToViewportPoint(target.position);

        // 获取UI尺寸的一半
        float halfWidth = indicator.rect.width * 0.5f;
        float halfHeight = indicator.rect.height * 0.5f;

        // 计算屏幕边界
        float minX = halfWidth;
        float maxX = Screen.width - halfWidth;
        float minY = halfHeight;
        float maxY = Screen.height - halfHeight;

        if (targetViewportPos.z > 0)
        {
            // 转换到屏幕坐标
            Vector2 screenPos = new Vector2(
                targetViewportPos.x * Screen.width,
                targetViewportPos.y * Screen.height
            );

            // 限制在可视范围内
            screenPos.x = Mathf.Clamp(screenPos.x, minX, maxX);
            screenPos.y = Mathf.Clamp(screenPos.y, minY, maxY);

            // 转换回UI坐标系
            indicator.position = new Vector2(
                screenPos.x,
                screenPos.y
            );

            indicator.rotation = Quaternion.identity;
        }
        else
        {
            Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0) / 2;
            Vector3 targetScreenPos = mainCamera.WorldToScreenPoint(target.position);
            
            if (targetScreenPos.z < 0)
                targetScreenPos *= -1;
                
            Vector3 directionFromCenter = (targetScreenPos - screenCenter).normalized;

            // 计算与边界的交点
            float xRange = (Screen.width - indicator.rect.width) * 0.5f;
            float yRange = (Screen.height - indicator.rect.height) * 0.5f;
            
            float x = xRange / Mathf.Abs(directionFromCenter.x);
            float y = yRange / Mathf.Abs(directionFromCenter.y);
            float d = Mathf.Min(x, y);
            
            Vector3 edgePosition = screenCenter + (directionFromCenter * d);
            edgePosition.z = 0;
            
            // 确保UI完全在屏幕内
            edgePosition.x = Mathf.Clamp(edgePosition.x, minX, maxX);
            edgePosition.y = Mathf.Clamp(edgePosition.y, minY, maxY);
            
            indicator.position = edgePosition;

            float angle = Mathf.Atan2(directionFromCenter.y, directionFromCenter.x) * Mathf.Rad2Deg;
            //indicator.rotation = Quaternion.Euler(0, 0, angle + 90);
        }
    }
}
