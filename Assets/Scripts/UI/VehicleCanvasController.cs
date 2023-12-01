using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VehicleCanvasController : MonoBehaviour
{
    [SerializeField] private GameObject _throttleUi;
    [SerializeField] private GameObject _boostUi;
    [SerializeField] private GameObject _velocityUi;
    [SerializeField] private Image _throttleScale;
    [SerializeField] private Image _boostScale;
    [SerializeField] private TextMeshProUGUI _currentVelocityMph;

    private void Update()
    {
        this._throttleUi.SetActive(SpaceshipMovementController.IsLocalPlayerInVehicle && SpaceshipMovementController.IsLocalPlayerDriver);
        this._boostUi.SetActive(SpaceshipMovementController.IsLocalPlayerInVehicle && SpaceshipMovementController.IsLocalPlayerDriver);
        this._velocityUi.SetActive(SpaceshipMovementController.IsLocalPlayerInVehicle);

        this._throttleScale.fillAmount = SpaceshipMovementController.CurrentThrottle;
        this._boostScale.fillAmount = SpaceshipMovementController.CurrentBoost;
        this._currentVelocityMph.text = ((int)SpaceshipMovementController.CurrentVelocityMph).ToString();
    }
}
