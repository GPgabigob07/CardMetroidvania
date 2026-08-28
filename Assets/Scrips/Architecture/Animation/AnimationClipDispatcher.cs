using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TicGame.Architecture
{
    [Serializable]
    struct EventClip
    {
        public string ClipName;
        public VoidEventChannelSO trigger;
    }

    public class AnimationClipDispatcher : MonoBehaviour
    {
        [SerializeField] Animator animator;

        [SerializeField] private List<EventClip> animationClipEvents = new();

        private List<AnimationClipEventLink> animationClipEventLinks = new();

        private void OnEnable() {
            animationClipEventLinks = animationClipEvents
                                      .Select(e =>
                                          new AnimationClipEventLink(
                                              eventSo: e.trigger,
                                              animationClipName: e.ClipName,
                                              animator: animator
                                          )
                                      )
                                      .ToList();

            foreach (var link in animationClipEventLinks) {
                link.Subscribe();
            }
        }

        private void OnDisable() {
            foreach (var link in animationClipEventLinks) {
                link.Unsubscribe();
            }

            animationClipEventLinks = new List<AnimationClipEventLink>();
        }
    }

    sealed class AnimationClipEventLink
    {
        private VoidEventChannelSO eventSo;
        private string animationClipName;
        private Animator animator;

        public AnimationClipEventLink(
            VoidEventChannelSO eventSo,
            string animationClipName,
            Animator animator
        ) {
            this.eventSo = eventSo;
            this.animationClipName = animationClipName;
            this.animator = animator;
        }

        public void Subscribe() {
            eventSo.Raised += TriggerAnimation;
        }

        public void Unsubscribe() {
            eventSo.Raised -= TriggerAnimation;
        }

        private void TriggerAnimation() {
            if (animator) {
                animator.Play(animationClipName);
            }
        }
    }
}