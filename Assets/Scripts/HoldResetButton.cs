using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class HoldResetButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private PausePanelController pausePanelController;

    private Button targetButton;
    private bool isPressed;

    private void Awake()
    {
        targetButton = GetComponent<Button>();
        if (pausePanelController == null)
            pausePanelController = FindFirstObjectByType<PausePanelController>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetButton != null && !targetButton.interactable)
            return;

        isPressed = true;
        pausePanelController?.BeginResetHold();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;
        isPressed = false;
        pausePanelController?.ReleaseResetHold();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isPressed) return;
        isPressed = false;
        pausePanelController?.ReleaseResetHold();
    }
}