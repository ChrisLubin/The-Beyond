using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class BlackOutCanvasController : MonoBehaviour
{
    [SerializeField] private Image _overlay;

    private const float _FALL_DURATION_BLACK_OUT_MIN_TIME = 5f;
    public const float FALL_DURATION_BLACK_OUT_MAX_TIME = 10f;

    private void Awake() => MultiplayerSystem.OnStateChange += this.OnMultiplayerStateChange;
    private void OnDestroy() => MultiplayerSystem.OnStateChange -= this.OnMultiplayerStateChange;

    private void Update()
    {
        if (ThirdPersonController.LocalPlayerCurrentFallDuration <= _FALL_DURATION_BLACK_OUT_MIN_TIME)
        {
            this._overlay.color = new Color(0f, 0f, 0f, 0f);
            return;
        }
        if (ThirdPersonController.LocalPlayerCurrentFallDuration >= FALL_DURATION_BLACK_OUT_MAX_TIME)
        {
            this._overlay.color = new Color(0f, 0f, 0f, 1f);
            return;
        }

        this._overlay.color = new Color(0f, 0f, 0f, Mathf.InverseLerp(_FALL_DURATION_BLACK_OUT_MIN_TIME, FALL_DURATION_BLACK_OUT_MAX_TIME, ThirdPersonController.LocalPlayerCurrentFallDuration));
    }

    private void OnMultiplayerStateChange(MultiplayerState state)
    {
        switch (state)
        {
            case MultiplayerState.CreatingLobby:
                this._overlay.gameObject.SetActive(true);
                break;
            case MultiplayerState.JoiningLobby:
                this._overlay.gameObject.SetActive(true);
                break;
            default:
                break;
        }
    }
}
