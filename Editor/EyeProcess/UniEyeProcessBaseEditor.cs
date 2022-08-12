﻿using UniEyeController.EyeProcess;
using UnityEditor;
using UnityEngine;

namespace UniEyeController.Editor.EyeProcess
{
    public abstract class UniEyeProcessBaseEditor
    {
        private SerializedProperty _enabled;
        
        public UniEyeProcessBaseEditor(SerializedProperty property)
        {
            _enabled = property.FindPropertyRelative(nameof(UniEyeProcessBase.enabled));
            
            GetProperties(property);
        }

        public void Draw()
        {
            EditorGUILayout.PropertyField(_enabled, new GUIContent("機能の有効化"));
            
            DrawProperties();
        }

        protected abstract void GetProperties(SerializedProperty property);
        protected abstract void DrawProperties();
    }
}