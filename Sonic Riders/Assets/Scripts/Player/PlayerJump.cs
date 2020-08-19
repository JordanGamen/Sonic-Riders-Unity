﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJump : MonoBehaviour
{
    private PlayerSound playerSound;
    [SerializeField] private SphereCollider groundCol;

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
    public bool JumpButtonUp { get; set; } = false;
    public bool DontDragDown { get; set; } = false;

    private Rigidbody rb;
    private PlayerMovement mov;
    private PlayerTricks playerTricks;
    private CharacterStats charStats;
    [SerializeField] private float timeForLength = 0.5f;

    private float rampPower;
    public float RampPower { get { return rampPower; } }
    private float maxRampPower;
    private float worstRampPower;

    public Ramp CurrRamp { get; set; }

    private bool alreadyFell = false;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mov = GetComponent<PlayerMovement>();
        playerTricks = GetComponent<PlayerTricks>();
        charStats = GetComponent<CharacterStats>();
        playerSound = GetComponentInChildren<PlayerSound>();
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

            JumpHold = false;

            return;
        }

        if (JumpHoldControls)
        {
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

        if (JumpButtonUp)
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
                        Debug.Log("Diff: " + diffPos + " Percent: "+ percent + " power: " + rampPower);
                    }
                    else
                    {
                        rampPower = worstRampPower;
                    }

                    playerSound.PlaySoundEffect(PlayerSound.voiceSounds.JUMPRAMP, PlayerSound.sounds.NONE);
                }
                else
                {
                    if (!playerTricks.CanDoTricks)
                    {
                        playerSound.PlaySoundEffect(PlayerSound.voiceSounds.PERFECTJUMP, PlayerSound.sounds.NONE);
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
                playerSound.PlaySoundEffect(PlayerSound.voiceSounds.NONE, PlayerSound.sounds.JUMP);
            }

            mov.RaycastLength = raycastJumpLength;
            DontDragDown = true;
            Invoke("CanDragDown", 0.2f);

            if (rampPower > 0)
            {
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
                Vector3 localJumpVel = transform.GetChild(0).TransformDirection(new Vector3(0, jumpHeight, mov.Speed));
                rb.velocity = localJumpVel;
            }

            jumpHeight = startingJumpHeight;
            GrindJumpHeight = 0;
            jumpRelease = false;
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
                playerSound.PlaySoundEffect(PlayerSound.voiceSounds.JUMPRAMP, PlayerSound.sounds.NONE);
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
