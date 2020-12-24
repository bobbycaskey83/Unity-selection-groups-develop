﻿using System.Collections.Generic;
using Unity.SelectionGroups.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;


namespace Unity.SelectionGroups
{
    /// <summary>
    /// The main editor window for working with selection groups.
    /// </summary>
    public partial class SelectionGroupEditorWindow : EditorWindow
    {

        const int LEFT_MOUSE_BUTTON = 0;
        const int RIGHT_MOUSE_BUTTON = 1;

        static readonly Color SELECTION_COLOR = new Color32(62, 95, 150, 255);
        static readonly Color HOVER_COLOR = new Color32(112, 112, 112, 128);

        ReorderableList list;
        Vector2 scroll;
        ISelectionGroup activeSelectionGroup;
        float width;
        static SelectionGroupEditorWindow editorWindow;
        Rect? hotRect = null;
        GUIStyle miniButtonStyle;
        HashSet<Object> activeSelection = new HashSet<Object>();
        HashSet<string> activeNames = new HashSet<string>();

        Object hotMember;

        static void CreateNewGroup()
        {
            Undo.RegisterCompleteObjectUndo(SelectionGroupManager.instance, "Create");
            SelectionGroupManager.instance.CreateGroup("New Group");
        }
    }
}
