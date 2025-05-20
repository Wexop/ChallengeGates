using Unity.Netcode;
using UnityEngine;

namespace ChallengeGates.Scripts;

public class Trophy : GrabbableObject
{

    private float valueTimer;
    private float valueDelay;

    private bool timerIsRunning = false;
    public override void Start()
    {
        base.Start();
        if(IsServer)
        {
            valueDelay = ChallengeGatesPlugin.instance.trophyDecreaseDelay.Value;
            valueTimer = valueDelay;
            SetValueServerRpc(ChallengeGatesPlugin.instance.baseTrophyValue.Value);
        }
    }

    public override void Update()
    {
        base.Update();

        if (!IsServer || !timerIsRunning) return;
        
            
        valueTimer -= Time.deltaTime;

        if (scrapValue > 0 && valueTimer <= 0 )
        {
            valueTimer = valueDelay;
            var value = scrapValue - ChallengeGatesPlugin.instance.trophyDecreaseAmount.Value;
            SetValueServerRpc(value > 0 ? value : 0);
            
        }
        
    }

    public override void GrabItem()
    {
        base.GrabItem();
        SetTimerStateServerRpc(false);
    }

    [ServerRpc]
    public void SetValueServerRpc(int value)
    {
        SetValueClientRpc(value);
    }
    
    [ClientRpc]
    public void SetValueClientRpc(int value)
    {
        if(ChallengeGatesPlugin.instance.debug.Value) Debug.Log($"TROPHY CHANGED VALUE {value}");
        SetScrapValue(value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetTimerStateServerRpc(bool running)
    {
        timerIsRunning = running;
    }
    
}