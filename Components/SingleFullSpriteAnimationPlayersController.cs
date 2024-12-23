using System;
using System.Linq;
using UnityEngine;

namespace Runtime.Animation
{
    /// <summary>
    /// This component holds some single full sprite animation players and control those.<br/>
    /// </summary>
    public class SingleFullSpriteAnimationPlayersController : MonoBehaviour
    {
        #region Members

        [SerializeField]
        private SingleFullSpriteAnimationPlayerData[] _controlledSingleFullSpriteAnimationPlayersData;

        #endregion Members

        #region API Methods

        private void Awake()
        {
            if (_controlledSingleFullSpriteAnimationPlayersData.Length > 0)
            {
                bool thereIsNoTheController = true;
                for (int i = 0; i < _controlledSingleFullSpriteAnimationPlayersData.Length; i++)
                {
                    if (_controlledSingleFullSpriteAnimationPlayersData[i].isTheController)
                    {
                        thereIsNoTheController = false;
                        break;
                    }
                }
                if (thereIsNoTheController)
                    _controlledSingleFullSpriteAnimationPlayersData[0].isTheController = true;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (_controlledSingleFullSpriteAnimationPlayersData.Count(x => x.isTheController) > 1)
            {
                Debug.LogError("Can not have more than one controller. Reset all!");
                for (int i = 0; i < _controlledSingleFullSpriteAnimationPlayersData.Length; i++)
                    _controlledSingleFullSpriteAnimationPlayersData[i].isTheController = false;
            }
        }
#endif
        #endregion API Methods

        #region Class Methods

        public void StopAnimation(Action onStoppedAction)
        {
            for (int i = 0; i < _controlledSingleFullSpriteAnimationPlayersData.Length; i++)
            {
                if (_controlledSingleFullSpriteAnimationPlayersData[i].isTheController)
                    _controlledSingleFullSpriteAnimationPlayersData[i].singleFullSpriteAnimationPlayer.Stop(onStoppedAction);
                else
                    _controlledSingleFullSpriteAnimationPlayersData[i].singleFullSpriteAnimationPlayer.Stop();
            }
        }

        #endregion Class Methods

        #region Owner Structs

        [Serializable]
        public struct SingleFullSpriteAnimationPlayerData
        {
            #region Members

            public SingleFullSpriteAnimationPlayer singleFullSpriteAnimationPlayer;
            public bool isTheController;

            #endregion Members
        }

        #endregion Owner Structs
    }
}