using Unity.Netcode;
using UnityEngine;
using static VehicleSeatController;
using Cysharp.Threading.Tasks;

public class VehicleNetworkController : NetworkBehaviour
{
    private VehicleSeatController _seatController;
    private VehicleInteractionController _interactionController;
    private SpaceshipMovementController _movementController;

    public NetworkList<SeatData> Seats;
    public NetworkVariable<ulong> DriverClientId = new(EMPTY_SEAT_PLAYER_ID, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
    public void ExitVehicleServerRpc(Vector3 vehicleVelocity = default, ServerRpcParams serverRpcParams = default)
    {
        ulong playerClientId = serverRpcParams.Receive.SenderClientId;
        if (!this._seatController.CanRemovePlayer(playerClientId)) { return; }
        bool wasPlayerDriver = this.DriverClientId.Value == playerClientId;

        this._seatController.RemovePlayer(playerClientId);
        this.ExitVehicleClientRpc(playerClientId);

        if (wasPlayerDriver)
            this._movementController.SetVelocity(vehicleVelocity);
    }

    [ClientRpc]
    public void ExitVehicleClientRpc(ulong playerId) => this.ExitVehicle(playerId);

    private async void ExitVehicle(ulong playerId)
    {
        if (playerId == MultiplayerSystem.LocalClientId)
            this._interactionController.DidInteraction(InteractionType.ExitVehicle);
        else
        {
            await UniTask.WaitForSeconds(3f);
            if (!this._seatController.isInVehicle(playerId) && PlayerManager.Instance.TryGetPlayer(playerId, out PlayerController player))
                player.GetComponent<CharacterController>().enabled = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeSeatServerRpc(Vector3 clientVehicleVelocity = default, ServerRpcParams serverRpcParams = default)
    {
        ulong playerClientId = serverRpcParams.Receive.SenderClientId;
        if (!this._seatController.CanChangeSeat(playerClientId)) { return; }
        bool wasPlayerDriver = this.DriverClientId.Value == playerClientId;
        Vector3 hostVehicleVelocity = this._movementController.Velocity;

        this._seatController.ChangeSeat(playerClientId);

        if (wasPlayerDriver)
            this._movementController.SetVelocity(clientVehicleVelocity);

        bool isPlayerNowDriver = this.DriverClientId.Value == playerClientId;
        if (isPlayerNowDriver)
            this.ChangeSeatClientRpc(hostVehicleVelocity, playerClientId.GenerateRpcParamsToClient());
    }

    [ClientRpc]
    public void ChangeSeatClientRpc(Vector3 vehicleVelocity, ClientRpcParams _ = default) => this._movementController.SetVelocity(vehicleVelocity);
}
