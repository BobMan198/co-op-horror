using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyColliderKill : MonoBehaviour
{
    private EnemyMovement enemyScript;

    void Start()
    {
        enemyScript = transform.parent.GetComponent<EnemyMovement>();
    }
}
