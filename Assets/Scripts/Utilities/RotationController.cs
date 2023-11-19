using Unity.Netcode;
using UnityEngine;

public class RotationController : NetworkBehaviour
{
    [SerializeField] private float _rotationSpeed = 1f;

    private void Update()
    {
        if (!this.IsHost) { return; }

        transform.Rotate(0f, this._rotationSpeed * Time.deltaTime, 0f);
    }
}
