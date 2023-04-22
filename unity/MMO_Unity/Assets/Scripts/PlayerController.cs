using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class PlayerController : MonoBehaviour
{
    [SerializeField]
    float _speed = 10.0f;    

    void Start()
    {
        
    }

    float _yAngle = 0.0f;    
    void Update()
    {
        _yAngle += Time.deltaTime * 100.0f;

        //절대 회전값 지정
        //transform.eulerAngles = new Vector3(0, _yAngle, 0);

        // 특정 축을 기준으로 +/- delta (1)
        //transform.Rotate(new Vector3(0.0f, Time.deltaTime * 100.0f, 0.0f));

        // 특정 축을 기준으로 +/- delta (2)
        //transform.rotation = Quaternion.Euler(new Vector3(0, _yAngle, 0));

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
