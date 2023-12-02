using UnityEngine;
using Cinemachine;
using System;

public class PlayerCameraController : NetworkBehaviorAutoDisable<PlayerCameraController>
{
    [SerializeField] private CinemachineVirtualCamera _firstPersonCamera;
    [SerializeField] private CinemachineVirtualCamera _thirdPersonCamera;
    [SerializeField] private CinemachineVirtualCamera _hoverBehindCamera;

    public event Action OnThirdPersonCameraReached;

    public static CinemachineVirtualCamera LOCAL_PLAYER_CAMERA { get; private set; }

    protected override void OnOwnerNetworkSpawn()
    {
        CinemachineBlendEventController.OnBlendFinished += this.OnBlendFinished;
        this._hoverBehindCamera.enabled = true;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (!this.IsOwner) { return; }

        CinemachineBlendEventController.OnBlendFinished -= this.OnBlendFinished;
    }

    private void Update()
    {
        if (!this.IsOwner) { return; }

        PlayerCameraController.LOCAL_PLAYER_CAMERA = this._firstPersonCamera.enabled ? this._firstPersonCamera : this._thirdPersonCamera;
    }

    private void OnBlendFinished(ICinemachineCamera blendedCamera)
    {
        if (blendedCamera.Name != this._hoverBehindCamera.Name) { return; }

        this._thirdPersonCamera.enabled = true;
        this.OnThirdPersonCameraReached?.Invoke();
    }
}
