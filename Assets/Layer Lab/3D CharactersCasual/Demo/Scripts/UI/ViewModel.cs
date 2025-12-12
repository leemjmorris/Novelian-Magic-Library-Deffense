using UnityEngine;
using UnityEngine.EventSystems;

namespace Layer_lab._3D_Casual_Character
{
    public class ViewModel : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private float rotationSpeed = 0.18f;
        private bool _isDragging;
        private Vector2 _startDragPos;

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            _startDragPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isDragging)
            {
                Vector2 currentDragPos = eventData.position;
                Vector2 dragDelta = currentDragPos - _startDragPos;

                float rotationX = dragDelta.x * rotationSpeed;
                CharacterBase.Instance.transform.Rotate(Vector3.up, -rotationX, Space.World);

                _startDragPos = currentDragPos;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
        }
    }
}