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
        Finger _index = fingers[(int)Finger.FingerType.INDEX];
        Finger _middle = fingers[(int)Finger.FingerType.MIDDLE];
        Finger _ring = fingers[(int)Finger.FingerType.RING];
        Finger _pinky = fingers[(int)Finger.FingerType.PINKY];

        // Access the bone array, then get the Metacarpal bone from it using the BoneType Enum cast to an int
        Bone _thumbMetacarpal = _thumb.bones[(int)Bone.BoneType.METACARPAL];
        Bone _indexMetacarpal = _index.bones[(int)Bone.BoneType.METACARPAL];
        Bone _middleMetacarpal = _middle.bones[(int)Bone.BoneType.METACARPAL];
        Bone _ringMetacarpal = _ring.bones[(int)Bone.BoneType.METACARPAL];
        Bone _pinkyMetacarpal = _pinky.bones[(int)Bone.BoneType.METACARPAL];

        Bone _indexProximal = _index.bones[(int)Bone.BoneType.PROXIMAL];
        Bone _indexIntermediate = _index.bones[(int)Bone.BoneType.INTERMEDIATE];

        // Iterate through the other 4 fingers
        for (int i = 1; i < 5; i++) {
            Bone metacarpal = _hand.fingers[i].bones[(int)Bone.BoneType.METACARPAL];
            Bone proximal = _hand.fingers[i].bones[(int)Bone.BoneType.PROXIMAL];
            Bone intermediate = _hand.fingers[i].bones[(int)Bone.BoneType.INTERMEDIATE];
            Bone distal = _hand.fingers[i].bones[(int)Bone.BoneType.DISTAL];

            float theta_mcp_fe = Quaternion.Angle(proximal.Rotation, metacarpal.Rotation);
            float theta_pip = Quaternion.Angle(intermediate.Rotation, proximal.Rotation);
        }
        // Debug.Log(index_angles[0] + " " + index_angles[1] + " " + index_angles[2]);

        Vector3 pv = _indexProximal.NextJoint - _indexProximal.PrevJoint;
        Vector3 iv = _indexIntermediate.NextJoint - _indexIntermediate.PrevJoint;
        Vector3 pi = (_indexIntermediate.Direction * _indexIntermediate.Length) - (_indexProximal.Direction * _indexProximal.Length);
        // float index_theta_pip = Vector3.Angle(_indexProximal.Direction, pi);
        // float index_theta_pip = Quaternion.Angle(_indexProximal.Rotation, _indexIntermediate.Rotation);
        Vector3 index_pip_angle = _indexIntermediate.Rotation.eulerAngles - _indexProximal.Rotation.eulerAngles;
        // Debug.Log(_hand.Rotation.eulerAngles + "\t" + _indexMetacarpal.Rotation.eulerAngles + "\t" + _indexProximal.Rotation.eulerAngles + "\t" + _indexIntermediate.Rotation.eulerAngles);
        // Debug.Log(_indexIntermediate.Rotation.eulerAngles - _indexProximal.Rotation.eulerAngles);

        // float index_theta_pip = Quaternion.Angle(_indexIntermediate.Rotation, _indexProximal.Rotation);
        float index_theta_mcp_fe = Quaternion.Angle(_indexMetacarpal.Rotation, _indexProximal.Rotation);
        float index_theta_pip = Quaternion.Angle(_indexProximal.Rotation, _indexIntermediate.Rotation);
        float k = (index_theta_mcp_fe / index_theta_pip);
        // Debug.Log(index_theta_mcp_fe + "\t" + index_theta_pip + "\t" + k);


        // Theta_mcp_aa
        const float mcp_aa_lower_limit = -15;
        const float mcp_aa_upper_limit = 15;

        Debug.DrawLine(_indexMetacarpal.NextJoint, _indexProximal.Direction, Color.red);

        Debug.DrawLine(_indexMetacarpal.NextJoint, _indexMetacarpal.Basis.xBasis, Color.grey);
        Debug.DrawLine(_indexMetacarpal.NextJoint, _indexMetacarpal.Basis.yBasis, Color.blue);
        Debug.DrawLine(_indexMetacarpal.NextJoint, _indexMetacarpal.Basis.zBasis, Color.green);

        // Debug.DrawLine(_indexProximal.NextJoint, _indexProximal.Basis.xBasis, Color.grey);
        // Debug.DrawLine(_indexProximal.NextJoint, _indexProximal.Basis.yBasis, Color.blue);
        // Debug.DrawLine(_indexProximal.NextJoint, _indexProximal.Basis.zBasis, Color.green);

        Vector3 normed_x = Vector3.Normalize(_indexMetacarpal.Basis.xBasis);
        Vector3 normed_y = Vector3.Normalize(_indexMetacarpal.Basis.yBasis);
        Vector3 normed_z = Vector3.Normalize(_indexMetacarpal.Basis.zBasis);

        // float vnx = Vector3.Dot(_indexProximal.Direction, normed_x);
        // float vny = Vector3.Dot(_indexProximal.Direction, normed_y);
        // float vnz = Vector3.Dot(_indexProximal.Direction, normed_z);

        // Vector3 px = Vector3.Project(vnx * normed_x, _indexMetacarpal.Basis.xBasis);
        // Vector3 py = Vector3.Project(vny * normed_y, _indexMetacarpal.Basis.yBasis);
        // Vector3 pz = Vector3.Project(vnz * normed_z, _indexMetacarpal.Basis.zBasis);

        // Debug.DrawRay(_indexMetacarpal.NextJoint, px, Color.magenta);
        // Debug.DrawRay(_indexMetacarpal.NextJoint, py, Color.white);
        // Debug.DrawRay(_indexMetacarpal.NextJoint, pz, Color.cyan);

        // Vector3 qx = _indexMetacarpal.NextJoint + _indexProximal.Direction - Vector3.Project(_indexProximal.Direction, normed_x);
        Vector3 qy = _indexMetacarpal.NextJoint + _indexProximal.Direction - Vector3.Project(_indexProximal.Direction, normed_y);
        // Vector3 qz = _indexMetacarpal.NextJoint + _indexProximal.Direction - Vector3.Project(_indexProximal.Direction, normed_z);

        // Debug.DrawRay(_indexMetacarpal.NextJoint, qx, Color.magenta);
        Debug.DrawLine(_indexMetacarpal.NextJoint, qy, Color.white);
        // Debug.DrawLine(_indexMetacarpal.NextJoint, qz, Color.cyan);

        float abduct = Vector3.SignedAngle(
            Vector3.Normalize(qy),
            Vector3.Normalize(_indexMetacarpal.NextJoint + _indexMetacarpal.Basis.zBasis),
            Vector3.Normalize(_indexMetacarpal.NextJoint + _indexMetacarpal.Basis.yBasis)
        );
        // abduct = Mathf.Clamp(abduct, -15, 15);
        float flex = (Vector3.SignedAngle(
            Vector3.Normalize(_indexMetacarpal.NextJoint + _indexProximal.Direction),
            Vector3.Normalize(_indexMetacarpal.NextJoint + normed_z),
            Vector3.Normalize(_indexMetacarpal.NextJoint + normed_x)
        ) * -1) + 20;

        Debug.Log(abduct + "\t" + flex);

        
    }
}
