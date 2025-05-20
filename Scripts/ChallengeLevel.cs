using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using Vector3 = System.Numerics.Vector3;

namespace ChallengeGates.Scripts;

public class ChallengeLevel : MonoBehaviour
{
    public Transform playerSpawnPos;
    public Transform trophySpawnPos;

    public List<UnityEvent<PlayerControllerB>> onPlayerEscape;

    public string enterNotification;

    private GameObject trophy;
    public Trophy trophyScript;

    public virtual void OnSpawnServerExtra()
    {
        // for child class
    }
    

    public virtual void OnSpawnServer()
    {
        trophy = Instantiate(ChallengeGatesPlugin.instance.trophyGameObject, trophySpawnPos.position, Quaternion.identity);
        trophy.GetComponent<NetworkObject>().Spawn();
        trophyScript = trophy.GetComponent<Trophy>();
        OnSpawnServerExtra();
    }

    public void OnPlayerEscape(PlayerControllerB player)
    {
        onPlayerEscape.ForEach(e => e.Invoke(player));

        player.transform.position = RoundManager.FindMainEntrancePosition(false, true);

    }

    public void OnPlayerEnter()
    {

        if(enterNotification.Length > 0)
        {
            HUDManager.Instance.DisplayTip("Warning", enterNotification);
        }
    }

    private void OnDestroy()
    {
        if (GameNetworkManager.Instance.localPlayerController.IsServer)
        {
            trophy.GetComponent<NetworkObject>().Despawn();
        }
    }
}