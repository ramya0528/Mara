﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class MenuInputResource : MonoBehaviour
{
    public static float cameraSensitivity;
    public static float lockonCameraSensitivity;

    public static PlayerInput _playerInput;
    public static PlayerInput PlayerInput
    {
        get
        {
            if (_playerInput == null)
            {
                _playerInput = new PlayerInput();
                _playerInput.PlayerControls.Enable();
            }
            else if (!_playerInput.PlayerControls.enabled)
            {
                _playerInput.PlayerControls.Enable();
            }
            return _playerInput;
        }
    }

    public static void SaveOverrides()
    {
        overrides = new Dictionary<System.Guid, string>();
        foreach (var map in PlayerInput.asset.actionMaps) // https://forum.unity.com/threads/saving-user-bindings.805722/#post-5384310
        {
            foreach (var binding in map.bindings)
            {
                if (!string.IsNullOrEmpty(binding.overridePath))
                {
                    overrides[binding.id] = binding.overridePath;

                    //binding.id.ToByteArray(); // key
                    //overrides[binding.id] // value
                }
            }
        }

        OptionData data = new OptionData(overrides);
        
        SaveData.Save_Data(data);

        PlayerInput.PlayerControls.Disable();
    }


    public static void LoadOverrides(ref PlayerInput input, Dictionary<System.Guid, string> overrides) // temp
    {
        foreach (var map in input.asset.actionMaps)
        {
            var bindings = map.bindings;
            for (var i = 0; i < bindings.Count; ++i)
            {
                if (overrides.TryGetValue(bindings[i].id, out var overridePath))
                    map.ApplyBindingOverride(i, new InputBinding { overridePath = overridePath });
            }
        }
    }
}