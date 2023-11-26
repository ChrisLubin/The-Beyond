using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class SpaceshipExteriorController : MonoBehaviour
{
    private SpaceshipMovementController _movementController;
    private VehicleSeatController _seatController;

    [SerializeField] private List<ParticleSystem> _forwardThrusters;
    [SerializeField] private float _forwardThrustersTurningOffSimSpeed = 0.25f;
    [SerializeField] private float _forwardThrusterMinSize = 0.6f;
    [SerializeField] private float _forwardThrusterMaxSize = 1.2f;
    [SerializeField] private float _forwardThrusterMinSimSpeed = 0.7f;
    [SerializeField] private float _forwardThrusterMaxSimSpeed = 1f;

    private void Awake()
    {
        this._movementController = GetComponent<SpaceshipMovementController>();
        this._seatController = GetComponent<VehicleSeatController>();
    }

    private void Update()
    {
        float forwardThrustPercent = this._movementController.ForwardThrust / this._movementController.MaxForwardThrust; // 0 - 1
        float forwardThrustSize = Mathf.Lerp(this._forwardThrusterMinSize, this._forwardThrusterMaxSize, forwardThrustPercent);
        float forwardThrustSimSpeed = Mathf.Lerp(this._forwardThrusterMinSimSpeed, this._forwardThrusterMaxSimSpeed, forwardThrustPercent);

        foreach (ParticleSystem thruster in this._forwardThrusters)
        {
            MainModule thrusterMain = thruster.main;

            if (!this._seatController.HasDriver)
            {
                thrusterMain.startSize = 0f;
                thrusterMain.simulationSpeed = this._forwardThrustersTurningOffSimSpeed;
            }
            else
            {
                thrusterMain.startSize = forwardThrustSize;
                thrusterMain.simulationSpeed = forwardThrustSimSpeed;
            }
        }
    }
}
