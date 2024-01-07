using System;
using Cysharp.Threading.Tasks;
using StarterAssets;
using Unity.Netcode.Components;

public class PlayerResetSpawnController : NetworkBehaviorAutoDisable<PlayerResetSpawnController>
{
    private NetworkTransform _networkTransform;

    public event Action OnSpawnReset;

    private void Awake()
    {
        this._networkTransform = GetComponent<NetworkTransform>();
    }

    protected async override void OnOwnerNetworkSpawn()
    {
        await UniTask.Delay(300);
        this.TeleportToSpawnPoint();
        await UniTask.Delay(1000);
        this._networkTransform.Interpolate = true;
    }

    private async void Update()
    {
        if (!this.IsOwner) { return; }

        if (ThirdPersonController.LocalPlayerCurrentFallDuration > BlackOutCanvasController.FALL_DURATION_BLACK_OUT_MAX_TIME)
        {
            this._networkTransform.Interpolate = false;
            this.TeleportToSpawnPoint();
            this.OnSpawnReset?.Invoke();
            await UniTask.Delay(100);
            this._networkTransform.Interpolate = true;
        }
    }

    private void TeleportToSpawnPoint() => transform.position = PlayerManager.Instance.GetSpawnPoint(this.OwnerClientId);
}
