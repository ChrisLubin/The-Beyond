using System;
using UnityEngine;

public class VehicleInteractionController : NetworkBehaviourWithLogger<VehicleInteractionController>, IInteractable
{
    private VehicleNetworkController _networkController;
    private VehicleSeatController _seatController;
    private SpaceshipMovementController _movementController;

    public event Action<InteractionType> OnDidInteraction;

    protected override void Awake()
    {
        base.Awake();
        this._networkController = GetComponent<VehicleNetworkController>();
        this._seatController = GetComponent<VehicleSeatController>();
        this._movementController = GetComponent<SpaceshipMovementController>();
    }

    private void Update()
    {
        if (!this._seatController.IsLocalPlayerInVehicle) { return; }

        if (Input.GetKeyDown(KeyCode.T))
        {
            this._logger.Log("Local player is trying to exit vehicle");
            this._networkController.ExitVehicleServerRpc(this._movementController.Velocity);
        }
    }

    public void DoInteract()
    {
        if (!this._seatController.HasAvailableSeat) { return; }

        this._logger.Log("Local player is trying to enter vehicle");
        this._networkController.EnterVehicleServerRpc();
    }

    public void DidInteraction(InteractionType interactionType)
    {
        this._logger.Log($"Local player {(interactionType == InteractionType.EnterVehicle ? "entered" : "exited")} vehicle");
        this.OnDidInteraction?.Invoke(interactionType);
        PlayerManager.LocalPlayerInteractorController.DidInteraction(interactionType);
    }
}
