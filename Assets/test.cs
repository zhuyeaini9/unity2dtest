using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public Camera Cam;
    public float Speed = 10.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        float du = Input.GetAxis("Vertical") * Speed;
        float lr = Input.GetAxis("Horizontal") * Speed;

        // Make it move 10 meters per second instead of 10 meters per frame...
        du *= Time.deltaTime;
        lr *= Time.deltaTime;

        // Move translation along the object's z-axis
        transform.Translate(lr, du, 0);
    }


    
}
