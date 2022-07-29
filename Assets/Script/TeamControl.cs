using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TeamControl : MonoBehaviour
{
    public GameObject mPlayer;
    Vector2 mMovement;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        Character cha = mPlayer.GetComponent<Character>();
        if(cha)
        {
        }
    }

    void OnMove(InputValue movementValue)
    {
        mMovement = movementValue.Get<Vector2>();
    }
}
