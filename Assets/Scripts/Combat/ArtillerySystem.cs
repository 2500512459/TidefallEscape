using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArtillerySystem : MonoBehaviour
{
    public Indicator indicator;
    public Transform rig;
    [SerializeField] CannonBall ball;
    // Start is called before the first frame update
    void Start()
    {
    }
    // Update is called once per frame
    void Update()
    {
        if (indicator != null && rig != null)
        {
            indicator.transform.position = rig.position;
            if (Input.GetMouseButton(0))
            {
                indicator.gameObject.SetActive(true);
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    //Fire
                    CannonBall ballObj = ball.Spawn<CannonBall>(null, rig.position, Quaternion.identity);
                    ballObj.speed = indicator.GetCurrentVelocity();
                    ballObj.Launch(indicator.GetShootDirection());
                }
            }
            else
            {
                indicator.gameObject.SetActive(false);
            }
        }
    }
}
