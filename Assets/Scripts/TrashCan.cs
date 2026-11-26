using UnityEngine;
using UnityEngine.EventSystems;

public class TrashCan : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        GameObject item = eventData.pointerDrag;

        if (item == null)
            return;

        Draggable draggable = item.GetComponent<Draggable>();

        if (draggable == null)
            return;

        GameAudio.Play(SoundEffect.TrashCan);
        Destroy(item);

        Debug.Log("아이템 삭제 완료");
    }
}
