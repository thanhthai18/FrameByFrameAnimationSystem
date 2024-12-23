using System.Collections.Generic;
using UnityEngine;

namespace Runtime.Animation
{
    public class SpriteAnimation : ScriptableObject
    {
        #region Members

        [SerializeField]
        protected int fps = 30;
        [SerializeField]
        protected int totalDuration = -1;
        [SerializeField]
        protected List<Sprite> frames;
        [SerializeField]
        protected List<int> framesDuration;
        [SerializeField]
        protected bool useSpriteNamePostFixAsDuration;

        #endregion Members

        #region Properties

        /// <summary>
        /// Name property.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        /// <summary>
        /// FPS property
        /// </summary>
        public int FPS
        {
            get { return fps; }
            set { fps = value; }
        }

        /// <summary>
        /// Frames on this animation.
        /// </summary>
        public int FramesCount
        {
            get { return frames.Count; }
        }

        /// <summary>
        /// List of sprites.
        /// </summary>
        public List<Sprite> Frames
        {
            get { return frames; }
            set { frames = value; }
        }

        /// <summary>
        /// List with the duration of each frame.
        /// </summary>
        public List<int> FramesDuration
        {
            get { return framesDuration; }
            set { framesDuration = value; }
        }

        /// <summary>
        /// The total duration of the animation (in frames).
        /// </summary>
        public int AnimationDuration
        {
            get { return totalDuration; }
        }

        /// <summary>
        /// Use Sprite name's post fix as duration.
        /// </summary>
        public bool UseSpriteNamePostFixAsDuration
        {
            get { return useSpriteNamePostFixAsDuration; }
            set { useSpriteNamePostFixAsDuration = value; }
        }

        #endregion Properties

        #region Class Methods

        /// <summary>
        /// Creates an empty animation.
        /// </summary>
        public SpriteAnimation()
        {
            frames = new List<Sprite>();
            framesDuration = new List<int>();
        }

        /// <summary>
        /// Initialize values. 
        /// </summary>
        public void Setup()
        {
            totalDuration = 0;
            for (int i = 0; i < framesDuration.Count; i++)
                totalDuration += framesDuration[i];
        }

        /// <summary>
        /// Returns the sprite on the selected frame.
        /// </summary>
        public Sprite GetFrame(int index)
        {
            if (index < frames.Count)
                return frames[index];
            else
                return null;
        }

        /// <summary>
        /// Returns the duration (in frames) of the selected frame.
        /// </summary>
        public int GetFrameDuration(int index)
            => framesDuration[index];

        /// <summary>
        /// Get the frame at the specified time using the animation frame rate.
        /// </summary>
        public int GetFrameAtTime(float time)
            => GetFrameAtTime(time, fps);

        /// <summary>
        /// Get the frame at the specified time using the specified frame rate.
        /// </summary>
        public int GetFrameAtTime(float time, int frameRate)
        {
            int index = 0;
            float timePerFrame = 1f / frameRate;
            float totalAnimationTime = totalDuration * timePerFrame;

            if (time >= totalAnimationTime)
            {
                index = frames.Count - 1;
            }
            else if (time <= 0)
            {
                index = 0;
            }
            else
            {
                int frameDurationCounter = 0;

                while (time >= timePerFrame)
                {
                    time -= timePerFrame;
                    frameDurationCounter++;

                    if (frameDurationCounter >= framesDuration[index])
                    {
                        index++;
                        frameDurationCounter = 0;
                    }
                }

                if (index >= frames.Count)
                    index = frames.Count - 1;
            }

            return index;
        }

        /// <summary>
        /// Get the total duration of the animation in seconds using the animation frame rate.
        /// </summary>
        public float GetAnimationDurationInSeconds()
            => (float)AnimationDuration / fps;

        /// <summary>
        /// Get the frame at the specified normalized time (between 0 and 1) using the animation frame rate.
        /// </summary>
        public int GetFrameAtNormalizedTime(float normalizedTime)
        {
            normalizedTime = Mathf.Clamp(normalizedTime, 0f, 1f);
            return GetFrameAtTime(totalDuration * (1f / fps) * normalizedTime, fps);
        }

        /// <summary>
        /// Get the frame at the specified normalized time (between 0 and 1) using the specified frame rate.
        /// </summary>
        public int GetFrameAtNormalizedTime(float normalizedTime, int frameRate)
        {
            normalizedTime = Mathf.Clamp(normalizedTime, 0f, 1f);
            return GetFrameAtTime(totalDuration * (1f / frameRate) * normalizedTime, frameRate);
        }

        #endregion Class Methods
    }
}