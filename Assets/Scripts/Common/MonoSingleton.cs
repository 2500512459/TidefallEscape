using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static bool init = false;
    private static T instance = null;
    public static T Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                
                if (instance == null)
                {
                    //GameObject obj = new GameObject();
                    //obj.name = typeof(T).Name;
                    //instance = obj.AddComponent<T>();
                    Debug.Log("Need type " + typeof(T));
                }
                else
                {
                    init = true;
                }
            }
            return instance;
        }
    }

    protected virtual void Awake()
    {
        if (init) return;

        if (instance == null)
        {
            instance = (T)this;
            //DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            //Destroy(gameObject);
            Debug.LogWarning("Multi objects of type " + typeof(T));
        }
    }
}


