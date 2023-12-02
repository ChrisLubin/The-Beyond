using TMPro;
using Unity.Netcode;
using UnityEngine;

public class PlayerWorldUiController : NetworkBehaviour
{
    [SerializeField] private TextMeshPro _name;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (this.IsOwner)
            this._name.gameObject.SetActive(false);
        else
            this._name.text = MultiplayerSystem.Instance.GetPlayerUsername(this.OwnerClientId);
    }

    private void LateUpdate()
    {
        if (this.IsOwner) { return; }

        if (PlayerCameraController.LOCAL_PLAYER_CAMERA != null)
            this._name.transform.LookAt(PlayerCameraController.LOCAL_PLAYER_CAMERA.transform);
    }
}
