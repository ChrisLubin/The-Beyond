using Unity.Netcode;
using static VehicleSeatController;

public class VehicleNetworkController : NetworkBehaviour
{
    private VehicleSeatController _seatController;
    private VehicleInteractionController _interactionController;

    public NetworkList<SeatData> Seats;

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
        this.EnterVehicleClientRpc(playerClientId.GenerateRpcParamsToClient());
    }

    [ClientRpc]
    public void EnterVehicleClientRpc(ClientRpcParams _) => this._interactionController.DidInteraction(InteractionType.EnterVehicle);

    [ServerRpc(RequireOwnership = false)]
    public void ExitVehicleServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong playerClientId = serverRpcParams.Receive.SenderClientId;
        if (!this._seatController.CanRemovePlayer(playerClientId)) { return; }

        this._seatController.RemovePlayer(playerClientId);
        this.ExitVehicleClientRpc(playerClientId.GenerateRpcParamsToClient());
    }

    [ClientRpc]
    public void ExitVehicleClientRpc(ClientRpcParams _) => this._interactionController.DidInteraction(InteractionType.ExitVehicle);
}
