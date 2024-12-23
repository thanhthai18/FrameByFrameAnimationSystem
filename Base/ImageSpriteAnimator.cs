using UnityEngine;
using Runtime.Definition;
using UnityEngine.UI;
using System;
using DG.Tweening;

namespace Runtime.Animation
{
    /// <summary>
    /// This controls the animation that uses Image.
    /// </summary>
    public class ImageSpriteAnimator : SpriteAnimator
    {
        #region Members

        [SerializeField] private Image _image;

        #endregion Members

        #region Methods

        public override void TintColor(Color color)
            => _image.material.SetColor(Constant.HIT_MATERIAL_COLOR_PROPERTY, color);
        
        public override void FadeOut(float duration, Action onCompleted)
        {
            _image.DOKill();
            if (duration > 0.0f)
            {
                _image.DOFade(0, duration).OnComplete(() => onCompleted?.Invoke());
            }
            else
            {
                var color = _image.color;
                color.a = 0;
                _image.color = color;
                onCompleted?.Invoke();
            }
        }
        
        public override void FadeIn(float duration, Action onCompleted = null)
        {
            _image.DOKill();
            if (duration > 0.0f)
            {
                _image.DOFade(1, duration).OnComplete(() => onCompleted?.Invoke());
            }
            else
            {
                var color = _image.color;
                color.a = 1;
                _image.color = color;
                onCompleted?.Invoke();
            }
        }

        public override void ChangeFrame(int frameIndex)
        {
            var sprite = currentAnimation.GetFrame(frameIndex);
            if (sprite != null)
            {
                _image.sprite = sprite;
                _image.rectTransform.sizeDelta = 100 * sprite.rect.size / sprite.pixelsPerUnit;
            }
        }

        public override void ClearRenderer()
            => _image.sprite = null;


        public override void Play(string animation, float animateSpeedMultiplier = 1.0f, bool playOneShot = false,
            Action eventTriggeredCallbackAction = null, int eventTriggeredFrame = -1, bool playBackwards = false,
            LoopType loopType = LoopType.Repeat, Action eventStoppedCallbackAction = null)
        {
            SetPreviousAnimationName();
            currentAnimation = GetAnimation(animation);
            base.Play(animation, animateSpeedMultiplier, playOneShot, eventTriggeredCallbackAction, eventTriggeredFrame,
                playBackwards, loopType);
        }

        #endregion Methods
    }
}