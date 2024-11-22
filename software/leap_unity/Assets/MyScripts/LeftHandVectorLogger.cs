using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap;
using Unity.VisualScripting;
using UnityEngine;

public class LeftHandVectorLogger : MonoBehaviour
{
    public LeapProvider leapProvider;

     // Toggle for saving data
    [Header("CSV Options")]
    public bool saveToCSV = true; // Checkbox in Inspector

    [SerializeField]
    [Header("The number of samples before flushing the data to the csv Options")]
    public int maxFlush = 1024;

    [SerializeField]
    public string fileName = "leapData.csv";

    private List<string> headers = new List<string>
    {
        "Timestamp",
        "Millis",
        "thumb_tm_aa",
        "thumb_tm_flex",
        "thumb_mcp_aa",
        "thumb_mcp_flex",
        "thumb_ip",
        "index_mcp_aa",
        "index_mcp_flex",
        "index_pip",
        "middle_mcp_aa",
        "middle_mcp_flex",
        "middle_pip",
        "ring_mcp_aa",
        "ring_mcp_flex",
        "ring_pip",
        "little_mcp_aa",
        "little_mcp_flex",
        "little_pip"
    };

    private string filePath;

    // Buffer to store data before writing
    private List<string> buffer = new List<string>();

    // Counter to flush the buffer every maxFlush samples
    private int flushCounter = 0;


    void Start() {
        if (! saveToCSV) {
            return;
        }

        // Set the file path 
        string dataDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../data/"));
        filePath = Path.Combine(dataDirectory, fileName);
        
        // Write CSV headers (optional)
        if (!File.Exists(filePath))
        {            
            File.AppendAllText(filePath, string.Join(",", headers) + Environment.NewLine);
        }
    }

    void BufferData(List<float> values, DateTime timestamp)
    {
        // Prepare the row: Timestamp followed by values
        List<string> formattedValues = values.ConvertAll(value => value.ToString("F3"));

        string ctimeFormat = timestamp.ToString("ddd MMM dd HH:mm:ss yyyy");
        string row = ctimeFormat + "," + timestamp.Millisecond + "," + string.Join(",", formattedValues);
        buffer.Add(row);
    }

    void FlushBufferToFile()
    {
        if (buffer.Count > 0)
        {
            // Append all buffered rows to the file at once
            File.AppendAllText(filePath, string.Join(Environment.NewLine, buffer) + Environment.NewLine);

            // Clear the buffer
            buffer.Clear();

            Debug.Log("Buffer flushed to file.");
        }
    }

    private void OnEnable()
    {
        leapProvider.OnUpdateFrame += OnUpdateFrame;
    }
    private void OnDisable()
    {
        leapProvider.OnUpdateFrame -= OnUpdateFrame;
    }
    void OnApplicationQuit()
    {
        FlushBufferToFile();
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
        List<float> finger_angles = new List<float>();

        // Calculate the thumb special case
        Finger _thumb = _hand.fingers[(int)Finger.FingerType.THUMB];

        // Leap treats the thumb metacarpal as length 0 bone to represent the TM 
        Bone _thumbMetacarpal = _thumb.bones[(int)Bone.BoneType.METACARPAL];
        Bone _thumbProximal = _thumb.bones[(int)Bone.BoneType.PROXIMAL];
        Bone _thumbIntermediate = _thumb.bones[(int)Bone.BoneType.INTERMEDIATE];
        Bone _thumbDistal = _thumb.bones[(int)Bone.BoneType.DISTAL];

        // Thumb TM
        float thumb_theta_tm_aa = Vector3.SignedAngle(
            Vector3.Normalize(_thumbMetacarpal.NextJoint + _thumbProximal.Direction),
            Vector3.Normalize(_thumbMetacarpal.NextJoint + _thumbMetacarpal.Basis.zBasis),
            Vector3.Normalize(_thumbMetacarpal.NextJoint + _thumbMetacarpal.Basis.yBasis)
        );
        float thumb_theta_tm_flex = (Vector3.SignedAngle(
            Vector3.Normalize(_thumbMetacarpal.NextJoint + _thumbProximal.Direction),
            Vector3.Normalize(_thumbMetacarpal.NextJoint + _thumbMetacarpal.Basis.zBasis),
            Vector3.Normalize(_thumbMetacarpal.NextJoint + _thumbMetacarpal.Basis.xBasis)
        ) * -1) + 0;
        finger_angles.Add(thumb_theta_tm_aa);
        finger_angles.Add(thumb_theta_tm_flex);

        // Thumb MCP flexion/extension and abduction/adduction
        float thumb_theta_mcp_aa = Vector3.SignedAngle(
            Vector3.Normalize(_thumbProximal.NextJoint + _thumbIntermediate.Direction),
            Vector3.Normalize(_thumbProximal.NextJoint + _thumbProximal.Basis.zBasis),
            Vector3.Normalize(_thumbProximal.NextJoint + _thumbProximal.Basis.yBasis)
        );
        float thumb_theta_mcp_flex = (Vector3.SignedAngle(
            Vector3.Normalize(_thumbProximal.NextJoint + _thumbIntermediate.Direction),
            Vector3.Normalize(_thumbProximal.NextJoint + _thumbProximal.Basis.zBasis),
            Vector3.Normalize(_thumbProximal.NextJoint + _thumbProximal.Basis.xBasis)
        ) * -1) + 0;
        finger_angles.Add(thumb_theta_mcp_aa);
        finger_angles.Add(thumb_theta_mcp_flex);

        // Thumb IP angle can be calculated from theta_mcp_fe
        float theta_ip = Vector3.SignedAngle(
            Vector3.Normalize(_thumbIntermediate.NextJoint + _thumbDistal.Direction),
            Vector3.Normalize(_thumbIntermediate.NextJoint + _thumbIntermediate.Basis.zBasis),
            Vector3.Normalize(_thumbIntermediate.NextJoint + _thumbIntermediate.Basis.xBasis)
        );
        theta_ip *= -1;
        finger_angles.Add(theta_ip);

        // Iterate through the 4 main fingers
        for (int i = 1; i < 5; i++) {
            Bone metacarpal = _hand.fingers[i].bones[(int)Bone.BoneType.METACARPAL];
            Bone proximal = _hand.fingers[i].bones[(int)Bone.BoneType.PROXIMAL];
            Bone intermediate = _hand.fingers[i].bones[(int)Bone.BoneType.INTERMEDIATE];
            Bone distal = _hand.fingers[i].bones[(int)Bone.BoneType.DISTAL];

            Vector3 qy = (
                metacarpal.NextJoint
                + proximal.Direction 
                - Vector3.Project(proximal.Direction, metacarpal.Basis.yBasis) // May need to normalize before projecting?
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
            finger_angles.Add(theta_mcp_aa);
            finger_angles.Add(theta_mcp_flex);
            finger_angles.Add(theta_pip);
        }
        
        // Debug print finger joint angles
        // Debug.Log(String.Join(", ", finger_angles));

        if (saveToCSV) {
            // Get the current time
            DateTime now = DateTime.Now;
            BufferData(finger_angles, now);

            if (flushCounter >= maxFlush) {
                FlushBufferToFile();
                flushCounter = 0;
            }

            flushCounter++;
        }
    }
}
