using System.Collections.Generic;
using UnityEngine;

namespace GraphicsCat
{
    public class IMGUIDock : MonoBehaviour
    {
        enum Position
        {
            TopLeft,
            TopRight,
            TopCenter,

            UpperLeft,
            UpperRight,

            CenterLeft,
            CenterRight,

            LowerCenter,
        }

        static Dictionary<Position, IMGUIDock> s_docks = new();

        public static IMGUIDock topLeft { get { return Get(Position.TopLeft); } }
        public static IMGUIDock topRight { get { return Get(Position.TopRight); } }
        public static IMGUIDock topCenter { get { return Get(Position.TopCenter); } }

        public static IMGUIDock upperLeft { get { return Get(Position.UpperLeft); } }
        public static IMGUIDock upperRight { get { return Get(Position.UpperRight); } }

        public static IMGUIDock centerLeft { get { return Get(Position.CenterLeft); } }
        public static IMGUIDock centerRight { get { return Get(Position.CenterRight); } }

        public static IMGUIDock lowerCenter { get { return Get(Position.LowerCenter); } }

        static float margin { get { return 3 / IMGUIUtils.scaleWithScreenSize; } }

        static string expandPrefKey
        {
            get
            {
                return nameof(IMGUIDock) + "-" + nameof(IMGUIDock.m_Position) + "-" + "Expand";
            }
        }

        static IMGUIDock Get(Position position)
        {
            IMGUIDock dock;
            s_docks.TryGetValue(position, out dock);
            if (dock == null)
            {
                dock = CreateDock(position);
                s_docks[position] = dock;
            }
            return dock;
        }

        static IMGUIDock CreateDock(Position position)
        {
            var dockName = nameof(IMGUIDock) + "-" + position;
            var dockGO = new GameObject(dockName);

            var dockComp = dockGO.AddComponent<IMGUIDock>();
            dockComp.m_Position = position;

            var root = GraphicsCatUtils.GetGraphicsCatRoot();
            dockComp.transform.SetParent(root.transform);

            return dockComp;
        }

        bool m_Expand = true;
        Position m_Position = Position.TopLeft;
        List<IMGUIDockable> m_DockedGUIs = new();
        IMGUIEventBlocker m_EventBlocker = new();

        public void DockGUI(IMGUIDockable gui)
        {
            if (m_DockedGUIs.IndexOf(gui) == -1)
                m_DockedGUIs.Add(gui);
        }

        public void UndockGUI(IMGUIDockable gui)
        {
            if (m_DockedGUIs.IndexOf(gui) != -1)
                m_DockedGUIs.Remove(gui);
        }

        void Awake()
        {
            GameObject.DontDestroyOnLoad(gameObject);
            m_Expand = PrefUtils.Get(expandPrefKey, true);
        }

        void OnDisable()
        {
            m_EventBlocker.UnblockAllEventSystems();
        }

        void OnGUI()
        {
            if (HaveToDrawSomethings() == false)
                return;

            IMGUIUtils.BeginGUI();

            switch (m_Position)
            {
                case Position.TopLeft: DrawTopLeft(); break;
                case Position.TopCenter: DrawTopCenter(); break;
                case Position.TopRight: DrawTopRight(); break;

                case Position.UpperLeft: DrawUpperLeft(); break;
                case Position.UpperRight: DrawUpperRight(); break;

                case Position.CenterLeft: DrawCenterLeft(); break;
                case Position.CenterRight: DrawCenterRight(); break;

                case Position.LowerCenter: DrawLowerCenter(); break;
            }

            IMGUIUtils.EndGUI();
        }

        void DrawTopLeft()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width / IMGUIUtils.scaleWithScreenSize));
            {
                GUILayout.Space(margin);
                DrawContent();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        void DrawTopCenter()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width / IMGUIUtils.scaleWithScreenSize));
            {
                GUILayout.FlexibleSpace();
                DrawContent();
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }


        void DrawTopRight()
        {
            GUILayout.BeginHorizontal(GUILayout.Width(Screen.width / IMGUIUtils.scaleWithScreenSize));
            {
                GUILayout.FlexibleSpace();
                DrawContent();
                GUILayout.Space(margin);
            }
            GUILayout.EndHorizontal();
        }

        void DrawUpperLeft()
        {
            var widthOption = GUILayout.Width(Screen.width / IMGUIUtils.scaleWithScreenSize);
            var heightOption = GUILayout.Height(Screen.height / IMGUIUtils.scaleWithScreenSize);

            GUILayout.BeginHorizontal(widthOption, heightOption);
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.Space(Screen.height / IMGUIUtils.scaleWithScreenSize * 0.25f);
                    DrawContent();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        void DrawUpperRight()
        {
            var widthOption = GUILayout.Width(Screen.width / IMGUIUtils.scaleWithScreenSize);
            var heightOption = GUILayout.Height(Screen.height / IMGUIUtils.scaleWithScreenSize);

            GUILayout.BeginHorizontal(widthOption, heightOption);
            {
                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(Screen.height / IMGUIUtils.scaleWithScreenSize * 0.25f);
                    DrawContent();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();

                GUILayout.Space(margin);
            }
            GUILayout.EndHorizontal();
        }

        void DrawCenterLeft()
        {
            var widthOption = GUILayout.Width(Screen.width / IMGUIUtils.scaleWithScreenSize);
            var heightOption = GUILayout.Height(Screen.height / IMGUIUtils.scaleWithScreenSize);

            GUILayout.BeginHorizontal(widthOption, heightOption);
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.FlexibleSpace();
                    DrawContent();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        void DrawCenterRight()
        {
            var widthOption = GUILayout.Width(Screen.width / IMGUIUtils.scaleWithScreenSize);
            var heightOption = GUILayout.Height(Screen.height / IMGUIUtils.scaleWithScreenSize);

            GUILayout.BeginHorizontal(widthOption, heightOption);
            {
                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical(heightOption);
                {
                    GUILayout.FlexibleSpace();
                    DrawContent();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        void DrawLowerCenter()
        {
            var widthOption = GUILayout.Width(Screen.width / IMGUIUtils.scaleWithScreenSize);
            var heightOption = GUILayout.Height(Screen.height / IMGUIUtils.scaleWithScreenSize);

            GUILayout.BeginHorizontal(widthOption, heightOption);
            {
                GUILayout.FlexibleSpace();

                GUILayout.BeginVertical();
                {
                    GUILayout.Space(Screen.height / IMGUIUtils.scaleWithScreenSize * 0.75f);
                    DrawContent();
                    GUILayout.FlexibleSpace();
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }

        void DrawContent()
        {
            using (var verticalScopeInner = new GUILayout.VerticalScope("box"))
            {
                if (GUILayout.Button(m_Expand ? " - " : " + "))
                {
                    m_Expand = !m_Expand;
                    PrefUtils.Set(expandPrefKey, m_Expand);
                }

                if (m_Expand == false)
                    return;

                bool hasNullOwner = false;
                for (int i = 0, conut = m_DockedGUIs.Count; i < conut; ++i)
                {
                    var dockedGUI = m_DockedGUIs[i];
                    if (dockedGUI == null)
                    {
                        hasNullOwner = true;
                        continue;
                    }

                    var monoBehaviour = dockedGUI as MonoBehaviour;
                    if (monoBehaviour == null)
                    {
                        hasNullOwner = true;
                        continue;
                    }

                    if (monoBehaviour.isActiveAndEnabled == false)
                        continue;

                    dockedGUI.OnDockGUI();
                }

                if (hasNullOwner)
                {
                    for (int i = 0; i < m_DockedGUIs.Count;)
                    {
                        var dockedGUI = m_DockedGUIs[i];
                        if (dockedGUI == null)
                        {
                            m_DockedGUIs.RemoveAt(i);
                            continue;
                        }

                        var monoBehaviour = dockedGUI as MonoBehaviour;
                        if (monoBehaviour == null)
                        {
                            m_DockedGUIs.RemoveAt(i);
                            continue;
                        }

                        ++i;
                    }
                }
            }

            var lastRect = GUILayoutUtility.GetLastRect();
            m_EventBlocker.OnGUI(lastRect);
        }

        bool HaveToDrawSomethings()
        {
            for (int i = 0, end = m_DockedGUIs.Count; i < end; ++i)
            {
                var dockedGUI = m_DockedGUIs[i] as MonoBehaviour;
                if (dockedGUI && dockedGUI.isActiveAndEnabled)
                    return true;
            }
            return false;
        }
    }
}