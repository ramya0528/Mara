﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class TrackingHealthBar : MonoBehaviour
{
    private Transform _cam;

    // Get transform component if null
    public RectTransform Transform
    {
        get
        {
            if(!_transform)
                _transform = GetComponent<RectTransform>();

            return _transform;
        }
    }

    private RectTransform _transform;

    // Get slider component if null
    public Slider Slider
    {
        get
        {
            if (!_slider)
                _slider = GetComponent<Slider>();

            return _slider;
        }
    }
    private Slider _slider;

    private void Awake()
    {
        _cam = GlobalState.state.Camera.transform;
    }

    // Health bar billboarding
    private void LateUpdate()
    {
        transform.LookAt(transform.position + _cam.forward);
    }

    public void SetValue(float amount)
    {
        Slider.value = amount;
    }

    public void SetMaxValue(float amount)
    {
        Slider.maxValue = amount;
        Transform.sizeDelta = new Vector2(amount, 10);
    }
}