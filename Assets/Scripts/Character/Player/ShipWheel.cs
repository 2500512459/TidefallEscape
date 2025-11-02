using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ShipWheel : MonoBehaviour
{
    public Vector2 range;

    public float speed;

    private float rotationY = 0;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (InputManager.Instance != null)
        {
            rotationY += InputManager.Instance.inputMove.x * speed;

            rotationY = Mathf.Clamp(rotationY, range.x, range.y);
            //Debug.Log("WHEEL ROTATION " + rotationZ.ToString());

            transform.localRotation = Quaternion.Euler(0, rotationY, 0);
        }
    }
}


