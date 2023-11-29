using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GravityWellController : NetworkBehaviorAutoDisable<GravityWellController>
{
    [SerializeField] private Collider _collider;
    [SerializeField] private Transform _wellObjectContainer;

    private List<IGravityWellObject> _wellObjectsNotParentedHere = new();

    protected override void OnOwnerNetworkSpawn() => this._collider.enabled = true;

    private void Update()
    {
        if (!this.IsOwner) { return; }
        List<IGravityWellObject> toRemove = new();

        foreach (IGravityWellObject wellObject in this._wellObjectsNotParentedHere)
        {
            if (!wellObject.CanBeReParented()) { continue; }
            if (!wellObject.transform.TryGetComponent<NetworkObject>(out NetworkObject networkObject)) { continue; }

            networkObject.TrySetParent(this._wellObjectContainer);
            toRemove.Add(wellObject);
        }

        foreach (IGravityWellObject wellObject in toRemove)
            this._wellObjectsNotParentedHere.Remove(wellObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!this.IsHost) { return; }
        if (!other.TryGetComponent<IGravityWellObject>(out IGravityWellObject wellObject)) { return; }
        if (this._wellObjectsNotParentedHere.Contains(wellObject)) { return; }
        if (wellObject.transform.parent == this._wellObjectContainer) { return; }

        this._wellObjectsNotParentedHere.Add(wellObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!this.IsHost) { return; }
        if (!other.TryGetComponent<IGravityWellObject>(out IGravityWellObject wellObject)) { return; }

        if (this._wellObjectsNotParentedHere.Contains(wellObject))
            this._wellObjectsNotParentedHere.Remove(wellObject);

        if (wellObject.transform.parent != this._wellObjectContainer) { return; }
        if (!other.TryGetComponent<NetworkObject>(out NetworkObject networkObject)) { return; }

        networkObject.TryRemoveParent();
    }
}
