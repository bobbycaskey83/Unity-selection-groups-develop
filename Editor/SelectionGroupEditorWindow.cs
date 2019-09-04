﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.SelectionGroups
{
    public class SelectionGroupEditorWindow : EditorWindow
    {

        ReorderableList list;
        SerializedObject serializedObject;
        SelectionGroupContainer selectionGroups;
        Vector2 scroll;
        SerializedProperty activeSelectionGroup;
        float width;
        static SelectionGroupEditorWindow editorWindow;

        static SelectionGroupEditorWindow()
        {
            SelectionGroupContainer.onLoaded -= OnContainerLoaded;
            SelectionGroupContainer.onLoaded += OnContainerLoaded;
        }

        void OnEnable()
        {
            titleContent.text = "Selection Groups";
            editorWindow = this;
        }

        void OnHierarchyChange()
        {
            foreach (var i in SelectionGroupContainer.instances.ToArray())
            {
                var scene = i.Key;
                var container = i.Value;
                foreach (var g in container.groups)
                {
                    var name = g.Key;
                    var group = g.Value;
                    foreach (var o in group.objects.ToArray())
                    {
                        if (o != null && o.scene != scene)
                        {
                            group.objects.Remove(o);
                            SelectionGroupUtility.AddObjectToGroup(o, name);
                        }
                    }
                }
            }
        }

        static void OnContainerLoaded(SelectionGroupContainer container)
        {
            foreach (var name in container.groups.Keys.ToArray())
            {
                var mainGroup = SelectionGroupUtility.GetFirstGroup(name);
                var importedGroup = container.groups[name];
                importedGroup.color = mainGroup.color;
                importedGroup.selectionQuery = mainGroup.selectionQuery;
                importedGroup.showMembers = mainGroup.showMembers;
                container.groups[name] = importedGroup;
            }
            foreach (var i in SelectionGroupContainer.instances.Values)
            {
                foreach (var g in i.groups.Values)
                {
                    g.ClearQueryResults();
                }
                EditorUtility.SetDirty(i);
            }
            if (editorWindow != null) editorWindow.Repaint();
        }

        void OnDisable()
        {
            SelectionGroupContainer.onLoaded -= OnContainerLoaded;
            editorWindow = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

        }

        void OnSelectionChange()
        {
        }

        [MenuItem("Window/General/Selection Groups")]
        static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<SelectionGroupEditorWindow>();
            window.ShowUtility();
        }

        void OnGUI()
        {
            var names = SelectionGroupUtility.GetGroupNames();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                foreach (var n in names)
                {
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
                    var rect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                    var dropRect = rect;
                    var showChildren = DrawGroupWidget(rect, n);
                    if (showChildren)
                    {
                        var members = SelectionGroupUtility.GetGameObjects(n);
                        rect = GUILayoutUtility.GetRect(1, (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * members.Count);
                        dropRect.yMax = rect.yMax;
                        DrawGroupMembers(rect, n, members, allowRemove: true);
                        var queryMembers = SelectionGroupUtility.GetQueryObjects(n);
                        if (queryMembers.Count > 0)
                        {
                            var bg = GUI.backgroundColor;
                            GUI.backgroundColor = Color.yellow;
                            rect = GUILayoutUtility.GetRect(1, (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * queryMembers.Count);
                            dropRect.yMax = rect.yMax;
                            DrawGroupMembers(rect, n, queryMembers, allowRemove: false);
                            GUI.backgroundColor = bg;
                        }
                    }
                    if (HandleDragEvents(dropRect, n))
                        Event.current.Use();
                }
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add Group"))
                {
                    var actualName = SelectionGroupUtility.CreateNewGroup("New Group");
                    SelectionGroupUtility.AddObjectToGroup(Selection.gameObjects, actualName);
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);
                var bottom = GUILayoutUtility.GetLastRect();
                if (cc.changed)
                {
                }
            }
            EditorGUILayout.EndScrollView();

            if (focusedWindow == this)
                Repaint();
        }

        void DrawGroupMembers(Rect rect, string groupName, List<GameObject> members, bool allowRemove)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            foreach (var i in members)
            {
                DrawGroupMemberWidget(rect, groupName, i, allowRemove);
                rect.y += rect.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private void DrawGroupMemberWidget(Rect rect, string groupName, GameObject g, bool allowRemove)
        {
            // GUIContent content;
            var content = EditorGUIUtility.ObjectContent(g, typeof(GameObject));
            // content.text = g.name;
            if (Selection.activeGameObject == g)
            {
                GUI.Box(rect, string.Empty);
            }
            rect.x += 24;
            if (GUI.Button(rect, content, "label"))
            {
                Selection.activeGameObject = g;
                if (Event.current.button == 1)
                {
                    ShowGameObjectContextMenu(rect, groupName, g, allowRemove);
                }
            }
        }

        bool DrawGroupWidget(Rect rect, string groupName)
        {
            var group = SelectionGroupUtility.GetFirstGroup(groupName);
            var content = EditorGUIUtility.IconContent("LODGroup Icon");
            content.text = groupName;
            GUI.Box(rect, string.Empty);
            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                group.showMembers = EditorGUI.Foldout(rect, group.showMembers, content);
                var colorRect = rect;
                colorRect.x = colorRect.width - colorRect.height - 4;
                colorRect.width = colorRect.height;
                EditorGUI.DrawRect(colorRect, group.color);
                if (cc.changed)
                {
                    SelectionGroupUtility.UpdateGroup(groupName, group);
                }
            }
            if (HandleMouseEvents(rect, groupName))
                Event.current.Use();
            return group.showMembers;
        }

        void ShowGameObjectContextMenu(Rect rect, string groupName, GameObject g, bool allowRemove)
        {
            Selection.activeGameObject = g;
            var menu = new GenericMenu();
            var content = new GUIContent("Remove From Group");
            if (allowRemove)
                menu.AddItem(content, false, () => SelectionGroupUtility.RemoveObjectFromGroup(g, groupName));
            else
                menu.AddDisabledItem(content);
            menu.DropDown(rect);
        }

        void ShowGroupContextMenu(Rect rect, string groupName)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Remove Group"), false, () => SelectionGroupUtility.RemoveGroup(groupName));
            menu.AddItem(new GUIContent("Duplicate Group"), false, () => SelectionGroupUtility.DuplicateGroup(groupName));
            menu.AddItem(new GUIContent("Configure Group"), false, () => SelectionGroupDialog.Open(groupName));
            menu.DropDown(rect);
        }

        bool HandleMouseEvents(Rect position, string groupName)
        {
            var e = Event.current;
            if (position.Contains(e.mousePosition))
            {
                switch (e.type)
                {
                    case EventType.MouseDown:
                        ShowGroupContextMenu(position, groupName);
                        return true;
                }
            }
            return false;
        }

        bool HandleDragEvents(Rect position, string groupName)
        {
            var e = Event.current;
            if (position.Contains(e.mousePosition))
            {
                switch (e.type)
                {
                    case EventType.DragUpdated:
                        UpdateDrag(position);
                        return true;
                    case EventType.DragExited:
                        return true;
                    case EventType.DragPerform:
                        PerformDrag(position, groupName);
                        return true;
                }
            }
            return false;
        }

        void UpdateDrag(Rect rect)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.AcceptDrag();
        }

        void PerformDrag(Rect position, string groupName)
        {
            foreach (var i in DragAndDrop.objectReferences)
            {
                var g = i as GameObject;
                if (g != null) SelectionGroupUtility.AddObjectToGroup(g, groupName);
            }
        }
    }
}
