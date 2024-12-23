using System;
using DG.Tweening;
using UnityEngine;
using Runtime.Definition;

namespace Runtime.Animation
{
    /// <summary>
    /// This controls the animation that uses sprites.
    /// </summary>
    public class ObjectSpriteAnimator : SpriteAnimator
    {
        #region Members

        [SerializeField]
        protected SpriteRenderer spriteRenderer;

        #endregion Members

        #region Class Methods

        public override void TintColor(Color color)
            => spriteRenderer.material.SetColor(Constant.HIT_MATERIAL_COLOR_PROPERTY, color);

        public override void FadeOut(float duration, Action onCompleted = null)
        {
            spriteRenderer.DOKill();
            if (duration > 0.0f)
            {
                spriteRenderer.DOFade(0, duration).OnComplete(() => onCompleted?.Invoke());
            }
            else
            {
                var color = spriteRenderer.color;
                color.a = 0;
                spriteRenderer.color = color;
                onCompleted?.Invoke();
            }
        }
        
        public override void FadeIn(float duration, Action onCompleted = null)
        {
            spriteRenderer.DOKill();
            if (duration > 0.0f)
            {
                spriteRenderer.DOFade(1, duration).OnComplete(() => onCompleted?.Invoke());
            }
            else
            {
                var color = spriteRenderer.color;
                color.a = 1;
                spriteRenderer.color = color;
                onCompleted?.Invoke();
            }
        }

        public override void ChangeFrame(int frameIndex)
        {
            var sprite = currentAnimation.GetFrame(frameIndex);
            if (sprite != null)
                spriteRenderer.sprite = sprite;
        }

        public override void ClearRenderer()
            => spriteRenderer.sprite = null;

        #endregion Class Methods
    }
}