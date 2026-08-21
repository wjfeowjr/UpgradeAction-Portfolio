using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    protected static T instance;

    public static T Instance
    {
        get
        {
            if (instance == null)
                // FindObjectOfType 은 Unity 6 에서 폐기됐다.
                // FindAnyObjectByType 은 어느 것이 나올지 보장하지 않으므로,
                // 기존 동작에 가까운 FindFirstObjectByType 을 쓴다.
                instance = (T)FindFirstObjectByType(typeof(T));
            return instance;
        }
    }

    protected virtual void Awake()
    {
        // 이미 인스턴스가 존재하면 씬 복귀 시 새로 생긴 중복 오브젝트를 파괴
        if (instance != null && instance != (T)(MonoBehaviour)this)
        {
            Destroy(transform.root != null ? transform.root.gameObject : gameObject);
            return;
        }

        instance = (T)(MonoBehaviour)this;

        if (transform.parent != null && transform.root != null)
            DontDestroyOnLoad(transform.root.gameObject);
        else
            DontDestroyOnLoad(gameObject);
    }
}