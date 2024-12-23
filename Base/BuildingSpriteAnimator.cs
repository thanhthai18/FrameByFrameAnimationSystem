using System;
using Runtime.Animation;

public class BuildingSpriteAnimator : ObjectSpriteAnimator
{
    private void OnEnable()
    {
        if (this.animations.Count > 0)
        {
            Play();
        }
    }
}
