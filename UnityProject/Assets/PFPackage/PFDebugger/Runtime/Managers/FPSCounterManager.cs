using UnityEngine;

namespace PFDebugger
{
    [SubManager()]
    public class FPSCounterManager : SubManagerBase
    {
        private float updateInterval = 0.5f;
        private float currentFps;
        private int frames;
        private float accumulator;
        private float timeLeft;

        public float CurrentFps => currentFps;
        
        public float UpdateInterval
        {
            get => updateInterval;
            set
            {
                if (value <= 0f)
                {
                    Debug.LogError("Update interval is invalid");
                    return;
                }
                
                updateInterval = value;
                Reset();
            }
        }
        
        public override void Init()
        {
            Reset();
        }

        public override void Tick(float elapseSeconds, float realElapseSeconds)
        {
            frames++;
            accumulator += realElapseSeconds;
            timeLeft -= realElapseSeconds;

            if (timeLeft <= 0f)
            {
                currentFps = accumulator > 0f ? frames / accumulator : 0f;
                frames = 0;
                accumulator = 0f;
                timeLeft += updateInterval;
            }
        }


        private void Reset()
        {
            currentFps = 0f;
            frames = 0;
            accumulator = 0f;
            timeLeft = 0f;
        }
    }
}