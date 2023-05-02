using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Managers : MonoBehaviour
{
    static Managers s_Instance; // 유일성이 보장된다.
    static Managers Instance { get { Init();  return s_Instance; } }

    InputManager _input = new InputManager();
    public static InputManager Input { get { return Instance._input; } }

    // Start is called before the first frame update
    void Start()
    {
        Init();

    }

    // Update is called once per frame
    void Update()
    {
        _input.OnUpdate();        
    }

    static void Init()
    {
        if(s_Instance == null)
        {
            // 초기화        
            GameObject go = GameObject.Find("@Managers");
            if(go == null)
            {
                go = new GameObject { name = "@Managers" };
                go.AddComponent<Managers>();
            }

            DontDestroyOnLoad(go);
            s_Instance = go.GetComponent<Managers>();
        }        
    }
}
