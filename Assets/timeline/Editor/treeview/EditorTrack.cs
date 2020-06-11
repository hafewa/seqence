using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace UnityEditor.Timeline
{
    public struct TrackMenuAction
    {
        public string desc;
        public bool on;
        public GenericMenu.MenuFunction2 fun;
        public object arg;
    }

    public class EditorTrack : ITimelineInspector
    {
        public XTrack track;
        public Rect rect;
        public Rect head;
        public bool select;
        private GenericMenu pm;

        public uint ID
        {
            get { return track.ID; }
        }

        protected virtual Color trackColor
        {
            get { return Color.red; }
        }

        protected virtual List<TrackMenuAction> actions { get; }

        protected bool triger
        {
            get
            {
                var pos = Event.current.mousePosition;
                return head.Contains(pos) || rect.Contains(pos);
            }
        }

        public void OnGUI()
        {
            var backgroundColor = select
                ? TimelineStyles.colorDuration
                : TimelineStyles.markerHeaderDrawerBackgroundColor;

            var headColor = backgroundColor;
            EditorGUI.DrawRect(head, headColor);
            Rect tmp = head;
            tmp.width = 4;
            EditorGUI.DrawRect(tmp, trackColor);

            EditorGUI.DrawRect(rect, backgroundColor);
            tmp = rect;
            tmp.height = 2;
            tmp.y = rect.y + rect.height - 2;
            EditorGUI.DrawRect(tmp, trackColor * 0.9f);

            GUIHeader();
            GUIContent();
            if (!track.locked)
            {
                ProcessEvent();
            }
        }

        protected void ProcessEvent()
        {
            var e = Event.current;
            if (e.type == EventType.ContextClick)
            {
                if (triger)
                {
                    pm = new GenericMenu();
                    if (TimelineWindow.inst.tree.AnySelect())
                    {
                        pm.AddItem(EditorGUIUtility.TrTextContent("UnSelect All  \t #u"), false, UnSelectAll, false);
                        pm.AddDisabledItem(EditorGUIUtility.TrTextContent("Select All \t %#s"));
                    }
                    else
                    {
                        pm.AddItem(EditorGUIUtility.TrTextContent("Select All Tracks \t %#s"), false, UnSelectAll,
                            true);
                        pm.AddDisabledItem(EditorGUIUtility.TrTextContent("UnSelect All Tracks \t %u"));
                    }

                    pm.AddSeparator("");
                    pm.AddItem(EditorGUIUtility.TrTextContent("Add Clip \t #a"), false, AddClip, e.mousePosition);
                    pm.AddItem(EditorGUIUtility.TrTextContent("Delete \t #d"), false, DeleteClip, e.mousePosition);
                    if (track.mute)
                    {
                        pm.AddItem(EditorGUIUtility.TrTextContent("UnMute Track \t "), false, UnmuteClip);
                    }
                    else
                    {
                        pm.AddItem(EditorGUIUtility.TrTextContent("Mute Track \t"), false, MuteClip);
                    }
                    if (@select)
                    {
                        pm.AddItem(EditorGUIUtility.TrTextContent("UnSelect Track \t #s"), false, SelectTrack, false);
                    }
                    else
                    {
                        pm.AddItem(EditorGUIUtility.TrTextContent("Select Track \t #s"), false, SelectTrack, true);
                    }
                    pm.AddSeparator("");
                    if (actions != null)
                    {
                        for (int i = 0; i < actions.Count; i++)
                        {
                            var at = actions[i];
                            pm.AddItem(EditorGUIUtility.TrTextContent(at.desc), at.@on, at.fun, at.arg);
                        }
                    }
                    pm.AddSeparator("");
                    var marks = TypeUtilities.GetBelongMarks(track.trackType);
                    for (int i = 0; i < marks.Count; i++)
                    {
                        var mark = marks[i];
                        string str = mark.ToString();
                        int idx = str.LastIndexOf('.');
                        str = str.Substring(idx + 1);
                        var ct = EditorGUIUtility.TrTextContent("Add " + str);
                        MarkAction action = new MarkAction() {type = mark, posX = e.mousePosition.x};
                        pm.AddItem(ct, false, AddMark, action);
                    }
                    pm.ShowAsContext();
                }
            }
        }

        private void SelectTrack(object arg)
        {
            bool sele = (bool) arg;
            this.@select = sele;

            TimelineInspector.inst?.SetActive(this, sele);
            TimelineWindow.inst.Repaint();
        }

        public void YOffset(float y)
        {
            head.y += y;
            rect.y += y;
        }

        public void SetHeight(float height)
        {
            head.height = height;
            rect.height = height;
        }

        protected void GUIHeader()
        {
            var tmp = head;
            tmp.y += head.height / 4;
            GUILayout.BeginArea(tmp);
            GUILayout.BeginHorizontal();
            GUILayout.Space(5);
            GUILayout.Label(track.ToString());
            OnGUIHeader();
            if (GUILayout.Button("mute", TimelineStyles.mute))
            {
                track.SetFlag(TrackMode.Mute, !track.mute);
            }
            if (GUILayout.Button("lock", TimelineStyles.locked))
            {
                track.SetFlag(TrackMode.Lock, !track.locked);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        protected virtual void OnGUIHeader()
        {
        }

        protected void GUIContent()
        {
            var clips = track.clips;
            if (clips != null)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    new EditorClip(this, clips[i]).OnGUI();
                }
            }
            var marks = track.marks;
            if (marks != null)
            {
                for (int i = 0; i < marks.Length; i++)
                {
                    DrawMarkItem(marks[i]);
                }
            }
            if (track.locked)
            {
                GUI.Box(rect, "", TimelineStyles.lockedBG);
            }
            GUILayout.BeginArea(rect);
            OnGUIContent();
            GUILayout.EndArea();
        }

        void DrawMarkItem(XMarker mark)
        {
            float x = TimelineWindow.inst.TimeToPixel(mark.time);
            Rect tmp = rect;
            tmp.x = x;
            tmp.y = rect.y + rect.height / 4;
            tmp.width = 20;
            GUIContent cont = TimelineWindow.inst.state.config.GetIcon(mark.type);

            GUI.Box(tmp, cont, GUIStyle.none);
        }

        protected virtual void OnGUIContent()
        {
        }

        public void UnSelectAll(object arg)
        {
            bool select = (bool) arg;
            TimelineWindow.inst.tree?.ResetSelect(select);
        }

        private void AddMark(object m)
        {
            MarkAction t = (MarkAction) m;
            float time = TimelineWindow.inst.PiexlToTime(t.posX);
            EditorFactory.MakeMarker(t.type, time, track);
        }

        protected virtual void AddClip(object mpos)
        {
            TimelineWindow.inst.Repaint();
        }

        private void DeleteClip(object mpos)
        {
            Debug.Log("delete Click");
            Vector2 pos = (Vector2) mpos;
            float time = TimelineWindow.inst.PiexlToTime(pos.x);
            bool find = false;
            if (track.clips != null)
            {
                for (int i = 0; i < track.clips.Length; i++)
                {
                    var clip = track.clips[i];
                    if (time >= clip.start && time <= clip.end)
                    {
                        track.RmClip(clip);
                        find = true;
                        TimelineWindow.inst.Repaint();
                        break;
                    }
                }
            }
            if (!find)
            {
                EditorUtility.DisplayDialog("tip", "not select clip", "ok");
            }
        }

        private void MuteClip()
        {
            track.SetFlag(TrackMode.Mute, true);
            TimelineWindow.inst.Repaint();
        }

        private void UnmuteClip()
        {
            track.SetFlag(TrackMode.Mute, false);
            TimelineWindow.inst.Repaint();
        }


        private bool clipF, markF, trackF;

        public void OnInspector()
        {
            trackF = EditorGUILayout.Foldout(trackF, "track: " + track.trackType + " " + track.ID);
            if (trackF)
            {
                int cnt = track.childs?.Length ?? 0;
                EditorGUILayout.LabelField("sub track count:" + cnt);
                if (track.clips != null)
                {
                    clipF = EditorGUILayout.Foldout(clipF, "clips:");
                    if (clipF)
                    {
                        foreach (var clip in track.clips)
                        {
                            EditorGUILayout.LabelField(clip.Display);
                            clip.start = EditorGUILayout.FloatField("start", clip.start);
                            float d = EditorGUILayout.FloatField("duration", clip.duration);
                            if (d > 0)
                            {
                                clip.duration = d;
                            }
                            OnInspectorClip(clip);
                        }
                    }
                }
                if (track.marks != null)
                {
                    markF = EditorGUILayout.Foldout(markF, "marks");
                    if (markF)
                    {
                        foreach (var mark in track.marks)
                        {
                            EditorGUILayout.LabelField(mark.type.ToString());
                            mark.time = EditorGUILayout.FloatField("time", mark.time);
                        }
                    }
                }
                EditorGUILayout.Space();
            }
        }

        protected virtual void OnInspectorClip(IClip clip)
        {
        }
    }
}
