using System;

namespace Runtime.Animation
{
    /// <summary>
    /// This component plays a single animation right at the time the game object appears in the scene.<br/>
    /// But this one has a full animation set including: Start phase, middle phase, and end phase.<br/>
    /// The middle phase can be either loop or not loop depending on the input.
    /// </summary>
    public class SingleFullSpriteAnimationPlayer : SingleSpriteAnimationPlayer
    {
        #region API Methods

        protected override void OnEnable()
        {
            spriteAnimator.Pause();
            spriteAnimator.AnimationStoppedAction = null;
            spriteAnimator.AnimationStoppedAction = OnStopAnimation;
            spriteAnimator.Play(playOneShot: true);
        }

        #endregion API Methods

        #region Class Methods

        public void Stop(Action stopAction = null)
        {
            if (spriteAnimator.animations.Count > 2 && spriteAnimator.animations[2] != null)
            {
                spriteAnimator.AnimationStoppedAction = null;
                spriteAnimator.AnimationStoppedAction = () => stopAction?.Invoke();
                var endAnimation = spriteAnimator.animations[2];
                spriteAnimator.Play(endAnimation, playOneShot: true);
            }
            else stopAction?.Invoke();
        }

        protected override void OnStopAnimation()
        {
            if (spriteAnimator.animations.Count >= 1 && spriteAnimator.animations[1] != null)
            {
                var middleAnimation = spriteAnimator.animations[1];
                spriteAnimator.Play(middleAnimation, playOneShot: !isAnimationLoop);
            }
        }

        #endregion Class Methods

        #region Unity Callback Events Methods

        public void StopAndDestroySelf()
            => Stop(DestroySelf);

        #endregion Unity Callback Events Methods
    }
}