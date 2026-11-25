using UnityEngine;
using UnityEngine.EventSystems;

public class ScratchBotDropReceiver : MonoBehaviour, IDropHandler
{
    [SerializeField] private ScratchBot scratchBot;

    private void Awake()
    {
        if (scratchBot == null)
            scratchBot = GetComponent<ScratchBot>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (scratchBot == null || eventData.pointerDrag == null)
            return;

        GameObject draggedObj = eventData.pointerDrag;
        if (draggedObj.GetComponent<Draggable>() == null)
            return;

        scratchBot.TryAbsorbLottery(draggedObj);
    }
}

