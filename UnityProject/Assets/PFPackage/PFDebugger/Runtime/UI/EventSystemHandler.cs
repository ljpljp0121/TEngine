using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

namespace PFDebugger
{
    [DefaultExecutionOrder(1000)]
    public class EventSystemHandler : MonoBehaviour
    {
        [SerializeField] private GameObject embeddedEventSystem;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        private void Awake()
        {
            if (embeddedEventSystem == null)
                return;

            StandaloneInputModule legacyInputModule = embeddedEventSystem.GetComponent<StandaloneInputModule>();
            if (legacyInputModule == null)
                return;

            DestroyImmediate(legacyInputModule);
            embeddedEventSystem.AddComponent<InputSystemUIInputModule>();
        }
#endif

        private void OnEnable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            ActivateEventSystemIfNeeded();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;

            DeactivateEventSystem();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DeactivateEventSystem();
            ActivateEventSystemIfNeeded();
        }

        private void OnSceneUnloaded(Scene current)
        {
            DeactivateEventSystem();
        }

        private void ActivateEventSystemIfNeeded()
        {
            if (embeddedEventSystem != null && EventSystem.current == null)
                embeddedEventSystem.SetActive(true);
        }

        private void DeactivateEventSystem()
        {
            if (embeddedEventSystem != null)
                embeddedEventSystem.SetActive(false);
        }
    }
}
