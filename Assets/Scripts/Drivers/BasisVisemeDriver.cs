using Basis.Scripts.BasisSdk;
using Basis.Scripts.LipSync.Scripts;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static Basis.Scripts.LipSync.Scripts.OVRLipSync;

namespace Basis.Scripts.Drivers
{
    public class BasisVisemeDriver : OVRLipSyncContextBase
    {
        public int laughterBlendTarget = -1;
        public float laughterThreshold = 0.5f;
        public float laughterMultiplier = 1.5f;
        public int smoothAmount = 70;
        public BasisAvatar Avatar;
        public float laughterScore = 0.0f;
        public float[] FinalBlendShapes;
        public bool[] HasViseme;
        public int BlendShapeCount;
        private ConcurrentQueue<float[]> audioQueue = new ConcurrentQueue<float[]>();
        private CancellationTokenSource cts = new CancellationTokenSource();
        public int TimingInMs = 10;//10ms
        public void Initialize(BasisAvatar avatar)
        {
            // Debug.Log("Initalizing " + nameof(BasisVisemeDriver));  
            Avatar = avatar;
            Smoothing = smoothAmount;
            BlendShapeCount = Avatar.FaceVisemeMovement.Length;
            FinalBlendShapes = new float[Enum.GetNames(typeof(Viseme)).Length];
            Array.Fill(FinalBlendShapes, -1);
            HasViseme = new bool[BlendShapeCount];
            for (int Index = 0; Index < BlendShapeCount; Index++)
            {
                if (Avatar.FaceVisemeMovement[Index] != -1)
                {
                    HasViseme[Index] = true;
                }
                else
                {
                    HasViseme[Index] = false;
                }
            }
            Task.Run(() => ProcessQueue(), cts.Token);
        }
        public void EventLateUpdate()
        {
            if (Avatar != null)
            {
                // get the current viseme frame
                OVRLipSync.Frame frame = GetCurrentPhonemeFrame();
                if (frame != null)
                {
                    for (int Index = 0; Index < BlendShapeCount; Index++)
                    {
                        if (HasViseme[Index])
                        {
                            // Viseme blend weights are in range of 0->1.0, we need to make range 100
                            if (FinalBlendShapes[Index] != frame.Visemes[Index])
                            {
                                float VisemeModified = frame.Visemes[Index] * 100.0f;
                                Avatar.FaceVisemeMesh.SetBlendShapeWeight(Avatar.FaceVisemeMovement[Index], VisemeModified);
                                FinalBlendShapes[Index] = frame.Visemes[Index];
                            }
                        }
                    }
                    if (laughterBlendTarget != -1)
                    {
                        // Laughter score will be raw classifier output in [0,1]
                        float laughterScore = frame.laughterScore;

                        // Threshold then re-map to [0,1]
                        laughterScore = laughterScore < laughterThreshold ? 0.0f : laughterScore - laughterThreshold;
                        laughterScore = Mathf.Min(laughterScore * laughterMultiplier, 1.0f);
                        laughterScore *= 1.0f / laughterThreshold;

                        Avatar.FaceVisemeMesh.SetBlendShapeWeight(laughterBlendTarget, laughterScore * 100.0f);
                    }
                }
                else
                {
                    Debug.Log("missing frame");
                }
                laughterScore = this.Frame.laughterScore;
                // Update smoothing value
                if (smoothAmount != Smoothing)
                {
                    Smoothing = smoothAmount;
                }
            }
        }
        private void OnDestroy()
        {
            // Cancel the background processing thread when the object is destroyed
            cts.Cancel();
        }
        /// <summary>
        /// data is coming from the audio thread
        /// place that data into a seperate thread.
        /// this way we dont bog down the audio thread.
        /// </summary>
        /// <param name="data"></param>
        public void ProcessAudioSamples(float[] data)
        {
            // Enqueue audio data for processing
            audioQueue.Enqueue(data);
        }

        public void ProcessQueue()
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (audioQueue.TryDequeue(out float[] data))
                {
                    ProcessData(data);
                }
                else
                {
                    // Sleep briefly to avoid busy-waiting
                    Thread.Sleep(TimingInMs);
                }
            }
        }

        public void ProcessData(float[] data)
        {
            // Process data in a thread-safe manner
            lock (this)
            {
                if (Context == 0 || OVRLipSync.IsInitialized() != OVRLipSync.Result.Success)
                {
                    return;
                }
                OVRLipSync.Frame frame = this.Frame;
                OVRLipSync.ProcessFrame(Context, data, frame, false);
            }

        }
    }
}