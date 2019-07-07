using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeviationCalculator
{
    private Queue<Vector3> positions = new Queue<Vector3>();
    private Queue<float> rotationDiffs = new Queue<float>();
    private Quaternion lastRotation = Quaternion.identity;
    private int windowLength;
    private float positionDeviation;
    private float rotationDeviation;
    private bool rotationInit = false;

    public float PositionDeviation
    {
        get
        {
            int count = positions.Count;
            if (count == 0)
            {
                return 0;
            }

            Vector3 sum = Vector3.zero;
            foreach (Vector3 position in positions)
            {
                sum += position;
            }
            Vector3 average = sum / count;

            float sumSquared = 0f;
            foreach (Vector3 position in positions)
            {
                sumSquared += (position - average).sqrMagnitude;
            }

            return sumSquared / count;
        }
    }

    public float RotationDeviation
    {
        get
        {
            int count = rotationDiffs.Count;
            if (count == 0)
            {
                return 0;
            }

            float sum = 0f;
            float sumSquared = 0f;
            foreach (float diff in rotationDiffs)
            {
                sum += diff;
                sumSquared += diff * diff;
            }

            float average = sum / count;

            return sumSquared / count - average * average;
        }
    }

    public DeviationCalculator(int windowLength = 100)
    {
        this.windowLength = windowLength;
    }

    public void Reset()
    {
        positions.Clear();
        rotationDiffs.Clear();
        lastRotation = Quaternion.identity;
        rotationInit = false;
    }

    public void UpdatePosition(Vector3 position)
    {
        positions.Enqueue(position * 100);

        if (positions.Count > windowLength)
        {
            positions.Dequeue();
        }
    }

    public void UpdateRotation(Quaternion rotation)
    {
        if (!rotationInit)
        {
            lastRotation = rotation;
            rotationDiffs.Enqueue(0);
            rotationInit = true;
            return;
        }

        float diff = Mathf.Abs(Quaternion.Angle(lastRotation, rotation));
        rotationDiffs.Enqueue(diff);

        if (rotationDiffs.Count > windowLength)
        {
            rotationDiffs.Dequeue();
        }
    }
}
