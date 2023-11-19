using System;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviorAutoDisable<PlayerController>
{
    private PlayerCameraController _cameraController;
    private PlayerInput _input;
    private CharacterController _characterController;
    private ThirdPersonController _thirdPersonController;

    public static event Action<ulong, PlayerController> OnSpawn;

    private void Awake()
    {
        this._cameraController = GetComponent<PlayerCameraController>();
        this._input = GetComponent<PlayerInput>();
        this._characterController = GetComponent<CharacterController>();
        this._thirdPersonController = GetComponent<ThirdPersonController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        PlayerController.OnSpawn?.Invoke(this.OwnerClientId, this);
    }

    protected override void OnOwnerNetworkSpawn()
    {
        this._cameraController.OnThirdPersonCameraReached += this.OnThirdPersonCameraReached;
        this._characterController.enabled = true;
        this._thirdPersonController.enabled = true;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (!this.IsOwner) { return; }

        this._cameraController.OnThirdPersonCameraReached -= this.OnThirdPersonCameraReached;
    }

    private void OnThirdPersonCameraReached()
    {
        this._input.enabled = true;
    }
}
