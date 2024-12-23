using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Runtime.Animation
{
    public enum LoopType
    {
        Repeat,
        Yoyo
    }

    /// <summary>
    /// This controls the animation that uses sprites.
    /// </summary>
    public abstract class SpriteAnimator : MonoBehaviour
    {
        #region Members

        public List<SpriteAnimation> animations;
        [SerializeField]
        protected bool useScaledDeltaTime = false;
        [SerializeField]
        protected int initFrame = 0;
        protected bool playing = false;
        protected bool waitingLoop = false;
        protected bool currentOneShot = false;
        protected bool currentBackwards = false;
        protected int frameIndex = 0;
        protected int framesInAnimation = 0;
        protected int frameDurationCounter = 0;
        protected int currentFramerate = 60;
        protected float animationTimer = 0f;
        protected float currentAnimationTime = 0f;
        protected float loopTimer = 0f;
        protected float timePerFrame = 0f;
        protected LoopType currentLoopType;
        protected SpriteAnimation currentAnimation;
        protected string previousAnimationName;
        protected bool originalUseScaledDeltaTime;

        #endregion Members

        #region Properties

        public int FrameIndex
        {
            get
            {
                return frameIndex;
            }
        }

        public SpriteAnimation CurrentAnimation => currentAnimation;
        public string PreviousAnimationName => previousAnimationName;

        public bool IsPlaying => playing;

        /// <summary>
        /// Called when the animation is stopped.
        /// </summary>
        public Action AnimationStoppedAction { get; set; }
        private Action CurrentSkillEventTriggeredCallbackAction { get; set; }
        public Action CurrentAnimationStoppedAction { get; set; }
        private int CurrentEventTriggeredFrame { get; set; }

        #endregion Properties

        #region API Methods

        private void Start()
        {
            originalUseScaledDeltaTime = useScaledDeltaTime;
            this.frameIndex = this.initFrame;
        }

        private void Update()
        {
            // We do nothing if the current FPS <= 0.
            if (currentAnimation == null || currentFramerate <= 0 || !playing)
                return;

            if (!waitingLoop)
                UpdateAnimation();
            else
                UpdateLoop();
        }

        #endregion API Methods

        #region Class Methods

        /// <summary>
        /// Plays the specified animation.
        /// </summary>
        public void Play(SpriteAnimation animation, float animateSpeedMultiplier = 1.0f, bool playOneShot = false, Action eventTriggeredCallbackAction = null,
                         int eventTriggeredFrame = -1, bool playBackwards = false, LoopType loopType = LoopType.Repeat, Action eventStoppedCallbackAction = null)
        {
            CurrentSkillEventTriggeredCallbackAction = null;
            if (eventTriggeredCallbackAction != null)
            {
                CurrentSkillEventTriggeredCallbackAction = eventTriggeredCallbackAction;
                CurrentEventTriggeredFrame = eventTriggeredFrame;
            }
            
            if (eventStoppedCallbackAction != null)
            {
                CurrentAnimationStoppedAction = eventStoppedCallbackAction;
            }
            currentOneShot = playOneShot;
            currentBackwards = playBackwards;
            currentLoopType = loopType;

            // If it's the same animation but not playing, reset it, if playing, do nothing.
            if (currentAnimation != null && currentAnimation.Equals(animation))
            {
                if (!playing)
                {
                    Restart();
                    Resume();
                }
                else return;
            }
            // If the animation is new, save it as current animation and play it.
            else
            {
                SetPreviousAnimationName();
                currentAnimation = animation;
            }

            StartPlay(animateSpeedMultiplier);
        }

        /// <summary>
        /// Plays the first animation of the animation list.
        /// </summary>
        public void Play(float animateSpeedMultiplier = 1.0f, bool playOneShot = false, Action eventTriggeredCallbackAction = null, int eventTriggeredFrame = -1,
                         bool playBackwards = false, LoopType loopType = LoopType.Repeat, Action eventStoppedCallbackAction = null)
        {
            Play(animations[0].Name, animateSpeedMultiplier, playOneShot, eventTriggeredCallbackAction, eventTriggeredFrame, playBackwards, loopType, eventStoppedCallbackAction);
        }

        /// <summary>
        /// Plays an animation.
        /// </summary>
        public virtual void Play(string animationName, float animateSpeedMultiplier = 1.0f, bool playOneShot = false, Action eventTriggeredCallbackAction = null,
                         int eventTriggeredFrame = -1, bool playBackwards = false, LoopType loopType = LoopType.Repeat, Action eventStoppedCallbackAction = null)
        {
            CurrentSkillEventTriggeredCallbackAction = null;
            if (eventTriggeredCallbackAction != null)
            {
                CurrentSkillEventTriggeredCallbackAction = eventTriggeredCallbackAction;
                CurrentEventTriggeredFrame = eventTriggeredFrame;
            }

            CurrentAnimationStoppedAction = eventStoppedCallbackAction;
            currentOneShot = playOneShot;
            currentBackwards = playBackwards;
            currentLoopType = loopType;

            // If it's the same animation but not playing, reset it, if playing, do nothing.
            if (currentAnimation != null && currentAnimation.Name.Equals(animationName))
            {
                if (!playing)
                {
                    Restart();
                    Resume();
                }
                else return;
            }
            // Look for the animation only if its new or current animation is null
            else if (currentAnimation == null || !currentAnimation.Name.Equals(animationName))
            {
                SetPreviousAnimationName();
                currentAnimation = GetAnimation(animationName);
            }

            StartPlay(animateSpeedMultiplier);
        }

        public float GetAnimationTime(string animationName)
        {
            var animation = GetAnimation(animationName);
            return animation.GetAnimationDurationInSeconds();
        }

        public void UpdateUseScaledDeltaTime(bool useScaledDeltaTime)
            => this.useScaledDeltaTime = useScaledDeltaTime;

        public void ResetUseScaledDeltaTime()
            => this.useScaledDeltaTime = originalUseScaledDeltaTime;

        /// <summary>
        /// Tint the sprite's color.
        /// </summary>
        public virtual void TintColor(Color color) { }
        
        public virtual void FadeOut(float duration, Action onCompleted) { }
        public virtual void FadeIn(float duration, Action onCompleted) { }

        /// <summary>
        /// Pause the animation.
        /// </summary>
        public void Pause()
        {
            if (currentAnimation != null)
                playing = false;
        }

        /// <summary>
        /// Resumes the animation.
        /// </summary>
        public void Resume()
        {
            if (currentAnimation != null)
                playing = true;
        }

        /// <summary>
        /// Stops the animation.
        /// </summary>
        public void Stop()
        {
            playing = false;
            AnimationStoppedAction?.Invoke();
            CurrentAnimationStoppedAction?.Invoke();
        }

        /// <summary>
        /// Restarts the animation. If the animation is not playing the effects will apply when starts playing.
        /// </summary>
        public void Restart()
        {
            animationTimer = 0;
            frameIndex = (currentBackwards) ? framesInAnimation - 1 : 0;
            frameDurationCounter = 0;
            ChangeFrame(frameIndex);
        }

        private void UpdateAnimation()
        {
            if (useScaledDeltaTime)
            {
                animationTimer += Time.deltaTime;
                currentAnimationTime = !currentBackwards ? currentAnimationTime + Time.deltaTime : currentAnimationTime - Time.deltaTime;
            }
            else
            {
                animationTimer += Time.unscaledDeltaTime;
                currentAnimationTime = !currentBackwards ? currentAnimationTime + Time.unscaledDeltaTime : currentAnimationTime - Time.unscaledDeltaTime;
            }
            if (animationTimer >= timePerFrame)
            {
                // Check frame skips.
                while (animationTimer >= timePerFrame)
                {
                    frameDurationCounter++;
                    animationTimer -= timePerFrame;
                }
                
                if (frameIndex < 0 || frameIndex >= currentAnimation.FramesDuration.Count)
                {
                    Debug.LogWarning("Frame index is out of range. Resetting to valid index.");
                    frameIndex = 0;  // Reset to a valid index, or handle accordingly
                }

                // Change frame only if have passed the desired frames.
                if (frameIndex < currentAnimation.FramesDuration.Count && frameDurationCounter >= currentAnimation.FramesDuration[frameIndex])
                {
                    while (frameIndex < currentAnimation.FramesDuration.Count && frameDurationCounter >= currentAnimation.FramesDuration[frameIndex])
                    {
                        frameDurationCounter -= currentAnimation.FramesDuration[frameIndex];
                        frameIndex = (currentBackwards) ? frameIndex - 1 : frameIndex + 1;

                        if (CurrentEventTriggeredFrame != -1 && CurrentEventTriggeredFrame == frameIndex)
                        {
                            if (CurrentSkillEventTriggeredCallbackAction != null)
                                CurrentSkillEventTriggeredCallbackAction.Invoke();
                        }

                        // Check last or first frame.
                        if (CheckLastFrame())
                        {
                            if (currentOneShot)
                            {
                                Stop();
                                return;
                            }
                            else
                            {
                                waitingLoop = true;
                                loopTimer = 0;
                            }
                        }
                        else
                        {
                            // Change sprite.
                            ChangeFrame(frameIndex);
                        }
                    }
                }
            }
        }

        private void UpdateLoop()
        {
            if (useScaledDeltaTime)
                loopTimer -= Time.deltaTime;
            else
                loopTimer -= Time.unscaledDeltaTime;
            if (loopTimer <= 0)
            {
                if (currentLoopType == LoopType.Yoyo)
                    currentBackwards = !currentBackwards;

                waitingLoop = false;
                animationTimer = 0;
                currentAnimationTime = (currentBackwards) ? currentAnimation.AnimationDuration * timePerFrame : 0;
                frameIndex = (currentBackwards) ? framesInAnimation - 1 : 0;

                ChangeFrame(frameIndex);
            }
        }

        private void StartPlay(float animateSpeedMultiplier)
        {
            if (currentAnimation.AnimationDuration == -1)
                currentAnimation.Setup();

            currentFramerate = (int)(currentAnimation.FPS * animateSpeedMultiplier);
            timePerFrame = 1.0f / currentFramerate;
            framesInAnimation = currentAnimation.FramesCount;
            currentAnimationTime = currentBackwards ? currentAnimation.AnimationDuration * timePerFrame : 0;
            loopTimer = 0;

            frameIndex = (currentBackwards) ? framesInAnimation - 1 : 0;
            frameDurationCounter = 0;
            
            // Check if the animation have frames. Show warning if not.
            if (framesInAnimation == 0)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("Animation '" + name + "' has no frames.", gameObject);
#endif
                playing = false;
                return;
            }

            Restart();
            playing = true;

            if (!waitingLoop)
                ChangeFrame(frameIndex);
        }

        /// <summary>
        /// Search an animation with the given name.
        /// </summary>
        /// <returns>
        /// The animation. Null if not found.
        /// </returns>
        protected SpriteAnimation GetAnimation(string animationName)
            => animations.Find(x => x.Name.Equals(animationName));

        /// <summary>
        /// Changes the renderer to the given sprite
        /// </summary>
        public virtual void ChangeFrame(int frameIndex) { }
        public virtual void ClearRenderer() { }

        /// <summary>
        /// Sets the animation time to the specified time, updating de sprite to the correspondent frame at that time.
        /// </summary>
        /// <param name="time">Time in seconds</param>
        public void SetAnimationTime(float time)
        {
            if (currentAnimation != null)
            {
                float timePerFrame = 1f / currentFramerate;
                float totalAnimationTime = currentAnimation.AnimationDuration * timePerFrame;

                if (time >= totalAnimationTime)
                {
                    currentAnimationTime = totalAnimationTime;
                    animationTimer = timePerFrame;
                    frameIndex = framesInAnimation - 1;
                    frameDurationCounter = currentAnimation.FramesDuration[frameIndex] - 1;
                }
                else if (time <= 0)
                {
                    animationTimer = 0;
                    frameIndex = 0;
                    frameDurationCounter = 0;
                    currentAnimationTime = 0;
                }
                else
                {
                    frameIndex = 0;
                    frameDurationCounter = 0;
                    currentAnimationTime = time;

                    while (time >= timePerFrame)
                    {
                        time -= timePerFrame;
                        frameDurationCounter++;

                        if (frameDurationCounter >= currentAnimation.FramesDuration[frameIndex])
                        {
                            frameIndex++;
                            frameDurationCounter = 0;
                        }
                    }

                    if (frameIndex >= framesInAnimation)
                        frameIndex = framesInAnimation - 1;

                    animationTimer = time;
                }

                ChangeFrame(frameIndex);
            }
        }

        /// <summary>
        /// Sets the animation time to the specified normalized time (between 0 and 1), updating de sprite to the correspondent frame at that time.
        /// </summary>
        /// <param name="time">Time normalized (between 0 and 1).</param>
        public void SetAnimationNormalizedTime(float normalizedTime)
        {
            if (currentAnimation != null)
            {
                normalizedTime = Mathf.Clamp(normalizedTime, 0f, 1f);
                SetAnimationTime(currentAnimation.AnimationDuration * timePerFrame * normalizedTime);
            }
        }

        /// <summary>
        /// Check the last frame (backwards or not).
        /// </summary>
        private bool CheckLastFrame()
        {
            if ((!currentBackwards && frameIndex > framesInAnimation - 1))
            {
                frameIndex = framesInAnimation - 1;
                return true;
            }
            else if (currentBackwards && frameIndex < 0)
            {
                frameIndex = 0;
                return true;
            }

            return false;
        }

        protected void SetPreviousAnimationName()
        {
            if (currentAnimation != null)
            {
                previousAnimationName = currentAnimation.Name;
            }
        }

        #endregion Class Methods
    }
}