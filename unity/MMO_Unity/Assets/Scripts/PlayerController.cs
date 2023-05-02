using System.Collections;
using System.Collections.Generic;
using UnityEngine;

class Tank
{
    // 온갖 정보
    public float speed = 10.0f;
    Player player;  // 포함 관계 Netsted Prefab(Pre-Fabrication) 
                    // Prefab들을 조합 한것들을 Netsted Prefab이라고 한다.
}

class FastTank : Tank
{

}

class Player
{

}


public class PlayerController : MonoBehaviour
{
    [SerializeField]
    float _speed = 10.0f;    

    void Start()
    {
        // 실수로 KeyBoard가 구독이 되었으면 끈은 다음에 
        Managers.Input.KeyAction -= OnKeyboard; 
        Managers.Input.KeyAction += OnKeyboard; // 구독 신청

        Tank tank1 = new Tank();// Instance를 만든다.
        tank1.speed = 11.0f;
        Tank tank2 = new Tank();
        tank2.speed = 21.0f;
        Tank tank3 = new Tank();
        Tank tank4 = new Tank();
        Tank tank5 = new Tank();
    }
    
    void Update()
    { 
        
    }
    void OnKeyboard()
    {
        if (Input.GetKey(KeyCode.W))
        {
            // 방향으로 바라보게            
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.forward), 0.2f);
            //transform.Translate(Vector3.forward * Time.deltaTime * _speed); // 상대 좌표 방향으로 이동
            transform.position += Vector3.forward * Time.deltaTime * _speed; // 절대 좌표로 이동하는 방식이므로 어색함을 줄일수 도 있다.
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.back), 0.2f);
            //transform.Translate(Vector3.forward * Time.deltaTime * _speed);
            transform.position += Vector3.back * Time.deltaTime * _speed;
        }
        //transform
        if (Input.GetKey(KeyCode.A))
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.left), 0.2f);
            //transform.Translate(Vector3.forward * Time.deltaTime * _speed);
            transform.position += Vector3.left * Time.deltaTime * _speed;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.right), 0.2f);
            //transform.Translate(Vector3.forward * Time.deltaTime * _speed);
            transform.position += Vector3.right * Time.deltaTime * _speed;
        }
    }
}
