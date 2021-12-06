using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateAround : MonoBehaviour
{

    public Transform target;
    public float rotationSpeed;
    private void LateUpdate()
    {
        transform.RotateAround(target.position, target.up, Time.deltaTime * rotationSpeed);
    }
}
