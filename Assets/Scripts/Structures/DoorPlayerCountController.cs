using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DoorPlayerCountController : MonoBehaviour
{
    private List<ulong> _playerIdsInDoorway = new();
    public event Action<int, int> OnPlayersInDoorwayChange;

    private void OnPlayerEnterOrExit(int oldCount) => this.OnPlayersInDoorwayChange?.Invoke(oldCount, this._playerIdsInDoorway.Count);

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag != Constants.TagNames.Player) { return; }
        if (!other.TryGetComponent<NetworkObject>(out NetworkObject networkObject)) { return; }
        ulong clientId = networkObject.OwnerClientId;
        if (this.IsInDoorWay(clientId)) { return; }

        this._playerIdsInDoorway.Add(clientId);
        this.OnPlayerEnterOrExit(this._playerIdsInDoorway.Count - 1);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag != Constants.TagNames.Player) { return; }
        if (!other.TryGetComponent<NetworkObject>(out NetworkObject networkObject)) { return; }
        ulong clientId = networkObject.OwnerClientId;
        if (!this.IsInDoorWay(clientId)) { return; }

        this._playerIdsInDoorway.Remove(clientId);
        this.OnPlayerEnterOrExit(this._playerIdsInDoorway.Count + 1);
    }

    private bool IsInDoorWay(ulong clientId) => this._playerIdsInDoorway.Exists(playerId => playerId == clientId);
}
