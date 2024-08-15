using Basis.Scripts.Drivers;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(15001)]
public partial class BasisMuscleDriver : BasisBaseMuscleDriver
{
    [SerializeField]
    public PoseData RestingOnePoseData;
    [SerializeField]
    public PoseData CurrentOnPoseData;
    public void Initialize(BasisLocalAvatarDriver basisLocalAvatarDriver, Animator animator)
    {
        Animator = animator;
        BasisLocalAvatarDriver = basisLocalAvatarDriver;
        // Initialize the HumanPoseHandler with the animator's avatar and transform
        poseHandler = new HumanPoseHandler(Animator.avatar, Animator.transform);
        // Initialize the HumanPose
        pose = new HumanPose();

        SetMusclesAndRecordPoses();
        MuscleNames = HumanTrait.MuscleName;

    }
    public float increment = 0.2f;
    [SerializeField]
    [System.Serializable]
    public struct PoseDataAdditional
    {
        [SerializeField]
        public PoseData PoseData;
        public Vector2 Coord;
    }
    public void LoadAllPoints()
    {
        CoordToPose.Clear();
        // Define the corners
        Vector2 TopLeft = new Vector2(-1f, 1f);
        Vector2 TopRight = new Vector2(1f, 1f);
        Vector2 BottomLeft = new Vector2(-1f, -1f);
        Vector2 BottomRight = new Vector2(1f, -1f);

        // List to hold all PoseData points
        List<PoseDataAdditional> points = new List<PoseDataAdditional>();

        // Loop through the square grid using the increment
        for (float x = BottomLeft.x; x <= BottomRight.x; x += increment)
        {
            for (float y = BottomLeft.y; y <= TopLeft.y; y += increment)
            {
                PoseData poseData = new PoseData();

                // Set and record pose based on x and y coordinates
                SetAndRecordPose(x, ref poseData, y);

                // Add the poseData to the list
                PoseDataAdditional poseadd = new PoseDataAdditional();
                poseadd.PoseData = poseData;
                poseadd.Coord = new Vector2(x, y);
                points.Add(poseadd);
            }
        }

        // Optionally, handle the situation where increment doesn't land exactly on the corner points
        PoseData topLeftPose = new PoseData();
        SetAndRecordPose(TopLeft.x, ref topLeftPose, TopLeft.y);
        // Add the poseData to the list
        PoseDataAdditional poseDataAdditional = new PoseDataAdditional();
        poseDataAdditional.PoseData = topLeftPose;
        poseDataAdditional.Coord = TopLeft;
        points.Add(poseDataAdditional);

        PoseData topRightPose = new PoseData();
        SetAndRecordPose(TopRight.x, ref topRightPose, TopRight.y);
        // Add the poseData to the list
        poseDataAdditional = new PoseDataAdditional();
        poseDataAdditional.PoseData = topRightPose;
        poseDataAdditional.Coord = TopRight;
        points.Add(poseDataAdditional);

        PoseData bottomLeftPose = new PoseData();
        SetAndRecordPose(BottomLeft.x, ref bottomLeftPose, BottomLeft.y);
        // Add the poseData to the list
        poseDataAdditional = new PoseDataAdditional();
        poseDataAdditional.PoseData = bottomLeftPose;
        poseDataAdditional.Coord = BottomLeft;
        points.Add(poseDataAdditional);

        PoseData bottomRightPose = new PoseData();
        SetAndRecordPose(BottomRight.x, ref bottomRightPose, BottomRight.y);
        // Add the poseData to the list
        poseDataAdditional = new PoseDataAdditional();
        poseDataAdditional.PoseData = bottomRightPose;
        poseDataAdditional.Coord = BottomRight;
        points.Add(poseDataAdditional);
        foreach (var point in points)
        {
            CoordToPose.TryAdd(point.Coord, point);
        }
        // Cache dictionary keys for faster access
        coordKeys = new Vector2[CoordToPose.Count];
        CoordToPose.Keys.CopyTo(coordKeys, 0);
    }
    public void SetMusclesAndRecordPoses()
    {
        BoxedPoseData = new SquarePoseData();
        // Get the current human pose
        poseHandler.GetHumanPose(ref pose);
        LoadMuscleData();

        RecordCurrentPose(ref RestingOnePoseData);
        RecordCurrentPose(ref CurrentOnPoseData);

        LoadAllPoints();
    }
    public void LoadMuscleData()
    {
        // Assign muscle indices to each finger array using Array.Copy
        LeftThumb = new float[4];
        System.Array.Copy(pose.muscles, 55, LeftThumb, 0, 4);
        LeftIndex = new float[4];
        System.Array.Copy(pose.muscles, 59, LeftIndex, 0, 4);
        LeftMiddle = new float[4];
        System.Array.Copy(pose.muscles, 63, LeftMiddle, 0, 4);
        LeftRing = new float[4];
        System.Array.Copy(pose.muscles, 67, LeftRing, 0, 4);
        LeftLittle = new float[4];
        System.Array.Copy(pose.muscles, 71, LeftLittle, 0, 4);

        RightThumb = new float[4];
        System.Array.Copy(pose.muscles, 75, RightThumb, 0, 4);
        RightIndex = new float[4];
        System.Array.Copy(pose.muscles, 79, RightIndex, 0, 4);
        RightMiddle = new float[4];
        System.Array.Copy(pose.muscles, 83, RightMiddle, 0, 4);
        RightRing = new float[4];
        System.Array.Copy(pose.muscles, 87, RightRing, 0, 4);
        RightLittle = new float[4];
        System.Array.Copy(pose.muscles, 91, RightLittle, 0, 4);
    }
    public void LateUpdate()
    {
        UpdateAllFingers(BasisLocalAvatarDriver.References, ref CurrentOnPoseData);
    }
}