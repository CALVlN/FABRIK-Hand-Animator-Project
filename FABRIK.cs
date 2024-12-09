using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq.Expressions;
using System.Security.Cryptography;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class FABRIK : MonoBehaviour
{
    // Variables
    public int numJoints;
    public Transform target;
    public Transform[] joints;
    public Transform[] segments;
    public float[] distances;
    public float armLength;
    public float tolerance = 0.00001f;

    public float activeArmLength;
    public float iterationMaxTracker = 0;

    // Start is called before the first frame update
    void Start() {
        numJoints = joints.Length;
        distances = new float[numJoints];

        // ASSUMES JOINTS WERE SET IN EDITOR
        armLength = 0;
        for (int i = 0; i < numJoints - 1; i++) {
            Transform curTransform = joints[i];
            Transform nextTransform = joints[i + 1];

            distances[i] = Vector3.Distance(nextTransform.position, curTransform.position);
            armLength += distances[i];
        }
    }

    // Update is called once per frame
    void Update() {
        float distToTarget = Vector3.Distance(target.position, joints[0].position);
        // Target unreachable
        if (distToTarget > armLength) {
            for (int i = 0; i < numJoints - 1; i++) {
                // Distance between point and target
                float ri = Vector3.Distance(target.position, joints[i].position);
                float lamb = distances[i] / ri;
                float newX = (1 - lamb) * joints[i].position[0] + lamb * target.position[0];
                float newY = (1 - lamb) * joints[i].position[1] + lamb * target.position[1];
                float newZ = (1 - lamb) * joints[i].position[2] + lamb * target.position[2];
                joints[i + 1].position = new Vector3(newX, newY, newZ);
            }
        } // Target is reachable
        else {
            int maxIter = 32;
            int curIter = 0;
            Vector3 targetPos = target.position;
            Vector3 initialRootPos = joints[0].position;
            Vector3 endEffectorPos = joints[numJoints - 1].position;

            float difA = Vector3.Distance(endEffectorPos, targetPos);
            while (difA > tolerance && curIter < maxIter) {
                // Forward part
                joints[numJoints - 1].position = targetPos;
                for (int i = numJoints - 2; i > 0; i--) {
                    float ri = Vector3.Distance(joints[i + 1].position, joints[i].position);
                    float lamb = distances[i] / ri;

                    joints[i].position = (1 - lamb) * joints[i + 1].position + lamb * joints[i].position;
                }
                // Backward part
                joints[0].position = initialRootPos;
                for (int i = 0; i < numJoints - 2; i++) {
                    float ri = Vector3.Distance(joints[i + 1].position, joints[i].position);
                    float lamb = distances[i] / ri;

                    joints[i + 1].position = (1 - lamb) * joints[i].position + lamb * joints[i + 1].position;
                }

                difA = Vector3.Distance(joints[numJoints - 1].position, targetPos);
                curIter++;
                if (curIter > iterationMaxTracker) {
                    iterationMaxTracker = curIter;
                }
            }
        }

        activeArmLength = 0;
        for (int i = 0; i < numJoints - 1; i++) {
            activeArmLength += Vector3.Distance(joints[i].position, joints[i + 1].position);
        }

        SegmentLocationRotation();
    }

    void SegmentLocationRotation() {
        for (int i = 0; i < numJoints - 1; i++) {
            Vector3 position = (joints[i].position + joints[i + 1].position) / 2f;
            Quaternion rotation = Quaternion.LookRotation(joints[i].position - joints[i + 1].position, Vector3.forward);
            segments[i].position = position;
            segments[i].rotation = rotation;
        }
    }
}
