using UnityEngine;

namespace Runtime.Animation
{
    public class SingleSpriteAnimationPlayerNotPool : MonoBehaviour
    {
        #region Members

        [SerializeField]
        protected SpriteAnimator spriteAnimator;
        [SerializeField]
        protected SpriteRenderer spriteRenderer;
        [SerializeField]
        protected bool isAnimationLoop;

        #endregion Members

        #region API Methods

        protected virtual void OnEnable()
        {
            spriteRenderer.enabled = true;
            spriteAnimator.AnimationStoppedAction = null;
            if (!isAnimationLoop)
                spriteAnimator.AnimationStoppedAction = OnStopAnimation;
            spriteAnimator.Play(playOneShot: !isAnimationLoop);
        }

        #endregion API Methods

        #region Class Methods

        protected virtual void OnStopAnimation()
            => Hide();

        protected virtual void Hide()
        {
            spriteRenderer.enabled = false;
        }

        #endregion Class Methods
    }
}