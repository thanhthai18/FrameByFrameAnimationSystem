using UnityEngine;
using Runtime.Manager.Pool;

namespace Runtime.Animation
{
    /// <summary>
    /// This component plays a single animation right at the time the game object appears in the scene.<br/>
    /// The animation can be either loop or not loop depending on the input.
    /// </summary>
    public class SingleSpriteAnimationPlayer : MonoBehaviour
    {
        #region Members

        [SerializeField]
        protected SpriteAnimator spriteAnimator;
        [SerializeField]
        protected bool isAnimationLoop;

        #endregion Members

        #region API Methods

        protected virtual void OnEnable()
        {
            spriteAnimator.Pause();
            spriteAnimator.AnimationStoppedAction = null;
            if (!isAnimationLoop)
                spriteAnimator.AnimationStoppedAction = OnStopAnimation;
            spriteAnimator.Play(playOneShot: !isAnimationLoop);
        }

        #endregion API Methods

        #region Class Methods

        protected virtual void OnStopAnimation()
            => DestroySelf();

        protected virtual void DestroySelf()
        {
            transform.SetParent(null);
            PoolManager.Instance.Remove(gameObject);
        }

        #endregion Class Methods
    }
}