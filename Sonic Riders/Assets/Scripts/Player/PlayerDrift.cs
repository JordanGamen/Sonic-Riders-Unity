﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDrift : MonoBehaviour
{
    private PlayerMovement movement;
    private BoardStats stats;
    private CharacterStats charStats;
    private AudioManagerHolder audioHolder;
    public bool DriftPressed { get; set; } = false;
    public float DriftDir { get; set; } = 0;
    private Animator canvasAnim;

    private float driftTimer = 0;
    [SerializeField] private float brakePower = 30;

    // Start is called before the first frame update
    void Start()
    {
        movement = GetComponent<PlayerMovement>();
        charStats = GetComponent<CharacterStats>();
        canvasAnim = GameObject.FindGameObjectWithTag(Constants.Tags.canvas).GetComponent<Animator>();
        stats = charStats.BoardStats;
        audioHolder = GetComponent<AudioManagerHolder>();
    }

    // Update is called once per frame
    void Update()
    {
        if (movement.Grounded && charStats.Air > 0)
        {
            if (DriftPressed)
            {
                if ((movement.TurnAmount != 0 || driftTimer > 0))
                {
                    Drift();
                }
                else
                {
                    //Braking
                    if (movement.Speed > 0)
                    {
                        movement.Speed -= brakePower * Time.deltaTime;
                    }
                    else if (movement.Speed < 0)
                    {
                        movement.Speed += brakePower * Time.deltaTime;
                    }
                }
                
                return;
            }
        }

        if (!DriftPressed && driftTimer > 1)
        {
            driftTimer = 0;
            movement.FallToTheGround = false;
            if (movement.Speed < stats.Boost[charStats.Level])
            {
                movement.Speed = stats.Boost[charStats.Level];
            }
            else
            {
                movement.Speed += 5;
            }            
            movement.DriftBoost = true;

            audioHolder.SfxManager.Play(Constants.SoundEffects.boost);

            if (movement.IsPlayer)
            {
                canvasAnim.Play("BoostCircle");
            }
        }

        movement.Drifting = false;
        driftTimer = 0;
        if (!movement.DriftBoost)
        {
            DriftDir = 0;
        }        
    }

    private void Drift()
    {
        if (movement.Speed <= 0)
        {
            movement.Drifting = false;
            driftTimer = 0;
            return;
        }

        if (DriftDir == 0)
        {
            if (movement.TurnAmount > 0)
            {
                DriftDir = 1;
            }
            else
            {
                DriftDir = -1;
            }
        }       

        driftTimer += Time.deltaTime;
        movement.Drifting = true;
        movement.Speed -= 4 * Time.deltaTime;
        charStats.Air -= stats.AirDepletion * Time.deltaTime;
    }
}
