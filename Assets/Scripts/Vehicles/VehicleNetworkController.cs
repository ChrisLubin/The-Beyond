using Unity.Netcode;
using UnityEngine;
using static VehicleSeatController;

public class VehicleNetworkController : NetworkBehaviour
{
    private VehicleSeatController _seatController;
    private VehicleInteractionController _interactionController;
    private SpaceshipMovementController _movementController;

    public NetworkList<SeatData> Seats;
    public NetworkVariable<ulong> DriverClientId = new(EMPTY_SEAT_PLAYER_ID, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> Velocity = new(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        this.Seats = new();
        this._seatController = GetComponent<VehicleSeatController>();
        this._interactionController = GetComponent<VehicleInteractionController>();
        this._movementController = GetComponent<SpaceshipMovementController>();
    }

    [ServerRpc(RequireOwnership = false)]
    public void EnterVehicleServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong playerClientId = serverRpcParams.Receive.SenderClientId;
        if (!this._seatController.CanAddPlayer(playerClientId)) { return; }

        this._seatController.AddPlayer(playerClientId);
        this.EnterVehicleClientRpc(playerClientId);
    }

    [ClientRpc]
    public void EnterVehicleClientRpc(ulong playerId)
    {
        if (playerId == MultiplayerSystem.LocalClientId)
            this._interactionController.DidInteraction(InteractionType.EnterVehicle);
        else if (PlayerManager.Instance.TryGetPlayer(playerId, out PlayerController player))
            player.GetComponent<CharacterController>().enabled = false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ExitVehicleServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong playerClientId = serverRpcParams.Receive.SenderClientId;
        if (!this._seatController.CanRemovePlayer(playerClientId)) { return; }
        bool wasPlayerDriver = this.DriverClientId.Value == playerClientId;

        this._seatController.RemovePlayer(playerClientId);
        this.ExitVehicleClientRpc(playerClientId);

        if (wasPlayerDriver)
            this._movementController.SetRigidBodyVelocity();
    }

    [ClientRpc]
    public void ExitVehicleClientRpc(ulong playerId)
    {
        if (playerId == MultiplayerSystem.LocalClientId)
            this._interactionController.DidInteraction(InteractionType.ExitVehicle);
        else if (!this._seatController.isInVehicle(playerId) && PlayerManager.Instance.TryGetPlayer(playerId, out PlayerController player))
            player.GetComponent<CharacterController>().enabled = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeSeatServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong playerClientId = serverRpcParams.Receive.SenderClientId;
        if (!this._seatController.CanChangeSeat(playerClientId)) { return; }
        bool wasPlayerDriver = this.DriverClientId.Value == playerClientId;

        this._seatController.ChangeSeat(playerClientId);

        if (wasPlayerDriver)
            this._movementController.SetRigidBodyVelocity();

        bool isPlayerNowDriver = this.DriverClientId.Value == playerClientId;
        if (isPlayerNowDriver)
            this.ChangeSeatClientRpc(playerClientId.GenerateRpcParamsToClient());
    }

    [ClientRpc]
    public void ChangeSeatClientRpc(ClientRpcParams _ = default) => this._movementController.SetRigidBodyVelocity();
}
