using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GraphicsCat
{
    public class IMGUIEventBlocker
    {
        List<EventSystem> m_DisabledEventSystems = new();

        public void OnGUI(Rect blockRect)
        {
            if (blockRect.Contains(Event.current.mousePosition * GameViewUtils.GetGameViewScale()))
            {
                BlockCurrentEventSystem();
            }
            else
            {
                UnblockAllEventSystems();
            }
        }

        public void BlockCurrentEventSystem()
        {
            EventSystem es = EventSystem.current;
            if (es != null)
            {
                es.enabled = false;
                m_DisabledEventSystems.Add(es);
            }
        }

        public void UnblockAllEventSystems()
        {
            foreach (var es in m_DisabledEventSystems)
            {
                if (es != null)
                    es.enabled = true;
            }
            m_DisabledEventSystems.Clear();
        }
    }
}