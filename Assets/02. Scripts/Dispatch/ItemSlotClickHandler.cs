using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Dispatch
{
    /// <summary>
    /// 아이템 슬롯 클릭 핸들러
    /// ScrollRect 내부에서도 클릭 이벤트를 안정적으로 받기 위해 IPointerClickHandler 직접 구현
    /// </summary>
    [AddComponentMenu("Dispatch/ItemSlotClickHandler")]
    public class ItemSlotClickHandler : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public event Action<int> OnSlotClicked;

        [SerializeField]
        private int slotIndex;
        public int SlotIndex
        {
            get => slotIndex;
            set => slotIndex = value;
        }

        private bool isPointerDown = false;
        private Vector2 pointerDownPosition;
        private float pointerDownTime;

        // 클릭으로 인정할 최대 이동 거리 (픽셀)
        private const float MAX_CLICK_DISTANCE = 30f;
        // 클릭으로 인정할 최대 시간 (초)
        private const float MAX_CLICK_TIME = 0.5f;

        public void OnPointerDown(PointerEventData eventData)
        {
            isPointerDown = true;
            pointerDownPosition = eventData.position;
            pointerDownTime = Time.unscaledTime;
            GameLog.Log($"[ItemSlotClickHandler] OnPointerDown - Slot {SlotIndex}");
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isPointerDown)
            {
                float distance = Vector2.Distance(pointerDownPosition, eventData.position);
                float duration = Time.unscaledTime - pointerDownTime;

                GameLog.Log($"[ItemSlotClickHandler] OnPointerUp - Slot {SlotIndex}, Distance: {distance}, Duration: {duration}");

                // 짧은 시간 내에 적은 이동이면 클릭으로 인정
                if (distance <= MAX_CLICK_DISTANCE && duration <= MAX_CLICK_TIME)
                {
                    GameLog.Log($"[ItemSlotClickHandler] 클릭 인정! Slot {SlotIndex}");
                    OnSlotClicked?.Invoke(SlotIndex);
                }
            }
            isPointerDown = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // OnPointerUp에서 이미 처리하므로 여기서는 로그만
            GameLog.Log($"[ItemSlotClickHandler] OnPointerClick - Slot {SlotIndex}");
        }
    }
}
