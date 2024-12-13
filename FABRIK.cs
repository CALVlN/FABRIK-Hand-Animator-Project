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
    [Tooltip("maxAngles stores the maximum angles for joint i, in the order max x1, min x1, max y1, min y1. The z axis is never allowed to rotate.")]
    public Vector4[] maxAngles;
    public Vector3 palmNormal;
    public float armLength;
    public float tolerance = 0.00001f;

    public float activeArmLength;
    public float iterationMaxTracker = 0;

    // Start is called before the first frame update
    void Start() {
        numJoints = joints.Length;
        distances = new float[numJoints];
        maxAngles = new Vector4[numJoints];
        palmNormal = -this.transform.up;

        // ASSUMES JOINTS WERE SET IN EDITOR
        armLength = 0;
        for (int i = 0; i < numJoints - 1; i++) {
            Transform curTransform = joints[i];
            Transform nextTransform = joints[i + 1];

            distances[i] = Vector3.Distance(nextTransform.localPosition, curTransform.localPosition);
            armLength += distances[i];

            maxAngles[i] = new Vector4(90, 0, 90, 0);
        }
    }

    /// <summary>
    /// Takes an upper and lower limit along with two vectors. Returns -1000 if the calculated angle between the vectors
    /// is within limits, or the closest angle limit in degrees if the angle is over/under the limit.
    /// </summary>
    /// <param name="upperLimit"></param>
    /// <param name="lowerLimit"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <returns>The angle limit</returns>
    float AngleConstraintHelper(float upperLimit, float lowerLimit, Vector3 v1, Vector3 v2) {
        float angle = Vector3.Angle(v1, v2);

        if (angle > upperLimit) {
            return angle;
        } else if (angle < lowerLimit) {
            return angle;
        }

        return -1000;
    }

    /// <summary>
    /// Takes three joints, p1, p2, and p3. If the angle between these joints is greater than the angle constraint helper allows,
    /// it calculates a new position for p1 in the coordinate system of p3 that lies within angle constraints. Otherwise, it returns
    /// the position of p1 in the coordinate system of p3.
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <returns>newP1</returns>
    Vector3 DoAngleConstraints(Vector3 p1, Vector3 p2, Vector3 p3) {
        Vector3 newP1 = p1;

        Vector3 v1 = p1 - p2;
        Vector3 v2 = p2 - p3;


        float a = AngleConstraintHelper(maxAngles[0].x, maxAngles[0].y, v1, v2);

        Vector3 cross = Vector3.Cross(v1, v2);

        float dist = Vector3.Distance(p1, p3);
        if (a > 90 || cross.z < 0) {
            Debug.Log("Constrained   " + cross * 100);
        } else {
            Debug.Log("NoConstraints " + cross);
        }


        // Debug.Log(dist);

        return newP1;
    }

    void SegmentLocationRotation() {
        for (int i = 0; i < numJoints - 1; i++) {
            Vector3 position = (joints[i].localPosition + joints[i + 1].localPosition) / 2f;
            Quaternion rotation = Quaternion.LookRotation(joints[i].localPosition - joints[i + 1].localPosition, Vector3.forward);
            segments[i].localPosition = position;
            segments[i].rotation = rotation;
        }
    }

    // Update is called once per frame
    void Update() {
        Vector3 tProjPos = new Vector3(target.localPosition.x, target.localPosition.y, 0);
        Vector3 rootProjPos = new Vector3(joints[0].localPosition.x, joints[0].localPosition.y, 0);
        float distToTarget = Vector3.Distance(tProjPos, rootProjPos);
        // Target unreachable
        if (distToTarget > armLength) {
            for (int i = 0; i < numJoints - 1; i++) {
                // Distance between point and target
                Vector3 jProjPos = new Vector3(joints[i].localPosition.x, joints[i].localPosition.y, 0);
                float ri = Vector3.Distance(tProjPos, jProjPos);
                float lamb = distances[i] / ri;
                float newX = (1 - lamb) * joints[i].localPosition[0] + lamb * target.localPosition[0];
                float newY = (1 - lamb) * joints[i].localPosition[1] + lamb * target.localPosition[1];
                float newZ = (1 - lamb) * joints[i].localPosition[2] + lamb * target.localPosition[2];
                joints[i + 1].localPosition = new Vector3(newX, newY, 0);
            }
        } // Target is reachable
        else {
            int maxIter = 32;
            int curIter = 0;
            Vector3 targetPos = target.localPosition;
            Vector3 initialRootPos = joints[0].localPosition;
            Vector3 endEffectorPos = joints[numJoints - 1].localPosition;

            // PROJECT TARGET ONTO XY PLANE
            targetPos.z = 0;

            float difA = Vector3.Distance(endEffectorPos, targetPos);
            while (difA > tolerance && curIter < maxIter) {
                // Forward part
                joints[numJoints - 1].localPosition = targetPos;
                for (int i = numJoints - 2; i > 0; i--) {
                    float ri = Vector3.Distance(joints[i + 1].localPosition, joints[i].localPosition);
                    float lamb = distances[i] / ri;

                    joints[i].localPosition = (1 - lamb) * joints[i + 1].localPosition + lamb * joints[i].localPosition;

                    float angle = Vector3.Angle(joints[i].localPosition, joints[i + 1].localPosition);
                    // Debug.Log("Forward angles " + angle);
                }
                // Backward part
                joints[0].localPosition = initialRootPos;
                for (int i = 0; i < numJoints - 2; i++) {
                    float ri = Vector3.Distance(joints[i + 1].localPosition, joints[i].localPosition);
                    float lamb = distances[i] / ri;

                    joints[i + 1].localPosition = (1 - lamb) * joints[i].localPosition + lamb * joints[i + 1].localPosition;
                    Vector3 savedPos = joints[i + 1].localPosition;
                    Quaternion savedRot = joints[i + 1].rotation;
                    joints[i].forward = (joints[i].localPosition - joints[i + 1].localPosition).normalized;
                    joints[i + 1].transform.localPosition = savedPos;
                    joints[i + 1].transform.rotation = savedRot;

                    float angle = Vector3.Angle(joints[i].localPosition - joints[i + 1].localPosition, joints[i].localPosition - joints[i + 2].localPosition);
                    // Debug.Log("Backward part " + angle);
                }

                difA = Vector3.Distance(joints[numJoints - 1].localPosition, targetPos);
                curIter++;
                if (curIter > iterationMaxTracker) {
                    iterationMaxTracker = curIter;
                }
            }
            DoAngleConstraints(joints[numJoints - 1].localPosition, joints[numJoints - 2].localPosition, joints[numJoints - 3].localPosition);
        }

        activeArmLength = 0;
        for (int i = 0; i < numJoints - 1; i++) {
            activeArmLength += Vector3.Distance(joints[i].localPosition, joints[i + 1].localPosition);
        }

        // Debug.Log("localPosition: (" + this.transform.localPosition.x + " " + this.transform.localPosition.y + " " + this.transform.localPosition.z + ") localRotation: ("
        // + this.transform.localRotation.x + " " + this.transform.localRotation.y + " " + this.transform.localRotation.z + ")");
        // int ind = numJoints - 1;
        // Debug.Log("LocalPosJoint: (" + joints[ind].transform.localPosition.x + " " + joints[ind].transform.localPosition.y + " " + joints[ind].transform.localPosition.z +
        // ") localRotation: (" + joints[ind].transform.localRotation.x + " " + joints[ind].transform.localRotation.y + " " + joints[ind].transform.localRotation.z + ")");

        SegmentLocationRotation();
    }
}
