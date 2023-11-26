using Unity.Netcode;
using UnityEngine;
using static VehicleSeatController;
using Cysharp.Threading.Tasks;

public class VehicleNetworkController : NetworkBehaviour
{
    private VehicleSeatController _seatController;
    private VehicleInteractionController _interactionController;

    public NetworkList<SeatData> Seats;
    public NetworkVariable<ulong> DriverClientId = new(NO_DRIVER_CLIENT_ID, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        this.Seats = new();
        this._seatController = GetComponent<VehicleSeatController>();
        this._interactionController = GetComponent<VehicleInteractionController>();
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

        this._seatController.RemovePlayer(playerClientId);
        this.ExitVehicleClientRpc(playerClientId);
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
}
