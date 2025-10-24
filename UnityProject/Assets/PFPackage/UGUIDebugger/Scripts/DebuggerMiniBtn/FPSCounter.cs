using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PFDebugger
{
    internal class FPSCounter
    {
        private float updateInterval;
        private float currentFps;
        private int frames;
        private float accumulator;
        private float timeLeft;

        public FPSCounter(float updateInterval)
        {
            if (updateInterval <= 0f)
            {
                Debug.LogError("Update interval is invalid.");
                return;
            }

            this.updateInterval = updateInterval;
            Reset();
        }

        public float UpdateInterval
        {
            get => updateInterval;
            set
            {
                if (value <= 0f)
                {
                    Debug.LogError("Update interval is invalid.");
                    return;
                }

                updateInterval = value;
                Reset();
            }
        }

        public float CurrentFps => currentFps;

        public void Update(float elapseSeconds, float realElapseSeconds)
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