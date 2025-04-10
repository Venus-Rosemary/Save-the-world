using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = (T)FindObjectOfType(typeof(T));

                if (_instance == null)
                {
                    Debug.LogError("An instance of " + typeof(T) +
                                   " is needed in the scene, but there is none.");
                }
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject); // 如果希望在场景切换时也保持实例，可以加上这一行
        }
        else
        {
            Destroy(gameObject); // 确保不会存在多个实例
        }
    }
}
