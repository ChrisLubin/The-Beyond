using Unity.Netcode;
using UnityEngine;

public class PlayerVisualController : NetworkBehaviour
{
    private PlayerNetworkController _networkController;
    private SkinnedMeshRenderer[] _skinnedMeshes;

    private void Awake()
    {
        this._networkController = GetComponent<PlayerNetworkController>();
        this._skinnedMeshes = GetComponentsInChildren<SkinnedMeshRenderer>();
        this._networkController.PlayerSkinnedMeshIndex.OnValueChanged += this.UpdateMesh;
    }

    public override void OnNetworkSpawn()
    {
        if (this.IsOwner)
            this.RandomizeSkinnedMesh();
        else
            this.UpdateMesh(0, this._networkController.PlayerSkinnedMeshIndex.Value);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        this._networkController.PlayerSkinnedMeshIndex.OnValueChanged -= this.UpdateMesh;
    }

    private void RandomizeSkinnedMesh()
    {
        if (!this.IsOwner) { return; }

        int randomIndex = Random.Range(0, this._skinnedMeshes.Length);
        this._networkController.PlayerSkinnedMeshIndex.Value = randomIndex;
        this.UpdateMesh(0, randomIndex);
    }

    private void UpdateMesh(int _, int meshIndex)
    {
        for (int i = 0; i < this._skinnedMeshes.Length; i++)
        {
            SkinnedMeshRenderer skinnedMesh = this._skinnedMeshes[i];
            skinnedMesh.enabled = i == meshIndex;
        }
    }
}
