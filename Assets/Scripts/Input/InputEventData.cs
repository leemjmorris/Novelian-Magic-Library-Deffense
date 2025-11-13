using UnityEngine;

namespace NovelianMagicLibraryDefense.Input
{
    //JML: Touch/Tap event data structure
    public struct TouchEventData
    {
        public Vector2 position;
        public float timestamp;
    }

    //JML: Drag event data structure with start, current position and delta
    public struct DragEventData
    {
        public Vector2 startPosition;
        public Vector2 currentPosition;
        public Vector2 delta;
        public float timestamp;
    }

    //JML: Long press event data structure with duration tracking
    public struct LongPressEventData
    {
        public Vector2 position;
        public float duration;
        public float timestamp;
    }

    //JML: Swipe direction enumeration for directional input detection
    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }

    //JML: Swipe event data structure with direction, distance and duration
    public struct SwipeEventData
    {
        public Vector2 startPosition;
        public Vector2 endPosition;
        public SwipeDirection direction;
        public float distance;
        public float duration;
        public float timestamp;
    }
}