using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Runtime.Audio;
using Runtime.Manager.Pool;
using Runtime.Message;
using UnityEngine;

namespace Runtime.Animation
{
    public class StatisPrisonSkillAnimator : MonoBehaviour
    {
        [SerializeField] private List<SpriteRenderer> _pillarsSpriteRenderer;
        [SerializeField] private List<ObjectSpriteAnimator> _explosionsSpriteAnimator;
        [SerializeField] private SpriteRenderer _underSpriteRenderer;
        [SerializeField] private GameObject _vfxThunderGameobject;
        [SerializeField] private Color _fadeOutColor;
        [SerializeField] private float _fadeOutTime;
        [SerializeField] private float _height;
        [SerializeField] private string _soundSummonName = "sfx_skill_statis_prison_summon";
        [SerializeField] private string _soundZapName = "sfx_skill_statis_prison_zap";

        private readonly List<SpriteAnimation> _explosionAnimations = new();
        private readonly Color _originColor = new(1f, 1f, 1f);
        private float _duration;

        private const float PILLAR_MOVE_DURATION = 0.1f;

        public void InitVFX(float duration)
        {
            _explosionAnimations.Clear();
            _duration = duration;
            _underSpriteRenderer.gameObject.SetActive(false);
            _vfxThunderGameobject.SetActive(false);

            EnableExplosions(isEnable: false);
            InitExplosionAnimations();
            GeneratePillars().Forget();
        }

        private void InitExplosionAnimations()
        {
            var explosionSpriteAnimator = _explosionsSpriteAnimator;

            foreach (var objectSpriteAnimator in explosionSpriteAnimator)
            {
                _explosionAnimations.Add(objectSpriteAnimator.animations[0]);
            }
        }

        private async UniTaskVoid GeneratePillars()
        {
            FadePillars(isFadeIn: true, _originColor);
            SetPillarsPosition();
            PlaySoundSummon();
            var pillarsSpriteRenderer = _pillarsSpriteRenderer;
            for (int i = 0; i < pillarsSpriteRenderer.Count; i++)
            {
                var spriteRenderer = pillarsSpriteRenderer[i];
                var pillarTransform = spriteRenderer.transform;
                var currentPos = pillarTransform.position;
                var targetPos = new Vector3(currentPos.x, currentPos.y - _height, currentPos.z);
                var index = i;

                await UniTask.Delay(
                      TimeSpan.FromSeconds(0.06f)
                    , ignoreTimeScale: true
                    , cancellationToken: this.GetCancellationTokenOnDestroy()
                );

                if (index == pillarsSpriteRenderer.Count - 1)
                {
                    pillarTransform
                        .DOMove(targetPos, PILLAR_MOVE_DURATION)
                        .OnComplete(() => {
                            PlayExplosionEffect(index).Forget();
                            PlayVFX(); })
                        .SetUpdate(true);
                }
                else
                {
                    var i1 = i;
                    pillarTransform
                        .DOMove(targetPos, PILLAR_MOVE_DURATION)
                        .OnComplete(() => { PlayExplosionEffect(index).Forget(); })
                        .SetUpdate(true);
                }
            }
        }

        private void PlayVFX()
        {
            Messenger.Publish(new StartTriggerDamageByStatisPrisonSkillMessage());
            PlayThunderEffect().Forget();
            FadeUnderSpriteRenderer(true);
        }

        private async UniTaskVoid PlayExplosionEffect(int index)
        {
            _explosionsSpriteAnimator[index].gameObject.SetActive(true);
            var explosionSpriteAnimator = _explosionsSpriteAnimator[index];
            explosionSpriteAnimator.Play(_explosionAnimations[index], playOneShot: true);
            await UniTask.WaitUntil(() => explosionSpriteAnimator.IsPlaying == false);
            explosionSpriteAnimator.gameObject.SetActive(false);
        }

        private void SetPillarsPosition()
        {
            var pillarsSpriteRenderer = _pillarsSpriteRenderer;

            foreach (var spriteRenderer in pillarsSpriteRenderer)
            {
                var pillarTransform = spriteRenderer.transform;
                var endPos = pillarTransform.position;
                var startPos = new Vector3(endPos.x, endPos.y + _height, endPos.z);
                pillarTransform.position = startPos;
            }
        }

        private void EnableExplosions(bool isEnable)
        {
            foreach (var objectSpriteAnimator in _explosionsSpriteAnimator)
            {
                objectSpriteAnimator.gameObject.SetActive(isEnable);
            }
        }

        private async UniTaskVoid PlayThunderEffect()
        {
            _vfxThunderGameobject.SetActive(true);
            PlaySoundZap();
            await UniTask.Delay(
                  TimeSpan.FromSeconds(_duration)
                , cancellationToken: this.GetCancellationTokenOnDestroy()
                , ignoreTimeScale: true
            );

            RemoveVFX().Forget();
        }

        private async UniTaskVoid RemoveVFX()
        {
            _vfxThunderGameobject.SetActive(false);

            FadePillars(isFadeIn: false, _fadeOutColor);
            FadeUnderSpriteRenderer(isFadeIn: false);

            await UniTask.Delay(
                  TimeSpan.FromSeconds(_fadeOutTime)
                , cancellationToken: this.GetCancellationTokenOnDestroy()
                , ignoreTimeScale: true
            );

            PoolManager.Instance.Remove(gameObject);
        }

        private void FadePillars(bool isFadeIn, Color color)
        {
            foreach (var spriteRenderer in _pillarsSpriteRenderer)
            {
                if (isFadeIn)
                {
                    spriteRenderer.color = color;
                    continue;
                }

                spriteRenderer.DOColor(color, _fadeOutTime).SetUpdate(true);
                spriteRenderer.DOFade(0, _fadeOutTime).SetUpdate(true);
            }
        }

        private void FadeUnderSpriteRenderer(bool isFadeIn)
        {
            _underSpriteRenderer.gameObject.SetActive(true);
            _underSpriteRenderer.DOFade(isFadeIn ? 1 : 0, _fadeOutTime).SetUpdate(true);
        }

        private void PlaySoundSummon()
        {
            if (!string.IsNullOrEmpty(_soundSummonName))
                AudioController.Instance.PlaySoundEffectAsync(_soundSummonName, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private void PlaySoundZap()
        {
            if (!string.IsNullOrEmpty(_soundZapName))
                AudioController.Instance.PlaySoundEffectAsync(_soundZapName, this.GetCancellationTokenOnDestroy()).Forget();
        }
    }
}