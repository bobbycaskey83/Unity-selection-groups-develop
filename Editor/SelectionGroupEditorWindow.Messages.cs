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

        void OnEnable()
        {
            titleContent.text = "Selection Groups";
            editorWindow = this;
        }

        void OnHierarchyChange()
        {
            //This is required to preserve refences when a gameobject is moved between scenes in the editor.
            SanitizeSceneReferences();
        }

        void OnDisable()
        {
            SelectionGroupContainer.onLoaded -= OnContainerLoaded;
            editorWindow = null;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {

        }

        void OnSelectionChange()
        {
            activeSelection.Clear();
            activeSelection.UnionWith(Selection.objects);
        }

        void OnGUI()
        {
            SetupStyles();
            DrawGUI();

            //Unlike other drag events, this DragExited should be handled once per frame.
            if (Event.current.type == EventType.DragExited)
            {
                ExitDrag();
                Event.current.Use();
            }

            if (focusedWindow == this)
                Repaint();

            if(Event.current.type == EventType.Repaint)
                EditorApplication.delayCall += PerformSelectionCommands;
        }

        
    }
}
