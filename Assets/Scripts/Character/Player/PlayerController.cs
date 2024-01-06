using System;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviorAutoDisableWithLogger<PlayerController>, IGravityWellObject
{
    private PlayerCameraController _cameraController;
    private CharacterController _characterController;
    private ThirdPersonController _thirdPersonController;
    private NetworkTransform _networkTransform;

    public static event Action<ulong, PlayerController> OnSpawn;

    protected override void Awake()
    {
        base.Awake();
        this._cameraController = GetComponent<PlayerCameraController>();
        this._characterController = GetComponent<CharacterController>();
        this._thirdPersonController = GetComponent<ThirdPersonController>();
        this._networkTransform = GetComponent<NetworkTransform>();
    }

    public async override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PlayerController.OnSpawn?.Invoke(this.OwnerClientId, this);

        if (!this.IsOwner)
        {
            await UniTask.Delay(3000);
            this._characterController.enabled = true;
        }
    }

    protected override void OnOwnerNetworkSpawn()
    {
        this._cameraController.OnThirdPersonCameraReached += this.OnThirdPersonCameraReached;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (!this.IsOwner) { return; }

        this._cameraController.OnThirdPersonCameraReached -= this.OnThirdPersonCameraReached;
    }

    private void OnThirdPersonCameraReached()
    {
        InputSystem.isEnabled = true;
        this._characterController.enabled = true;
        this._thirdPersonController.enabled = true;
    }
    public bool CanBeReParented() => !this._networkTransform.InLocalSpace;

    private void OnTransformParentChanged()
    {
        if (!this.IsOwner) { return; }
        bool isParented = transform.parent != null;
        this._networkTransform.InLocalSpace = isParented;

        if (transform.parent != null && transform.parent.CompareTag(Constants.TagNames.GravityWellContainer))
            this._logger.Log("Entered gravity well");
    }
}
