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

    public partial class SelectionGroupEditorWindow : EditorWindow
    {

        [MenuItem("Window/General/Selection Groups")]
        static void OpenWindow()
        {
            var window = EditorWindow.GetWindow<SelectionGroupEditorWindow>();
            window.ShowUtility();
        }

        void DrawGUI()
        {
            var names = SelectionGroupUtility.GetGroupNames();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (hotRect.HasValue)
                EditorGUI.DrawRect(hotRect.Value, Color.white * 0.5f);
            if (GUILayout.Button("Add Group"))
            {
                CreateNewGroup(Selection.objects);
            }
            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                foreach (var n in names)
                {
                    var isActive = activeNames.Contains(n);
                    GUILayout.Space(EditorGUIUtility.singleLineHeight);
                    var rect = GUILayoutUtility.GetRect(1, EditorGUIUtility.singleLineHeight);
                    var dropRect = rect;
                    var showChildren = DrawHeader(rect, n, isActive: isActive);
                    if (showChildren)
                    {
                        var members = SelectionGroupUtility.GetGameObjects(n);
                        rect = GUILayoutUtility.GetRect(1, (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * members.Count);
                        dropRect.yMax = rect.yMax;
                        DrawAllGroupMembers(rect, n, members, allowRemove: true);
                        var queryMembers = SelectionGroupUtility.GetQueryObjects(n);
                        if (queryMembers.Count > 0)
                        {
                            var bg = GUI.backgroundColor;
                            GUI.backgroundColor = Color.yellow;
                            rect = GUILayoutUtility.GetRect(1, (EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight) * queryMembers.Count);
                            dropRect.yMax = rect.yMax;
                            DrawAllGroupMembers(rect, n, queryMembers, allowRemove: false);
                            GUI.backgroundColor = bg;
                        }
                    }

                    if (HandleGroupDragEvents(dropRect, n))
                        Event.current.Use();
                }
                GUILayout.FlexibleSpace();
                GUILayout.Space(16);

                // addNewRect.yMax = GUILayoutUtility.GetLastRect().yMax;
                //This handle creating a new group by dragging onto the add button.
                var addNewRect = GUILayoutUtility.GetLastRect();
                addNewRect.yMin -= 16;
                HandleGroupDragEvents(addNewRect, null);

                GUILayout.Space(EditorGUIUtility.singleLineHeight * 0.5f);
                var bottom = GUILayoutUtility.GetLastRect();
                if (cc.changed)
                {
                }
            }
            EditorGUILayout.EndScrollView();
        }

        void SetupStyles()
        {
            if (miniButtonStyle == null)
            {
                miniButtonStyle = EditorStyles.miniButton;
                miniButtonStyle.padding = new RectOffset(0, 0, 0, 0);
            }
        }

        void DrawAllGroupMembers(Rect rect, string groupName, List<GameObject> members, bool allowRemove)
        {
            rect.height = EditorGUIUtility.singleLineHeight;
            foreach (var i in members)
            {
                DrawGroupMember(rect, groupName, i, allowRemove);
                rect.y += rect.height;// + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        void DrawGroupMember(Rect rect, string groupName, GameObject g, bool allowRemove)
        {
            var content = EditorGUIUtility.ObjectContent(g, typeof(GameObject));
            var isThisMemberSelected = activeSelection.Contains(g);
            var isMouseOver = rect.Contains(Event.current.mousePosition);
            var isManySelected = activeSelection.Count > 1;
            var isAnySelected = activeSelection.Count > 0;
            var isLeftButton = Event.current.button == LEFT_MOUSE_BUTTON;
            var isRightButton = Event.current.button == RIGHT_MOUSE_BUTTON;
            var isMouseDown = Event.current.type == EventType.MouseDown;
            var isMouseUp = Event.current.type == EventType.MouseUp;
            var isNoSelection = activeSelection.Count == 0;
            var isControl = Event.current.control;
            var isLeftMouseDown = isMouseOver && isLeftButton && isMouseDown;
            var isLeftMouseUp = isMouseOver && isLeftButton && isMouseUp;

            if (isThisMemberSelected)
                EditorGUI.DrawRect(rect, SELECTION_COLOR);

            rect.x += 24;
            GUI.contentColor = allowRemove ? Color.white : Color.Lerp(Color.white, Color.yellow, 0.25f);
            GUI.Label(rect, content);
            GUI.contentColor = Color.white;
            if (isLeftMouseDown)
            {
                if (isNoSelection)
                    QueueSelectionOperation(SelectionCommand.Set, g);
                else
                {
                    if (isThisMemberSelected)
                    {
                        if (isControl)
                            QueueSelectionOperation(SelectionCommand.Remove, g);
                    }
                    else
                    {
                        if (isControl)
                            QueueSelectionOperation(SelectionCommand.Add, g);
                        else
                            QueueSelectionOperation(SelectionCommand.Set, g);
                    }
                }
            }
            else if (isRightButton && isMouseOver && isMouseDown && isThisMemberSelected)
            {
                ShowGameObjectContextMenu(rect, groupName, g, allowRemove);
            }


            if (Event.current.type == EventType.MouseDrag && rect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = Selection.objects;
                DragAndDrop.StartDrag(g.name);
            }
        }

        bool DrawHeader(Rect rect, string groupName, bool isActive)
        {
            var group = SelectionGroupUtility.GetFirstGroup(groupName);
            if (group == null) return false;
            var content = EditorGUIUtility.IconContent("LODGroup Icon");
            content.text = $"{groupName}";
            var backgroundColor = groupName == hotGroup ? Color.white * 0.5f : Color.white * 0.4f;
            EditorGUI.DrawRect(rect, isActive ? SELECTION_COLOR : backgroundColor);
            if (HandleMouseEvents(rect, groupName, group))
                Event.current.Use();
            using (var cc = new EditorGUI.ChangeCheckScope())
            {
                rect.width = 16;
                group.showMembers = EditorGUI.Toggle(rect, group.showMembers, "foldout");
                rect.x += 16;
                rect.width = EditorGUIUtility.currentViewWidth - 96;

                if (GUI.Button(rect, content, "label"))
                {
                    hotGroup = groupName;
                    Selection.objects = SelectionGroupUtility.GetMembers(groupName).ToArray();
                }
                rect.x += rect.width;
                rect.width = 16;
                if (group.mutability != MutabilityMode.Disabled)
                {
                    if (GUI.Button(rect, EditorGUIUtility.IconContent("InspectorLock", "Toggle Mutability"), miniButtonStyle))
                    {
                        if (SelectionGroupEditorUtility.AreAnyMembersLocked(groupName))
                        {
                            SelectionGroupEditorUtility.UnlockGroup(groupName);
                        }
                        else
                        {
                            SelectionGroupEditorUtility.LockGroup(groupName);
                        }
                    }
                }
                rect.x += 20;

                if (group.visibility != VisibilityMode.Disabled)
                {

                    if (GUI.Button(rect, EditorGUIUtility.IconContent("d_VisibilityOn", "Toggle Visibility"), miniButtonStyle))
                    {
                        if (SelectionGroupEditorUtility.AreAnyMembersHidden(groupName))
                        {
                            SelectionGroupEditorUtility.ShowGroup(groupName);
                        }
                        else
                        {
                            SelectionGroupEditorUtility.HideGroup(groupName);
                        }
                    }
                }
                rect.x += 20;

                EditorGUI.DrawRect(rect, group.color);

                if (cc.changed)
                {
                    //saves the show members flag.
                    SelectionGroupEditorUtility.UpdateGroup(groupName, group);
                    MarkAllContainersDirty();
                }
            }

            return group.showMembers;
        }

        void ShowGameObjectContextMenu(Rect rect, string groupName, GameObject g, bool allowRemove)
        {
            Selection.activeGameObject = g;
            var menu = new GenericMenu();
            var content = new GUIContent("Remove From Group");
            if (allowRemove)
                menu.AddItem(content, false, () =>
                {
                    SelectionGroupEditorUtility.RecordUndo("Remove group member");
                    SelectionGroupEditorUtility.RemoveObjectsFromGroup(Selection.gameObjects, groupName);
                    MarkAllContainersDirty();
                });
            else
                menu.AddDisabledItem(content);
            menu.DropDown(rect);
        }

        void ShowGroupContextMenu(Rect rect, string groupName, SelectionGroup group)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Duplicate Group"), false, () =>
            {
                SelectionGroupEditorUtility.RecordUndo("Duplicate Group");
                SelectionGroupEditorUtility.DuplicateGroup(groupName);
                MarkAllContainersDirty();
            });
            menu.AddItem(new GUIContent("Clear Group"), false, () =>
            {
                SelectionGroupEditorUtility.RecordUndo("Clear Group");
                SelectionGroupEditorUtility.ClearGroup(groupName);
                MarkAllContainersDirty();
            });
            menu.AddItem(new GUIContent("Configure Group"), false, () => SelectionGroupConfigurationDialog.Open(groupName));
            menu.AddItem(new GUIContent("Delete Group"), false, () =>
            {
                SelectionGroupEditorUtility.RemoveGroup(groupName);
                MarkAllContainersDirty();
            });
            menu.DropDown(rect);
        }

        bool HandleMouseEvents(Rect position, string groupName, SelectionGroup group)
        {
            var e = Event.current;
            if (position.Contains(e.mousePosition))
            {
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (e.button == RIGHT_MOUSE_BUTTON)
                        {
                            ShowGroupContextMenu(position, groupName, group);
                            return true;
                        }
                        break;
                }
            }
            return false;
        }
    }
}
