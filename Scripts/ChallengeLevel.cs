using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace ChallengeGates.Scripts;

public class ChallengeLevel : MonoBehaviour
{

    public int id;
    
    public Transform playerSpawnPos;
    public Transform trophySpawnPos;

    public List<UnityEvent<PlayerControllerB>> onPlayerEscape;

    public string enterNotification;

    private GameObject trophy;
    public Trophy trophyScript;

    public Vector3 connectedPosition;

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

        //player.transform.position = RoundManager.FindMainEntrancePosition(false, true);
        player.transform.position = connectedPosition;
        
        Destroy(gameObject);

    }

    public virtual void OnPlayerEnter(ulong playerId)
    {

        if(enterNotification.Length > 0)
        {
           if(GameNetworkManager.Instance.localPlayerController.playerClientId == playerId) HUDManager.Instance.DisplayTip("Warning", enterNotification);
        }
    }

    public virtual void OnDestroy()
    {
        //ChallengeGatesPlugin.instance.numberOfRoom--;
        ChallengeGatesPlugin.instance.spawnedRooms.Remove(id);
        if (GameNetworkManager.Instance.localPlayerController.IsServer)
        {
            try
            {
                if(trophyScript.scrapValue == ChallengeGatesPlugin.instance.baseTrophyValue.Value) trophy.GetComponent<NetworkObject>().Despawn();

            }
            catch
            {
                // ignored
            }
        }
    }
}