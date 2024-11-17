using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap;
using Unity.VisualScripting;
using UnityEngine;

public class LeftHandVectorLogger : MonoBehaviour
{
    public LeapProvider leapProvider;

    private void OnEnable()
    {
        leapProvider.OnUpdateFrame += OnUpdateFrame;
    }
    private void OnDisable()
    {
        leapProvider.OnUpdateFrame -= OnUpdateFrame;
    }

    void OnUpdateFrame(Frame frame)
    {
        Hand _leftHand = frame.GetHand(Chirality.Left);
        
        //When we have a valid left hand, we can begin searching for more Hand information
        if(_leftHand != null)
        {
            OnUpdateHand(_leftHand);
        }
    }
    void OnUpdateHand(Hand _hand)
    {
        Finger[] fingers = _hand.fingers;

        // Use the FingerType Enum cast to an int to select a finger from the hand
        Finger _thumb = fingers[(int)Finger.FingerType.THUMB];


        // Iterate through the other 4 fingers
        for (int i = 1; i < 5; i++) {
            Bone metacarpal = _hand.fingers[i].bones[(int)Bone.BoneType.METACARPAL];
            Bone proximal = _hand.fingers[i].bones[(int)Bone.BoneType.PROXIMAL];
            Bone intermediate = _hand.fingers[i].bones[(int)Bone.BoneType.INTERMEDIATE];
            Bone distal = _hand.fingers[i].bones[(int)Bone.BoneType.DISTAL];

            Vector3 qy = (
                metacarpal.NextJoint
                + proximal.Direction 
                - Vector3.Project(proximal.Direction, metacarpal.Basis.yBasis)
            );

            float theta_mcp_aa = Vector3.SignedAngle(
                Vector3.Normalize(qy),
                Vector3.Normalize(metacarpal.NextJoint + metacarpal.Basis.zBasis),
                Vector3.Normalize(metacarpal.NextJoint + metacarpal.Basis.yBasis)
            );
            float theta_mcp_flex = (Vector3.SignedAngle(
                Vector3.Normalize(metacarpal.NextJoint + proximal.Direction),
                Vector3.Normalize(metacarpal.NextJoint + metacarpal.Basis.zBasis),
                Vector3.Normalize(metacarpal.NextJoint + metacarpal.Basis.xBasis)
            ) * -1) + 20;

            float theta_pip = Vector3.Angle(
                Vector3.Normalize(proximal.NextJoint + intermediate.Direction),
                Vector3.Normalize(proximal.NextJoint + proximal.Basis.zBasis)
            );
        }

        // Finger _index = fingers[(int)Finger.FingerType.INDEX];
        // Access the bone array, then get the Metacarpal bone from it using the BoneType Enum cast to an int
        // Bone _indexMetacarpal = _index.bones[(int)Bone.BoneType.METACARPAL];
        // Bone _indexProximal = _index.bones[(int)Bone.BoneType.PROXIMAL];
        // Bone _indexIntermediate = _index.bones[(int)Bone.BoneType.INTERMEDIATE];

        // Debug.DrawLine(_indexMetacarpal.NextJoint, _indexProximal.Direction, Color.red);
        // Debug.DrawLine(_indexMetacarpal.NextJoint, _indexMetacarpal.Basis.xBasis, Color.grey);
        // Debug.DrawLine(_indexMetacarpal.NextJoint, _indexMetacarpal.Basis.yBasis, Color.blue);
        // Debug.DrawLine(_indexMetacarpal.NextJoint, _indexMetacarpal.Basis.zBasis, Color.green);

        // Vector3 normed_x = Vector3.Normalize(_indexMetacarpal.Basis.xBasis);
        // Vector3 normed_y = Vector3.Normalize(_indexMetacarpal.Basis.yBasis);
        // Vector3 normed_z = Vector3.Normalize(_indexMetacarpal.Basis.zBasis);

        // Vector3 qy = _indexMetacarpal.NextJoint + _indexProximal.Direction - Vector3.Project(_indexProximal.Direction, normed_y);

        // // Debug.DrawLine(_indexMetacarpal.NextJoint, qy, Color.white);

        // float mcp_aa = Vector3.SignedAngle(
        //     Vector3.Normalize(qy),
        //     Vector3.Normalize(_indexMetacarpal.NextJoint + _indexMetacarpal.Basis.zBasis),
        //     Vector3.Normalize(_indexMetacarpal.NextJoint + _indexMetacarpal.Basis.yBasis)
        // );
        // float mcp_flex = (Vector3.SignedAngle(
        //     Vector3.Normalize(_indexMetacarpal.NextJoint + _indexProximal.Direction),
        //     Vector3.Normalize(_indexMetacarpal.NextJoint + normed_z),
        //     Vector3.Normalize(_indexMetacarpal.NextJoint + normed_x)
        // ) * -1) + 20;
        // Debug.Log(mcp_aa + "\t" + mcp_flex);

        // PIP
        // Debug.DrawLine(_indexProximal.NextJoint, _indexIntermediate.Direction, Color.red);
        // Debug.DrawLine(_indexProximal.NextJoint, _indexProximal.Basis.xBasis, Color.grey);
        // Debug.DrawLine(_indexProximal.NextJoint, _indexProximal.Basis.yBasis, Color.blue);
        // Debug.DrawLine(_indexProximal.NextJoint, _indexProximal.Basis.zBasis, Color.green);

        // float pip_flex = Vector3.Angle(
        //     Vector3.Normalize(_indexProximal.NextJoint + _indexIntermediate.Direction),
        //     Vector3.Normalize(_indexProximal.NextJoint + _indexProximal.Basis.zBasis)
        // );
        // Debug.Log(pip_flex);

    }
}
