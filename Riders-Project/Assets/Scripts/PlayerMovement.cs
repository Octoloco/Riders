using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody rb;
    [SerializeField] private float accelerationSpeed;
    [SerializeField] private float stopSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float dragForce;
    [SerializeField] private float acceleration;
    [SerializeField] private float savedVelocity;
    [SerializeField] private Transform model;
    [SerializeField] private Transform mainCamera;
    [SerializeField] private ParticleSystem thruster1;
    [SerializeField] private ParticleSystem thruster2;
    private Gravity gravityScript;
    [SerializeField] private Controls controls;
    float accel;
    float drift;
    float horizontal;
    float jump;
    public bool jumping;
    [SerializeField] private bool drifting;
    [SerializeField] private bool boosting;
    [SerializeField] private bool driftingBoost;
    [SerializeField] private float driftingTimer;
    [SerializeField] private float driftDirection;

    private bool distorting;
    private PostProcessVolume ppVolume;
    private AudioSource asMotor;
    private AudioSource asBoom;
    [SerializeField] private SoundEvent boomEvent;
    private SoundEvent motorEvent;
    private LensDistortion lensDistortion;
    private ChromaticAberration chromaticAberration;


    private void Awake()
    {
        controls = new Controls();

        controls.PlayerControls.Accelerate.performed += ctx => accel = ctx.ReadValue<float>();
        controls.PlayerControls.Accelerate.canceled += ctx => accel = 0;

        controls.PlayerControls.Drift.performed += ctx => drift = ctx.ReadValue<float>();
        controls.PlayerControls.Drift.canceled += ctx => drift = 0;

        controls.PlayerControls.Jump.performed += ctx => jump = ctx.ReadValue<float>();
        controls.PlayerControls.Jump.canceled += ctx => jump = 0;

        controls.PlayerControls.Move.performed += ctx => horizontal = ctx.ReadValue<float>();
        controls.PlayerControls.Move.canceled += ctx => horizontal = 0;
    }

    private void OnEnable()
    {
        controls.Enable();
    }

    private void OnDisable()
    {
        controls.Disable();
    }
    void Start()
    {

        asBoom = transform.GetChild(2).GetChild(0).GetComponent<AudioSource>();
        boomEvent = asBoom.GetComponent<SoundEvent>();
        asBoom.pitch = 2.7f;
        asMotor = transform.GetChild(2).GetChild(1).GetComponent<AudioSource>();
        asMotor.pitch = 1.35f;
        gravityScript = GetComponent<Gravity>();
        driftingTimer = 1;
        drifting = false;
        turnSpeed = 2f;
        rb = GetComponent<Rigidbody>();
        model = transform.Find("Render");
        mainCamera = Camera.main.transform;
        ppVolume = mainCamera.GetComponent<PostProcessVolume>();
        ppVolume.profile.TryGetSettings<LensDistortion>(out lensDistortion);
        ppVolume.profile.TryGetSettings<ChromaticAberration>(out chromaticAberration);
    }
    private void FixedUpdate()
    {
        
        BoostFX();
        Thrusters();
        GravityCheck();
        Movement();
        CameraMovement();
        Drift();
        Turn();
        Jump();
    }

    private void Jump()
    {
        if (jump > 0 && !jumping)
        {
            Debug.Log("JUMP");
            jumping = true;
            rb.AddForce(Vector3.up * 50, ForceMode.Impulse);
        }
    }

    private void BoostFX()
    {

        if (lensDistortion.intensity.value < 0 && !distorting)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity, 0, 2f * Time.deltaTime);
            lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity, 0, .5f * Time.deltaTime);
        }

        if (lensDistortion.intensity.value < -70 && distorting)
        {
            distorting = false;
        }

        if (distorting)
        {
            chromaticAberration.intensity.value = Mathf.Lerp(chromaticAberration.intensity, 1, 5f * Time.deltaTime);
            lensDistortion.intensity.value = Mathf.Lerp(lensDistortion.intensity, -75, 7 * Time.deltaTime);
        }
    }
    private void Thrusters()
    {
        float mod;
        float pitchMod;
        float targetAngle;
        float targetPitch;
        if (accel > 0 && !drifting)
        {
            pitchMod = 0.025f;
            mod = 1;
            targetAngle = 1f;
            targetPitch = 2.7f;
        }
        else
        {
            pitchMod = 0.1f;
            targetPitch = 1.31f;
            mod = 5;
            targetAngle = .13f;
        }

        asMotor.pitch = Mathf.Lerp(asMotor.pitch, targetPitch, pitchMod);
        Vector3 newAngle = new Vector3(thruster1.transform.localScale.x, thruster1.transform.localScale.y, targetAngle);
        Vector3 newAngle2 = new Vector3(thruster2.transform.localScale.x, thruster2.transform.localScale.y, targetAngle);
        thruster1.transform.localScale = Vector3.Lerp(thruster1.transform.localScale, newAngle, mod * Time.deltaTime);
        thruster2.transform.localScale = Vector3.Lerp(thruster2.transform.localScale, newAngle2, mod * Time.deltaTime);
    }
    private void CameraMovement()
    {

        mainCamera.localPosition = new Vector3(mainCamera.localPosition.x, 2.91f, -6.4f + ((accelerationSpeed * -6.4f) / 80) / 4);

        if (mainCamera.localPosition.z > -6.4f)
        {
            mainCamera.localPosition = new Vector3(mainCamera.localPosition.x, 2.91f, -6.4f);
        }
        if (horizontal <= 0.15 && horizontal >= -0.15)
        {
            horizontal = 0;
        }

        if (!drifting)
        {

            if (horizontal == 0)
            {
                float targetAngle;

                targetAngle = 0;

                Vector3 newAngle = new Vector3(targetAngle, mainCamera.localPosition.y, mainCamera.localPosition.z);
                mainCamera.localPosition = Vector3.Lerp(mainCamera.localPosition, newAngle, Time.deltaTime * 5);
            }
        }
        else
        {
            float targetAngle;

            if (driftDirection > 0)
            {
                targetAngle = 2.4f;
            }
            else if (driftDirection < 0)
            {
                targetAngle = -2.4f;
            }
            else
            {
                targetAngle = 0;
            }

            Vector3 newAngle = new Vector3(targetAngle, mainCamera.localPosition.y, mainCamera.localPosition.z);
            mainCamera.localPosition = Vector3.Lerp(mainCamera.localPosition, newAngle, Time.deltaTime * 5);
        }
    }

    private void GravityCheck()
    {
        Vector3 dwn = transform.TransformDirection(Vector3.down);
        RaycastHit hitInfo;
        if (Physics.Raycast(new Vector3(model.transform.position.x, model.transform.position.y, model.transform.position.z), dwn, out hitInfo))
        {
            if (hitInfo.distance <= 2)
            {
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y, rb.velocity.z);
                gravityScript.usesGravity = false;
                jumping = false;
                Vector3 prev_up = transform.up;
                Vector3 desired_up = Vector3.Lerp(prev_up, hitInfo.normal, Time.deltaTime * 10);
                Quaternion tilt = Quaternion.FromToRotation(transform.up, desired_up);
                transform.rotation = tilt * transform.rotation;


                if (hitInfo.distance < 2)
                {
                    transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, hitInfo.point.y + 1, transform.position.z), Time.deltaTime * 50);

                }
            }
            else
            {
                jumping = true;
                Vector3 prev_up = transform.up;
                Vector3 desired_up = Vector3.Lerp(prev_up, Vector3.up, Time.deltaTime * 3);
                Quaternion tilt = Quaternion.FromToRotation(transform.up, desired_up);
                transform.rotation = tilt * transform.rotation;

                gravityScript.usesGravity = true;
            }

        }
        
    }
    private void Drift()
    {

        float targetAngle;

        if (drifting)
        {

            if (rb.velocity != Vector3.zero && driftDirection != 0)
            {
                driftingTimer += Time.deltaTime;
            }

            if (driftDirection > 0)
            {

                targetAngle = -45;
            }
            else if (driftDirection < 0)
            {
                targetAngle = 45;
            }
            else
            {
                targetAngle = 0;
            }




            turnSpeed = 1.9f;
            if (rb.velocity.magnitude > 0)
            {
                rb.AddForce(new Vector3(((rb.velocity.x * -1) * stopSpeed), 0, ((rb.velocity.z * -1) * stopSpeed)), ForceMode.Acceleration);
            }
        }
        else
        {
            targetAngle = 0;
            turnSpeed = 1.2f;
        }

        Quaternion newAngle = Quaternion.Euler(model.localRotation.eulerAngles.x, model.localRotation.eulerAngles.y, targetAngle);
        model.localRotation = Quaternion.Lerp(model.localRotation, newAngle, Time.deltaTime * 2);

    }


    

    private void Movement()
    {

        if (boosting)
        {
            acceleration = 10;
            if (accelerationSpeed >= 155.8f)
            {
                boosting = false;
            }
        }
        else
        {
            acceleration = 2;
        }

        if (drift > 0)
        {

            if (!drifting)
            {
                driftDirection = horizontal;
            }
            drifting = true;
            driftingBoost = true;
            dragForce = 1.4f;

        }
        else
        {
            driftDirection = 0;
            drifting = false;
            if (rb.velocity.magnitude > 10)
            {
                if (driftingTimer > .6)
                {
                    if (driftingBoost && !drifting)
                    {
                        driftingBoost = false;
                        rb.velocity = Vector3.zero;
                        rb.AddRelativeForce(Vector3.forward * savedVelocity * 3.5f, ForceMode.Impulse);
                        boosting = true;
                        driftingTimer = 0;
                        distorting = true;
                        boomEvent.PlayClip();
                    }
                }
                else
                {
                    driftingBoost = false;
                }
            }
            else
            {
                driftingBoost = false;
                driftingTimer = 0;
            }
            driftingTimer = 0;
            dragForce = 1f;

        }


        if (accel > 0 && !drifting && !driftingBoost)
        {
            accelerationSpeed += acceleration;

            if (accelerationSpeed > 155.8)
            {
                accelerationSpeed = 155.8f;
            }
            rb.AddForce(model.forward * accelerationSpeed, ForceMode.Acceleration);
            if (rb.velocity.magnitude > 80)
            {
                rb.velocity = rb.velocity.normalized * 50;
            }

        }
        else if (accel > 0 && drifting)
        {
            accelerationSpeed -= 3;
            rb.AddForce(model.forward * accelerationSpeed, ForceMode.Acceleration);
            if (accelerationSpeed < 0)
            {
                accelerationSpeed = 0;
            }
        }
        else
        {
            accelerationSpeed -= 3;
            if (accelerationSpeed < 0)
            {
                accelerationSpeed = 0;
            }
        }

        savedVelocity = rb.velocity.magnitude;
        if (savedVelocity > 50)
        {
            savedVelocity = 50;
        }
        rb.AddForce(new Vector3(((rb.velocity.x * -1) * dragForce), 0, ((rb.velocity.z * -1) * dragForce)), ForceMode.Force);

    }



    private void Turn()
    {
        float targetAngle;

        if (horizontal > .15)
        {
            targetAngle = 10;

        }
        else if (horizontal < -.15)
        {
            targetAngle = -10;


        }
        else
        {
            targetAngle = 0;
        }

        Quaternion newAngle = Quaternion.Euler(model.localRotation.eulerAngles.x, targetAngle, model.localRotation.eulerAngles.z);
        model.localRotation = Quaternion.Lerp(model.localRotation, newAngle, Time.deltaTime * 2);

        if (!drifting)
        {
            transform.Rotate(new Vector3(0, horizontal * turnSpeed, 0));


        }
        else
        {
            transform.Rotate(new Vector3(0, driftDirection * turnSpeed, 0));
        }
    }

}
