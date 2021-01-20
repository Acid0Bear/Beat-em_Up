using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gate : MonoBehaviour
{
    [SerializeField] private GameObject Healthy = null, Broken = null;

    public void SetState(bool value)
    {
        if (value)
            Broken.SetActive(true);
        else
            Healthy.SetActive(true);
    }
}
