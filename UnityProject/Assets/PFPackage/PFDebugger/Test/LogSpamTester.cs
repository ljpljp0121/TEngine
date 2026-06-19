using System.Collections;
using UnityEngine;

namespace PFDebugger
{
    public class LogSpamTester : MonoBehaviour
    {
        [Header("Auto Spam")]
        [SerializeField] private bool autoStart = true;
        [SerializeField] private float intervalSeconds = 0.05f;
        [SerializeField] private int logsPerTick = 20;
        [SerializeField] private int maxLogs = 0;

        [Header("Message")]
        [SerializeField] private string prefix = "[LogSpamTester]";
        [SerializeField] private bool uniqueMessage = true;
        [SerializeField] private int extraMessageLength = 0;

        [Header("Type")]
        [SerializeField] private bool randomType = true;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode toggleSpamKey = KeyCode.F8;
        [SerializeField] private KeyCode burstKey = KeyCode.F9;
        [SerializeField] private int burstCount = 300;

        private Coroutine spamCoroutine;
        private int sequence;
        private string cachedExtraMessage;
        private int cachedExtraMessageLength = -1;

        private void OnValidate()
        {
            intervalSeconds = Mathf.Max(0f, intervalSeconds);
            logsPerTick = Mathf.Max(1, logsPerTick);
            maxLogs = Mathf.Max(0, maxLogs);
            burstCount = Mathf.Max(1, burstCount);
            extraMessageLength = Mathf.Max(0, extraMessageLength);
        }

        private void Start()
        {
            if (autoStart)
                StartSpam();
        }

        private void OnDisable()
        {
            StopSpam();
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleSpamKey))
            {
                if (spamCoroutine == null)
                    StartSpam();
                else
                    StopSpam();
            }

            if (Input.GetKeyDown(burstKey))
                EmitBurst(burstCount);
        }

        [ContextMenu("Start Spam")]
        public void StartSpam()
        {
            if (spamCoroutine != null)
                return;

            spamCoroutine = StartCoroutine(SpamLoop());
        }

        [ContextMenu("Stop Spam")]
        public void StopSpam()
        {
            if (spamCoroutine == null)
                return;

            StopCoroutine(spamCoroutine);
            spamCoroutine = null;
        }

        [ContextMenu("Emit Burst")]
        public void EmitBurst()
        {
            EmitBurst(burstCount);
        }

        public void EmitBurst(int count)
        {
            count = Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                EmitOneLog(i);
                if (maxLogs > 0 && sequence >= maxLogs)
                {
                    StopSpam();
                    break;
                }
            }
        }

        private IEnumerator SpamLoop()
        {
            while (true)
            {
                for (int i = 0; i < logsPerTick; i++)
                {
                    EmitOneLog(i);
                    if (maxLogs > 0 && sequence >= maxLogs)
                    {
                        StopSpam();
                        yield break;
                    }
                }

                if (intervalSeconds <= 0f)
                    yield return null;
                else
                    yield return new WaitForSeconds(intervalSeconds);
            }
        }

        private void EmitOneLog(int localIndex)
        {
            sequence++;

            LogType type = PickType(localIndex);
            string message = BuildMessage(localIndex);

            switch (type)
            {
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogType.Error:
                case LogType.Assert:
                case LogType.Exception:
                    Debug.LogError(message);
                    break;
                default:
                    Debug.Log(message);
                    break;
            }
        }

        private LogType PickType(int localIndex)
        {
            if (randomType)
            {
                int random = Random.Range(0, 3);
                if (random == 0) return LogType.Log;
                if (random == 1) return LogType.Warning;
                return LogType.Error;
            }

            int mode = (sequence + localIndex) % 3;
            if (mode == 0) return LogType.Log;
            if (mode == 1) return LogType.Warning;
            return LogType.Error;
        }

        private string BuildMessage(int localIndex)
        {
            if (!uniqueMessage)
                return $"{prefix} repeated message{GetExtraMessage()}";

            return $"{prefix} seq={sequence} frame={Time.frameCount} tickIndex={localIndex}{GetExtraMessage()}";
        }

        private string GetExtraMessage()
        {
            if (extraMessageLength <= 0)
                return string.Empty;

            if (cachedExtraMessageLength != extraMessageLength || cachedExtraMessage == null)
            {
                cachedExtraMessageLength = extraMessageLength;
                cachedExtraMessage = new string('X', extraMessageLength);
            }

            return cachedExtraMessage;
        }
    }
}
