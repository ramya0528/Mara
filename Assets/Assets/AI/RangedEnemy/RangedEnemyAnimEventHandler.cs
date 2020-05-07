﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangedEnemyAnimEventHandler : MonoBehaviour
{
    [SerializeField] private RangedEnemyAI parentAI;

    public void FireEvent()
    {
        parentAI.Fire();
    }

    public void TurnInterruptEvent()
    {
        parentAI._canTurn = false;
    }

    public void DestroyThis()
    {
        Destroy(this.parentAI.gameObject);
    }
}
