using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;

public class FABRIK : MonoBehaviour
{
    // Variables
    public int numJoints;
    public Transform target;
    public Transform[] joints = new Transform[3];
    public float[] distances;
    public float armLength;

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

        // TEST SETTING TRANSFORM
        // joints[2].position = new Vector3(-15, 2, 0);
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
        }
    }
}
