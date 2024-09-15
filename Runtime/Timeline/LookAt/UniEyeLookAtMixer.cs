using UniEyeController.Constants;
using UniEyeController.Core.Process.LookAt;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UniEyeController.Timeline.LookAt
{
    public class UniEyeLookAtMixer : PlayableBehaviour
    {
        public UniEyeLookAtTrack Track { get; set; }
        public TimelineClip[] Clips { get; set; }

        private LookAtProcess _process;

        /// <summary>
        /// 目の状態を元に戻す
        /// GatherPropertiesの仕様上、メンテナンスが難しくなってしまうため
        /// 自前でキャッシュを用意しています
        /// </summary>
        private LookAtStatus? _statusCache;
        
        public override void OnGraphStop(Playable playable)
        {
            // 再生する前の状態を復元する
            if (_statusCache.HasValue)
            {
                // 目を正面を向いた状態に戻す
                _process.status = LookAtStatus.LookForward;
                _process.Progress(UpdateMethod.Timeline);
                
                // キャッシュから復元する
                _process.status = _statusCache.Value;
            }
            
            _statusCache = null;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var controller = playerData as UniEyeController;
            if (controller == null) return;
            if (!controller.gameObject.activeInHierarchy) return;
            if (!controller.IsSettingValid) return;

            _process = controller.lookAtProcess;
            if (_process == null) return;
            
            // 再生する前の状態をキャッシュする
            if (_statusCache == null)
            {
                _statusCache = _process.status;
            }

            _process.status = _statusCache.Value;

            var nonZeroClipCount = 0;
            for (var i = 0; i < Clips.Length; i++)
            {
                var clip = Clips[i];
                var asset = clip.asset as UniEyeLookAtClip;
                if (asset == null) continue;

                var weight = playable.GetInputWeight(i);
                asset.status.targetTransform =
                    asset.status.targetTransformTimeline.Resolve(playable.GetGraph().GetResolver());

                if (weight > 0)
                {
                    var status = asset.status;
                    status.weight *= weight;

                    if (nonZeroClipCount == 0)
                    {
                        _process.status = status;
                    }
                    else if (nonZeroClipCount == 1)
                    {
                        _process.status2 = status;
                        _process.useStatus2 = true;
                    }

                    nonZeroClipCount++;
                }
            }

            _process.Progress(UpdateMethod.Timeline);
            _process.useStatus2 = false;
        }
    }
}