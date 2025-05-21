using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ChallengeGates.Scripts;

public class MovingSpikes: NetworkBehaviour
{
    private float moveSpeed = 2.4f;
    private float startMovingDelay = 5;

    private bool move;

    private void Update()
    {
        if (move)
        {
            transform.position += new Vector3(0,0, -moveSpeed * Time.deltaTime);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartMovingServerRpc()
    {
        StartMovingClientRpc();
    }
    
    [ClientRpc]
    void StartMovingClientRpc()
    {
        StartCoroutine(MoveDelay());
    }

    IEnumerator MoveDelay()
    {
        yield return new WaitForSeconds(startMovingDelay);
        move = true;
    }


}