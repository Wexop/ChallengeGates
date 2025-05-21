using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ChallengeGates.Scripts;

public class ChallengeGate : NetworkBehaviour
{
    private static readonly int Close = Animator.StringToHash("close");
    public Animator animator;
    
    public List<GameObject> levelObjects;
    private ChallengeLevel level;

    private bool gateIsEnabled = true;

    private void Start()
    {
        if(IsServer) SpawnLevelServerRpc(Random.Range(0, levelObjects.Count), ChallengeGatesPlugin.instance.GetNewRoomPosY());
    }

    [ServerRpc]
    void SpawnLevelServerRpc(int index, float pos)
    {
        SpawnLevelClientRpc(index, pos);
    }

    [ClientRpc]
    void SpawnLevelClientRpc(int index, float pos)
    {
        var levelObject = levelObjects[index];
        
        var o = Instantiate(levelObject, transform.position + (Vector3.down * pos), Quaternion.identity);
        level = o.GetComponent<ChallengeLevel>();
        
        if (IsServer)
        {
            level.OnSpawnServer();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if(!level || !gateIsEnabled) return;
        if (other.CompareTag("Player"))
        {
            PlayerControllerB player = other.GetComponent<PlayerControllerB>();
            if (player != null && !player.isPlayerDead && player.playerClientId == GameNetworkManager.Instance.localPlayerController.playerClientId)
            {
                OnPlayerCollideServerRpc(GameNetworkManager.Instance.localPlayerController.playerClientId);
                player.disableMoveInput = true;
                player.DropAllHeldItems();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void OnPlayerCollideServerRpc(ulong playerId)
    {
        OnPlayerCollideClientRpc(playerId);
    }
    
    [ClientRpc]
    void OnPlayerCollideClientRpc(ulong playerId)
    {
        animator.SetTrigger(Close);
        gateIsEnabled = false;
        StartCoroutine(TeleportationAnimation(playerId));
    }

    IEnumerator TeleportationAnimation(ulong playerId)
    {
        yield return new WaitForSeconds(1f);
        
        if(IsServer) level.trophyScript.SetTimerStateServerRpc(true);

        StartOfRound.Instance.allPlayerScripts.ToList().ForEach(player =>
        {
            if (player.playerClientId == playerId)
            {
                player.transform.position = level.playerSpawnPos.position;
                player.disableMoveInput = false;
                player.sprintMeter = 1f;
                level.OnPlayerEnter(playerId);
            }
        });

    }

    public override void OnDestroy()
    {
        Destroy(level.gameObject);
        base.OnDestroy();
    }
}