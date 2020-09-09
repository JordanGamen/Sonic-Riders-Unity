﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    private AudioManagerHolder audioHolder;

    [SerializeField] private float startingJumpHeight = 20;
    [SerializeField] private float jumpHeight = 20;
    [SerializeField] private float maxJumpHeight = 60;
    [SerializeField] private float jumpGain = 1;
    [SerializeField] private float raycastJumpLength = 0.5f;
    public float JumpHeight { get { return jumpHeight; } set { jumpHeight = value; } }
    public float GrindJumpHeight { get; set; } = 0;

    [SerializeField] private bool jumpRelease = false;
    [SerializeField] private bool actualJumpRelease = false;
    public bool JumpRelease { get { return jumpRelease; } set { jumpRelease = value; } }
    public bool JumpHold { get; set; } = false;
    public bool JumpHoldControls { get; set; } = false;
    public bool DontDragDown { get; set; } = false;

    private Rigidbody rb;
    private PlayerMovement mov;
    private PlayerTricks playerTricks;
    private CharacterStats charStats;
    [SerializeField] private float timeForLength = 0.5f;

    [SerializeField] private float rampPower;
    public float RampPower { get { return rampPower; } set { rampPower = value; } }
    private float maxRampPower;
    private float worstRampPower;

    [SerializeField] private float highestYvel = 0;

    public Ramp CurrRamp { get; set; }

    private bool alreadyFell = false;
    private bool canClamp = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mov = GetComponent<PlayerMovement>();
        playerTricks = GetComponent<PlayerTricks>();
        charStats = GetComponent<CharacterStats>();
        audioHolder = GetComponent<AudioManagerHolder>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!mov.Grounded || charStats.Air <= 0)
        {
            if (jumpHeight != startingJumpHeight)
            {
                jumpHeight = startingJumpHeight;
            }

            return;
        }

        if (JumpHoldControls)
        {
            HoldingJump();
        }
    }

    public void HoldingJump()
    {
        if (!(mov.Grounded && charStats.Air > 0))
        {
            return;
        }

        JumpHold = true;

        if (charStats.Air > 0)
        {
            charStats.Air -= 5 * Time.deltaTime;
        }

        if (jumpHeight < maxJumpHeight)
        {
            jumpHeight += jumpGain * Time.deltaTime;
        }
    }

    public void CheckRelease()
    {
        if (mov.Grounded && charStats.Air > 0)
        {
            JumpHold = false;

            if (transform.parent != null)
            {
                CurrRamp = transform.GetComponentInParent<Ramp>();

                if (CurrRamp.Flight)
                {
                    rampPower = 0;
                    jumpRelease = true;
                    return;
                }

                rampPower = CurrRamp.Power;
                maxRampPower = rampPower;
                worstRampPower = CurrRamp.WorstPower;

                if (transform.localPosition.z < CurrRamp.PerfectJump)
                {
                    if (transform.localPosition.z > -CurrRamp.PerfectJump)
                    {
                        float powerDiff = rampPower - worstRampPower;
                        float diffPos = transform.localPosition.z + CurrRamp.PerfectJump;
                        float percent = diffPos / (CurrRamp.PerfectJump * 2);
                        rampPower = worstRampPower + powerDiff * percent;
                        Debug.Log("Diff: " + diffPos + " Percent: " + percent + " power: " + rampPower);
                    }
                    else
                    {
                        rampPower = worstRampPower;
                    }

                    audioHolder.VoiceManager.Play(Constants.VoiceSounds.rampJump);
                }
                else
                {
                    if (!playerTricks.CanDoTricks)
                    {
                        audioHolder.VoiceManager.Play(Constants.VoiceSounds.perfectJump);
                    }
                }

                Debug.Log("Ramp power " + rampPower);
            }
            else
            {
                rampPower = 0;
            }

            jumpRelease = true;
        }
    }

    private void FixedUpdate()
    {
        if (jumpRelease)
        {
            if (mov.IsPlayer)
            {
                audioHolder.SfxManager.Play(Constants.SoundEffects.jump);
            }

            DontDragDown = true;
            mov.RaycastLength = raycastJumpLength;
            Invoke("CanDragDown", 0.5f);

            if (rampPower > 0)
            {
                canClamp = false;
                Quaternion rot = transform.GetChild(0).rotation;
                rot.y = transform.parent.rotation.y;
                transform.GetChild(0).rotation = rot;

                //rb.velocity = transform.GetChild(0).forward * mov.Speed;

                //rb.AddForce(transform.parent.GetChild(0).forward * (jumpHeight + rampPower), ForceMode.Force);
                rb.velocity = transform.parent.GetChild(0).forward * (jumpHeight + rampPower);

                transform.parent = null;
                alreadyFell = false;
                playerTricks.ChangeTrickSpeed(rampPower, maxRampPower, worstRampPower, jumpHeight, startingJumpHeight, maxJumpHeight);
            }
            else
            {
                rb.AddForce(transform.up * jumpHeight, ForceMode.VelocityChange);
                highestYvel = 0;
                canClamp = true;
            }

            jumpHeight = startingJumpHeight;
            GrindJumpHeight = 0;
            jumpRelease = false;
        }

        if (rb.velocity.y > highestYvel)
        {
            highestYvel = rb.velocity.y;
        }

        if (canClamp && !mov.Grounded)
        {
            Vector3 localVel = transform.GetChild(0).InverseTransformDirection(rb.velocity);
            localVel.y = Mathf.Clamp(localVel.y, -99, 6.5f + (jumpHeight - startingJumpHeight));
            rb.velocity = transform.GetChild(0).TransformDirection(localVel);
            //Debug.Log(localVel);
        }
    }

    private void CanDragDown()
    {
        DontDragDown = false;
    }

    public void FallingOffRamp(float worstPower, float perfectJump, float powerOfRamp)
    {
        if (transform.parent != null && !alreadyFell && transform.localPosition.z > perfectJump && !playerTricks.CanDoTricks)
        {            
            alreadyFell = true;
            rampPower = worstPower;
            maxRampPower = powerOfRamp;
            worstRampPower = worstPower;

            jumpRelease = true;

            //For now!!!!!
            if (mov.IsPlayer)
            {
                audioHolder.VoiceManager.Play(Constants.VoiceSounds.rampJump);
            }           

            Debug.Log("Fell of ramp");
        }
        else
        {
            if (!jumpRelease && !DontDragDown)
            {
                transform.parent = null;
            }
        }
    }
}
