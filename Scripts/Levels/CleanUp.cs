using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CleanUp : MonoBehaviour
{
    [SerializeField] private int YBounder = 0;
    void FixedUpdate()
    {
        if(this.transform.position.y < YBounder)
        {
            Destroy(this.gameObject);
        }
    }
}
