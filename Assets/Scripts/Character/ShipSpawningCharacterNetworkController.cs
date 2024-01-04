using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShipSpawningCharacterNetworkController : NetworkBehaviour
{
    private ShipSpawningCharacterController _controller;

    [SerializeField] private Transform _spaceshipPrefab;
    [SerializeField] private Transform _spawnContainer;
    [SerializeField] private List<Transform> _spawnableSpaceships;

    public NetworkVariable<int> SpawnableSpaceshipCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        this._controller = GetComponent<ShipSpawningCharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!this.IsOwner) { return; }
        this.SpawnableSpaceshipCount.Value = this._spawnableSpaceships.Count;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnSpaceshipServerRpc()
    {
        if (!this._controller.CanSpawn()) { return; }

        Transform spaceship = this._spawnableSpaceships[0];
        this._spawnableSpaceships.RemoveAt(0);
        spaceship.forward = this._controller.SpawnPointForward;
        spaceship.position = this._controller.SpawnPoint;
        this.SpawnableSpaceshipCount.Value = this._spawnableSpaceships.Count;
    }
}
