using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using Spine;
using Spine.Unity;

namespace Runtime.Animation
{
    public class StopSpineAfterCompleted : MonoBehaviour
    {
        [SerializeField] private SkeletonAnimation skeletonAnimation;
        [SerializeField] private float duration;

        private const float FADE_DURATION = 0.5f;

        private void Start()
        {
            skeletonAnimation.AnimationState.Complete += OnComplete;
        }

        private void OnEnable()
        {
            FadeoutAsync().Forget();
        }

        private async UniTaskVoid FadeoutAsync()
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(duration),
                ignoreTimeScale: true,
                cancellationToken: this.GetCancellationTokenOnDestroy()
            );

            DOVirtual.Float(1f, 0f, FADE_DURATION, (value) =>
            {
                skeletonAnimation.skeleton.A = value;
            })
            .SetUpdate(true)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                skeletonAnimation.skeleton.A = 1f;
            });
        }

        private void OnComplete(TrackEntry trackEntry)
        {
            Pause();
        }

        private void Pause()
        {
            var trackEntry = skeletonAnimation.AnimationState.GetCurrent(0);
            trackEntry.TimeScale = 0f;
        }
    }
}