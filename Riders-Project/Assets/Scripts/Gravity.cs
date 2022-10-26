using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity : MonoBehaviour
{
    [SerializeField] private float gravityScale = 1.0f;
    private static float globalGravity = -9.81f;
    public bool usesGravity;
    public Transform model;

    void Start()
    {
        usesGravity = true;
    }

    private void FixedUpdate()
    {
        if (usesGravity)
        {
            Vector3 gravity = globalGravity * gravityScale * model.up;
            GetComponent<Rigidbody>().AddForce(gravity, ForceMode.Acceleration);
        }
    }
}
