using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerNetworkController : NetworkBehaviorAutoDisable<PlayerNetworkController>
{
    private NetworkTransform _networkTransform;
    private PlayerInteractorController _interactorController;

    public NetworkVariable<int> PlayerSkinnedMeshIndex = new(12, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private void Awake()
    {
        this._networkTransform = GetComponent<NetworkTransform>();
        this._interactorController = GetComponent<PlayerInteractorController>();
    }

    protected override void OnOwnerNetworkSpawn()
    {
        this._interactorController.OnDidInteraction += this.OnDidInteraction;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (this.IsOwner)
            this._interactorController.OnDidInteraction -= this.OnDidInteraction;
    }

    private void OnDidInteraction(InteractionType interaction)
    {
        switch (interaction)
        {
            case InteractionType.EnterVehicle:
                this._networkTransform.Interpolate = false;
                break;
            case InteractionType.ExitVehicle:
                this._networkTransform.Interpolate = true;
                break;
            default:
                break;
        }
    }
}
