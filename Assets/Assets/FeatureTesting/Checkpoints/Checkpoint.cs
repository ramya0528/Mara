﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] 
    private Transform respawnPosition;

    [SerializeField]
    private bool maxHealthOnRespawn;

    private GameObject _globalState;

    private void Awake()
    {
        _globalState = GameObject.FindGameObjectWithTag("GlobalState");
    }

    private void OnTriggerEnter(Collider hitInfo)
    {
        if (hitInfo.CompareTag("Player"))
        {
            _globalState.GetComponent<CheckpointHandler>().ActivateCheckpoint(respawnPosition, maxHealthOnRespawn);
        }
    }
}