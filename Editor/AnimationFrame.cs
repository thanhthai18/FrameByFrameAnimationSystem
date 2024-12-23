using System;
using UnityEngine;

namespace Runtime.Animation
{
    [Serializable]
    public class AnimationFrame
    {
        #region Members

        protected int duration;
        protected Sprite frame;

        #endregion Members

        #region Properties

        public int Duration
        {
            get { return duration; }
            set { duration = value; }
        }

        public Sprite Frame
        {
            get { return frame; }
            set { frame = value; }
        }

        #endregion Properties

        #region Class Methods

        public AnimationFrame(Sprite frame, int duration)
        {
            this.duration = duration;
            this.frame = frame;
        }

        #endregion Class Methods
    }
}