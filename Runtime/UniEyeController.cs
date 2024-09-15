using System;
using UniEyeController.Constants;
using UniEyeController.Core.Controller.Eye;
using UniEyeController.Core.Controller.Eyelid;
using UniEyeController.Core.Extensions;
using UniEyeController.Core.Process.Blink;
using UniEyeController.Core.Process.LookAt;
using UniEyeController.Core.Process.MicroMove;
using UnityEditor;
using UnityEngine;

namespace UniEyeController
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public partial class UniEyeController : MonoBehaviour
    {
        public EyeAssignMethod assignMethod = EyeAssignMethod.Humanoid;
        
        public Animator animator;

        public GameObject prefabForGenericAvatar;
        
        public Transform manualEyeL;
        public Transform manualEyeR;
        
        public EyeSetting setting = new EyeSetting();
        public EyelidSetting eyelidSetting = new EyelidSetting();

        public Transform CurrentEyeL => _defaultStatus.EyeL.Bone;
        public Transform CurrentEyeR => _defaultStatus.EyeR.Bone;

        private DoubleEyeDefaultStatus _defaultStatus;
        
        public LookAtProcess lookAtProcess = new LookAtProcess();
        public MicroMoveProcess microMoveProcess = new MicroMoveProcess();
        public BlinkProcess blinkProcess = new BlinkProcess();

        public bool IsSettingValid
        {
            get
            {
                if (assignMethod == EyeAssignMethod.Generic && gameObject.scene == prefabForGenericAvatar.scene)
                {
                    Debug.LogError("GenericAvatarの初期状態参照用のPrefabはScene上のものを使用しないでください");
                    return false;
                }

                return true;
            }
        }

        private void OnEnable()
        {
            Init();
        }

        public void Init()
        {
            // TODO : 仮の対応
            if (assignMethod == EyeAssignMethod.Generic && gameObject.scene == prefabForGenericAvatar.scene)
            {
                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
                prefabForGenericAvatar = prefab;
            }
            
            if (!IsSettingValid) enabled = false;
            
            GetRequiredComponents();
            ChangeEyeBones();
        }

        public void ChangeEyeBones()
        {
            GetEyeDefaultStatusBones();
            
            var eyeController = new DoubleEyeController(_defaultStatus, setting);
            var eyelidController = new EyelidController(eyelidSetting);
            
            lookAtProcess.SetControllers(eyeController, eyelidController);
            microMoveProcess.SetControllers(eyeController, eyelidController);
            blinkProcess.SetControllers(eyeController, eyelidController);
        }

        public void AutoSetEyeBonesFromAnimator()
        {
            manualEyeL = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            manualEyeR = animator.GetBoneTransform(HumanBodyBones.RightEye);
        }

        partial void GetEyeDefaultStatusBonesVRM();

        private void GetEyeDefaultStatusBones()
        {
            switch (assignMethod)
            {
                case EyeAssignMethod.Humanoid:
                    _defaultStatus = DoubleEyeDefaultStatus.CreateFromHumanoid(animator);
                    break;
                case EyeAssignMethod.Generic:
                    _defaultStatus = DoubleEyeDefaultStatus.CreateFromGeneric(
                        transform,
                        manualEyeL,
                        manualEyeR,
                        prefabForGenericAvatar
                    );
                    break;
                case EyeAssignMethod.Vrm1:
                    GetEyeDefaultStatusBonesVRM();
                    break;
            }
        }

        private void UpdateInternal(UpdateMethod updateMethod)
        {
            switch (assignMethod)
            {
                case EyeAssignMethod.Humanoid:
                case EyeAssignMethod.Generic:
                    lookAtProcess.Progress(updateMethod);
                    microMoveProcess.Progress(updateMethod);
                    blinkProcess.Progress(updateMethod);
                    break;
                case EyeAssignMethod.Vrm1:
                    blinkProcess.Progress(updateMethod);
                    lookAtProcess.Progress(updateMethod);
                    microMoveProcess.Progress(updateMethod);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Update()
        {
            UpdateInternal(UpdateMethod.Update);
        }

        private void LateUpdate()
        {
            UpdateInternal(UpdateMethod.LateUpdate);
        }

        private void FixedUpdate()
        {
            UpdateInternal(UpdateMethod.FixedUpdate);
        }

        /// <summary>
        /// 手動でのアップデート
        /// Application.isPlayingやexecuteAlwaysの設定に関わらず、UpdateInternalを呼び出す
        /// </summary>
        public void ManualUpdate()
        {
            UpdateInternal(UpdateMethod.Manual);
        }

        private void Reset()
        {
            GetRequiredComponents();
        }

        private void GetRequiredComponents()
        {
            if (animator == null)
            {
                animator = gameObject.GetOrAddComponent<Animator>();
            }
        }
    }
}
