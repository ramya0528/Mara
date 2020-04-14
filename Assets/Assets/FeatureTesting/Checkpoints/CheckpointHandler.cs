﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointHandler : MonoBehaviour
{
    [SerializeField]
    float respawnTime;

    private CheckpointData _activeCheckPoint;
    private Timer _respawnTimer;

    // Subscribe to player death event
    private void OnEnable()
    {
        PlayerInsanity.onPlayerDeath += RespawnPlayer;
    }

    private void OnDisable()
    {
        PlayerInsanity.onPlayerDeath -= RespawnPlayer;
    }

    private void Start()
    {
        _activeCheckPoint = new CheckpointData(this.transform, 0);
    }

    private void LateUpdate()
    {
        if (_respawnTimer != null)
        {
            _respawnTimer.Time += Time.deltaTime;

            if (_respawnTimer.Expired())
            {
                Spawn();
                _respawnTimer = null;
            }
        }
    }


    // Save checkpoint data to struct on checkpoint activation
    public void ActivateCheckpoint(Transform respawnPosition, bool useMaxHealth = false)
    {
        print("Checkpoint Set");

        float insanity;

        Transform position = respawnPosition;

        if (useMaxHealth)
        {
            insanity = 0;
        }
        else
        {
            insanity = GlobalState.state.Player.GetComponent<PlayerInsanity>().GetInsanity();
        }

        _activeCheckPoint = new CheckpointData(position, insanity);
    }

    // Respawn player using checkpoint data
    private void Spawn()
    {
        print("Respawning Player");

        GlobalState.state.Player.GetComponent<CharacterController>().enabled = false;
        GlobalState.state.Player.transform.position = _activeCheckPoint.pos.position;
        GlobalState.state.Player.transform.rotation = _activeCheckPoint.pos.rotation;
        GlobalState.state.Player.GetComponent<PlayerInsanity>().SetInsanity(_activeCheckPoint.ins);
        GlobalState.state.Player.GetComponent<CharacterController>().enabled = true;
    }

    void RespawnPlayer()
    {
        _respawnTimer = new Timer(respawnTime);
    }
}

public struct CheckpointData 
{
    public CheckpointData(Transform position, float insanity)
    {
        pos = position;
        ins = insanity;
    }

    public Transform pos;
    public float ins;
}