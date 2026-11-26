using System;
using System.Collections.Generic;
using UnityEngine;

namespace PinkApocalypse.Player
{
    /// <summary>Holds the armature at its imported pose while body locomotion animates.</summary>
    [DisallowMultipleComponent]
    public sealed class CharacterArmAnimationBlocker : MonoBehaviour
    {
        private readonly List<Pose> armPose = new List<Pose>();

        private struct Pose
        {
            public Transform transform;
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        public void Initialize(Animator animator)
        {
            armPose.Clear();
            if (animator == null) return;

            AddArm(animator, HumanBodyBones.LeftUpperArm, "LeftUpperArm");
            AddArm(animator, HumanBodyBones.RightUpperArm, "RightUpperArm");
        }

        private void AddArm(Animator animator, HumanBodyBones humanBone, string fallbackName)
        {
            Transform root = animator.isHuman ? animator.GetBoneTransform(humanBone) : null;
            if (root == null)
            {
                foreach (Transform item in animator.GetComponentsInChildren<Transform>(true))
                {
                    if (string.Equals(item.name, fallbackName, StringComparison.OrdinalIgnoreCase))
                    {
                        root = item;
                        break;
                    }
                }
            }

            if (root == null) return;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                bool alreadyAdded = false;
                for (int i = 0; i < armPose.Count; i++)
                    if (armPose[i].transform == item) { alreadyAdded = true; break; }
                if (alreadyAdded) continue;

                armPose.Add(new Pose
                {
                    transform = item,
                    localPosition = item.localPosition,
                    localRotation = item.localRotation
                });
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < armPose.Count; i++)
            {
                Pose pose = armPose[i];
                if (pose.transform == null) continue;
                pose.transform.localPosition = pose.localPosition;
                pose.transform.localRotation = pose.localRotation;
            }
        }
    }
}
