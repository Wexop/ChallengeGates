using UnityEngine;

namespace ChallengeGates.Scripts;

public class ChallengeLevelMovingSpikes : ChallengeLevel
{
    public Transform spikesPosition;
    
    public MovingSpikes spikes;

    public override void OnSpawnServerExtra()
    {
        var spikesObject = Instantiate(ChallengeGatesPlugin.instance.movingSpikesObject, spikesPosition.position, spikesPosition.rotation);
        spikes = spikesObject.GetComponent<MovingSpikes>();
        spikes.NetworkObject.Spawn();
    }

    public override void OnPlayerEnter(ulong playerId)
    {
        base.OnPlayerEnter(playerId);
        if(spikes != null) spikes.StartMovingServerRpc();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Destroy(spikes.gameObject);
    }
}