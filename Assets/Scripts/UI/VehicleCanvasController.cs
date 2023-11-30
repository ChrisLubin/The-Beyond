using UnityEngine;
using UnityEngine.UI;

public class VehicleCanvasController : MonoBehaviour
{
    [SerializeField] private GameObject _spaceshipUi;
    [SerializeField] private Image _throttleScale;
    [SerializeField] private Image _boostScale;

    private void Update()
    {
        if ((this._spaceshipUi.activeSelf && !SpaceshipMovementController.IsLocalPlayerDriver) || (!this._spaceshipUi.activeSelf && SpaceshipMovementController.IsLocalPlayerInVehicle && SpaceshipMovementController.IsLocalPlayerDriver))
            this._spaceshipUi.SetActive(SpaceshipMovementController.IsLocalPlayerInVehicle && SpaceshipMovementController.IsLocalPlayerDriver);

        if (!SpaceshipMovementController.IsLocalPlayerInVehicle || !SpaceshipMovementController.IsLocalPlayerDriver) { return; }

        this._throttleScale.fillAmount = SpaceshipMovementController.CurrentThrottle;
        this._boostScale.fillAmount = SpaceshipMovementController.CurrentBoost;
    }
}
