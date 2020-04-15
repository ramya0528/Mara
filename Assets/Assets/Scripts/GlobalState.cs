﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalState : MonoBehaviour
{
    [SerializeField]
    private GameObject _player;

    [SerializeField]
    private Camera _camera;

    [SerializeField]
    private AudioManager _audioManager;

    public GameObject Player
    {
        get { return _player; }
    }

    public Camera Camera
    {
        get { return _camera; }
    }

    public AudioManager AudioManager
    {
        get { return _audioManager;  }
    }

    private static GlobalState _state;
    public static GlobalState state {
        get { return _state; }
    }

    [SerializeField] private GameObject _playerMesh;
    public GameObject PlayerMesh {
        get { return _playerMesh; }
    }

    private void Awake() {
        if (_state != null && _state != this) {
            Destroy(this.gameObject);
        }
        else {
            DontDestroyOnLoad(this);
            _state = this;
        }
    }

    private void OnDestroy() {
        if (this == _state) {
            _state = null;
        }
    }
}