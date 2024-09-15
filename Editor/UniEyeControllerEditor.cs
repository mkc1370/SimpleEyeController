using System;
using System.Collections.Generic;
using UniEyeController.Constants;
using UniEyeController.Editor.Core.Controller.Eye;
using UniEyeController.Editor.Core.Controller.Eyelid;
using UniEyeController.Editor.Core.Extensions;
using UniEyeController.Editor.Core.Process.Blink;
using UniEyeController.Editor.Core.Process.LookAt;
using UniEyeController.Editor.Core.Process.MicroMove;
using UniEyeController.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace UniEyeController.Editor
{
    using  UniEyeController = UniEyeController;
    
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UniEyeController))]
    public partial class UniEyeControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty _assignMethod;
        private SerializedProperty _animator;
        private SerializedProperty _prefabForGenericAvatar;
        private SerializedProperty _manualEyeL;
        private SerializedProperty _manualEyeR;

        private EyeSettingEditor _eyeSettingEditor;
        private EyelidSettingEditor _eyelidSettingEditor;
        
        private bool _debugFoldout;

        private LookAtProcessEditor _lookAtProcessEditor;
        private MicroMoveProcessEditor _microMoveProcessEditor;
        private BlinkProcessEditor _blinkProcessEditor;
        
        private bool _isFoldout = true;

        partial void OnEnableVRM();
        
        private void OnEnable()
        {
            OnEnableVRM();
            
            _assignMethod = serializedObject.FindProperty(nameof(UniEyeController.assignMethod));
            _animator = serializedObject.FindProperty(nameof(UniEyeController.animator));
            _prefabForGenericAvatar = serializedObject.FindProperty(nameof(UniEyeController.prefabForGenericAvatar));
            _manualEyeL = serializedObject.FindProperty(nameof(UniEyeController.manualEyeL));
            _manualEyeR = serializedObject.FindProperty(nameof(UniEyeController.manualEyeR));
            
            var eyeSetting = serializedObject.FindProperty(nameof(UniEyeController.setting));
            _eyeSettingEditor = new EyeSettingEditor(eyeSetting);
            
            var eyelidSetting = serializedObject.FindProperty(nameof(UniEyeController.eyelidSetting));
            _eyelidSettingEditor = new EyelidSettingEditor(eyelidSetting);
            
            var lookAt = serializedObject.FindProperty(nameof(UniEyeController.lookAtProcess));
            var microMove = serializedObject.FindProperty(nameof(UniEyeController.microMoveProcess));
            var blink = serializedObject.FindProperty(nameof(UniEyeController.blinkProcess));
            
            _lookAtProcessEditor = new LookAtProcessEditor(lookAt);
            _blinkProcessEditor = new BlinkProcessEditor(blink);
            _microMoveProcessEditor = new MicroMoveProcessEditor(microMove);
            
            var script = target as UniEyeController;
            if (script == null) return;
        }

        private void OnDisable()
        {
            var script = target as UniEyeController;
            if (script == null) return;
        }

        partial void DrawVrm();

        private List<string> _errorMessages = new List<string>();
        
        public override void OnInspectorGUI()
        {
            var script = target as UniEyeController;
            if (script == null) return;
            
            serializedObject.Update();

            CustomUI.Foldout("基本設定", ref _isFoldout);
            
            if (_isFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUI.BeginChangeCheck();

                EditorGUILayout.LabelField("目のボーンの設定", EditorStyles.boldLabel);
                EditorGUI.BeginDisabledGroup(Application.isPlaying);
                GUILayout.BeginVertical(GUI.skin.box);
                {
                    _errorMessages.Clear();

                    EditorGUILayout.PropertyField(_assignMethod, new GUIContent("キャラクターの種類"));
                    EditorGUILayout.Space();

                    switch ((EyeAssignMethod)_assignMethod.enumValueIndex)
                    {
                        case EyeAssignMethod.Humanoid:
                            var animator = (Animator)_animator.objectReferenceValue;
                            EditorExtensions.BeginErrorColor(animator == null || !animator.isHuman);
                            EditorGUILayout.PropertyField(_animator);
                            EditorExtensions.EndErrorColor();
                            if (animator == null)
                            {
                                _errorMessages.Add("Animatorが設定されていません");
                            }

                            if (animator != null)
                            {
                                if (animator.isHuman)
                                {
                                    // AnimatorがHumanoidのときに、Genericに変わったとき用の目のボーンをキャッシュしておく
                                    if (
                                        _manualEyeL.objectReferenceValue == null ||
                                        _manualEyeR.objectReferenceValue == null
                                    )
                                    {
                                        script.AutoSetEyeBonesFromAnimator();
                                    }
                                    
                                    EditorGUILayout.HelpBox("全てのmuscleが0の状態を基準として目の初期状態を設定します。", MessageType.Info);
                                }
                                else
                                {
                                    _errorMessages.Add("AnimatorがHumanoidではありません");
                                    if (GUILayout.Button("Genericに切り替える"))
                                    {
                                        _assignMethod.enumValueIndex = (int)EyeAssignMethod.Generic;
                                    }
                                }
                            }

                            break;
                        case EyeAssignMethod.Generic:
                            EditorExtensions.BeginErrorColor(_prefabForGenericAvatar.objectReferenceValue == null);
                            EditorGUI.BeginChangeCheck();
                            // Scene上のGameObjectを指定させないためにObjectFieldを使う
                            script.prefabForGenericAvatar = EditorGUILayout.ObjectField("基準となるPrefab", script.prefabForGenericAvatar, typeof(GameObject), false) as GameObject;
                            if (EditorGUI.EndChangeCheck())
                            {
                                EditorUtility.SetDirty(script);
                            }
                            EditorExtensions.EndErrorColor();
                            
                            GUILayout.BeginHorizontal();
                            GUILayout.Space(EditorGUIUtility.singleLineHeight * EditorGUI.indentLevel);

                            if (_prefabForGenericAvatar.objectReferenceValue == null)
                            {
                                _errorMessages.Add("基準となるPrefabが指定されていません");

                                if (GUILayout.Button("自動で検索する"))
                                {
                                    var prefab = PrefabUtility.GetCorrespondingObjectFromSource(script.gameObject);
                                    _prefabForGenericAvatar.objectReferenceValue = prefab;
                                }
                            }

                            GUILayout.EndHorizontal();

                            EditorGUILayout.Space();

                            EditorExtensions.BeginErrorColor(_manualEyeL.objectReferenceValue == null);
                            EditorGUILayout.PropertyField(_manualEyeL, new GUIContent("左目"));
                            EditorExtensions.EndErrorColor();
                            if (_manualEyeL.objectReferenceValue == null)
                            {
                                _errorMessages.Add("左目のTransformが設定されていません");
                            }

                            EditorExtensions.BeginErrorColor(_manualEyeR.objectReferenceValue == null);
                            EditorGUILayout.PropertyField(_manualEyeR, new GUIContent("右目"));
                            EditorExtensions.EndErrorColor();
                            if (_manualEyeR.objectReferenceValue == null)
                            {
                                _errorMessages.Add("右目のTransformが設定されていません");
                            }

                            var message =
                                $"基準となるPrefabを基準として目の初期状態を決定します。{Environment.NewLine}" +
                                $"目までのパス（GameObject名）が異なっていると正しく動作しません。";
                            EditorGUILayout.HelpBox(message, MessageType.Info);

                            break;
                        
                        case EyeAssignMethod.Vrm1:
                            DrawVrm();
                            break;
                    }

                    EditorGUILayout.Space();

                    EditorExtensions.DrawErrorMessages(_errorMessages);

                    if (EditorGUI.EndChangeCheck())
                    {
                        // ここで更新しないと変更された内容が反映されない
                        serializedObject.ApplyModifiedProperties();

                        if (_errorMessages.Count == 0)
                        {
                            script.ChangeEyeBones();
                        }
                    }
                }

                GUILayout.EndVertical();
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.Space();

                serializedObject.Update();

                // 目の設定
                _eyeSettingEditor.Draw();
                EditorGUILayout.Space();

                // まぶたの設定
                _eyelidSettingEditor.Draw();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            _lookAtProcessEditor.Draw();
            EditorGUILayout.Space();
            _microMoveProcessEditor.Draw();
            EditorGUILayout.Space();
            _blinkProcessEditor.Draw();
            EditorGUILayout.Space();

            _debugFoldout = EditorGUILayout.Foldout(_debugFoldout, "デバッグ用");
            if (_debugFoldout)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;
                {
                    EditorGUILayout.ObjectField("左目", script.CurrentEyeL, typeof(Transform), true);
                    EditorGUILayout.ObjectField("右目", script.CurrentEyeR, typeof(Transform), true);
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}