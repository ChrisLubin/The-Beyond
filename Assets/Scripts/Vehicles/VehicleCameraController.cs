using Cinemachine;
using UnityEngine;

public class VehicleCameraController : MonoBehaviour
{
    private VehicleInteractionController _interactionController;

    [SerializeField] private CinemachineVirtualCamera _camera;

    private void Awake()
    {
        this._interactionController = GetComponent<VehicleInteractionController>();
        this._interactionController.OnDidInteraction += this.OnDidInteraction;
    }

    private void OnDestroy()
    {
        this._interactionController.OnDidInteraction -= this.OnDidInteraction;
    }

    private void OnDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this._camera.enabled = true;
                break;
            case InteractionType.ExitVehicle:
                this._camera.enabled = false;
                break;
            default:
                break;
        }
    }
}
