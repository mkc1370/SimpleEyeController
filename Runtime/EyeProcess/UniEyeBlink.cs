﻿using System;
using UniEyeController.Core.Constants;
using UniEyeController.Core.Status;
using UnityEngine;
using Random = UnityEngine.Random;

namespace UniEyeController.EyeProcess
{
    public class UniEyeBlink : UniEyeProcessBase
    {
        private void Reset()
        {
            executionOrder = 3;
        }

        [Range(0f, 1f)]
        public float weight = 1f;
        
        [Range(0f, 1f)]
        public float eyeMoveMultiplier = 0.8f;
        
        [Range(0f, 10f)]
        public float eyeBlinkStopTimeMin = 3f;
        [Range(0f, 10f)]
        public float eyeBlinkStopTimeMax = 8f;

        /// <summary>
        /// 目を閉じるのにかかる時間
        /// </summary>
        public float timeToCloseEyelid = 0.04f;
        public float timeToOpenEyelid = 0.09f;
        
        /// <summary>
        /// まばたき中に眼球を動かすかどうか
        /// </summary>
        public bool moveEyeWithBlink = true;

        /// <summary>
        /// 目を閉じるときのイベント
        /// EyelidTypeがManualの場合に使用される
        /// </summary>
        public Action<float> OnBlink;

        private EyeBlinkState _eyeBlinkState;
        
        private enum EyeBlinkState
        {
            Idle,
            Closing,
            Opening
        }
        
        private float _eyeTime;
        
        public override void Progress(double time, IEyeStatus statusFromTimeline)
        {
            if (!CanExecute && statusFromTimeline == null) return;
            if (EyeController == null) return;
            if (EyelidController == null) return;
            
            _eyeTime -= Time.deltaTime;
            
            switch (_eyeBlinkState)
            {
                case EyeBlinkState.Idle:
                    if (_eyeTime <= 0)
                    {
                        _eyeBlinkState = EyeBlinkState.Closing;
                        _eyeTime = timeToCloseEyelid;
                    }
                    break;
                case EyeBlinkState.Closing:
                    Blink(1f - _eyeTime / timeToCloseEyelid);
                    if (_eyeTime <= 0)
                    {
                        _eyeBlinkState = EyeBlinkState.Opening;
                        _eyeTime = timeToOpenEyelid;
                        // 完全に閉じる
                        Blink(1);
                    }
                    break;
                case EyeBlinkState.Opening:
                    Blink(_eyeTime / timeToOpenEyelid);
                    if (_eyeTime <= 0)
                    {
                        _eyeBlinkState = EyeBlinkState.Idle;
                        _eyeTime = Random.Range(eyeBlinkStopTimeMin, eyeBlinkStopTimeMax);
                        // 完全に開く
                        Blink(0);
                    }
                    break;
            }
        }

        private void Blink(float value)
        {
            EyelidController.Blink(value * weight, OnBlink);
            if (moveEyeWithBlink)
            {
                EyeController.NormalizedRotate(Vector2.down * value * eyeMoveMultiplier, weight, RotationApplyMethod.Append);
            }
        }
    }
}