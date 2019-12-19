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
        const int LEFT_MOUSE_BUTTON = 0;
        const int RIGHT_MOUSE_BUTTON = 1;

        static readonly Color SELECTION_COLOR = new Color32(62, 95, 150, 255);

        ReorderableList list;
        Vector2 scroll;
        SelectionGroup activeSelectionGroup;
        float width;
        static SelectionGroupEditorWindow editorWindow;
        Rect? hotRect = null;
        GUIStyle miniButtonStyle;
        HashSet<Object> activeSelection = new HashSet<Object>();
        SelectionOperation nextSelectionOperation;
        HashSet<string> activeNames = new HashSet<string>();
        
        enum SelectionCommand
        {
            Add,
            Remove,
            Set,
            None
        }

        class SelectionOperation
        {
            public SelectionCommand command;
            public Object gameObject;
        }
    }
}
