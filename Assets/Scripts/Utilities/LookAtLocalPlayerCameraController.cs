using UnityEngine;

public class LookAtLocalPlayerCameraController : MonoBehaviour
{
    private void LateUpdate()
    {
        if (PlayerCameraController.LOCAL_PLAYER_CAMERA != null)
            transform.LookAt(PlayerCameraController.LOCAL_PLAYER_CAMERA.transform);
    }
}
