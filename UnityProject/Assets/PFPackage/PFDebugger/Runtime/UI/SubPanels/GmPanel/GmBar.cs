using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PFDebugger
{
    public class GmBar : MonoBehaviour
    {
        [SerializeField] private GmInputField inputField;
        [SerializeField] private Button executeButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private RectTransform suggestionRoot;
        [SerializeField] private GmSuggestionItem suggestionItemPrefab;

        private readonly List<GmCommandInfo> suggestionBuffer = new List<GmCommandInfo>(16);
        private readonly List<GmSuggestionItem> activeItems = new List<GmSuggestionItem>(16);
        private readonly Queue<GmSuggestionItem> itemPool = new Queue<GmSuggestionItem>(16);

        private GmManager gmManager;
        private int selectedSuggestionIndex = -1;
        private Coroutine reactivateInputCoroutine;

        public void OnInitBar()
        {
            gmManager = Debugger.GmManager;
            RegisterEvents();
            SetSuggestionVisible(false);
        }

        public void OnDeinitBar()
        {
            UnregisterEvents();
            ClearSuggestionItems();
        }

        public void OnBarShow()
        {
            RefreshSuggestions(inputField != null ? inputField.text : string.Empty);
        }

        public void OnBarHide()
        {
            SetSuggestionVisible(false);
        }

        public void RefreshSuggestions(string input)
        {
            suggestionBuffer.Clear();
            gmManager.GetSuggestions(input, suggestionBuffer);

            RebuildSuggestionItems();

            bool showSuggestions = !string.IsNullOrWhiteSpace(input) && suggestionBuffer.Count > 0;
            SetSuggestionVisible(showSuggestions);
            inputField.UseSuggestionNavigation = showSuggestions;

            selectedSuggestionIndex = showSuggestions ? 0 : -1;
            RefreshSelectionVisuals();
        }

        private void RegisterEvents()
        {
            inputField.onValueChanged.AddListener(OnInputValueChanged);
            inputField.onSubmit.AddListener(OnInputSubmit);
            inputField.NavigateUpRequested += OnNavigateUpRequested;
            inputField.NavigateDownRequested += OnNavigateDownRequested;
            inputField.CompleteRequested += OnCompleteRequested;
            executeButton.onClick.AddListener(ExecuteCurrentInput);
            clearButton.onClick.AddListener(ClearInput);
        }

        private void UnregisterEvents()
        {
            inputField.onValueChanged.RemoveListener(OnInputValueChanged);
            inputField.onSubmit.RemoveListener(OnInputSubmit);
            inputField.NavigateUpRequested -= OnNavigateUpRequested;
            inputField.NavigateDownRequested -= OnNavigateDownRequested;
            inputField.CompleteRequested -= OnCompleteRequested;
            executeButton.onClick.RemoveListener(ExecuteCurrentInput);
            clearButton.onClick.RemoveListener(ClearInput);
        }

        private void OnInputValueChanged(string input)
        {
            RefreshSuggestions(input);
        }

        private void OnInputSubmit(string input)
        {
            ExecuteCurrentInput();
        }

        private void OnNavigateUpRequested()
        {
            MoveSelection(-1);
        }

        private void OnNavigateDownRequested()
        {
            MoveSelection(1);
        }

        private void OnCompleteRequested()
        {
            CompleteSelectedSuggestion();
        }

        private void ExecuteCurrentInput()
        {
            string command = inputField.text;
            if (string.IsNullOrWhiteSpace(command))
            {
                SetSuggestionVisible(false);
                ReactivateInputFieldNextFrame();
                return;
            }

            GmCommandExecutionResult result = gmManager.ExecuteCommand(command);
            if (result.Success)
            {
                ClearInput(keepFocus: true);
                return;
            }

            RefreshSuggestions(command);
            ReactivateInputFieldNextFrame();
        }

        private void ClearInput()
        {
            ClearInput(false);
        }

        private void ClearInput(bool keepFocus)
        {
            if (inputField == null)
                return;

            inputField.SetTextWithoutNotify(string.Empty);
            SetSuggestionVisible(false);
            selectedSuggestionIndex = -1;
            RefreshSelectionVisuals();

            if (keepFocus)
                ReactivateInputFieldNextFrame();
        }

        private void OnSuggestionSelected(GmCommandInfo commandInfo)
        {
            string completedText = commandInfo.Command;
            if (commandInfo.Parameters.Count > 0)
                completedText += " ";

            inputField.SetTextWithoutNotify(completedText);
            inputField.caretPosition = completedText.Length;
            selectedSuggestionIndex = -1;
            RefreshSuggestions(completedText);
            RefreshSelectionVisuals();
            inputField.ActivateInputField();
        }

        private void RebuildSuggestionItems()
        {
            ClearSuggestionItems();

            if (suggestionItemPrefab == null || suggestionRoot == null)
                return;

            for (int i = 0; i < suggestionBuffer.Count; i++)
            {
                GmSuggestionItem item = GetOrCreateItem();
                item.transform.SetParent(suggestionRoot, false);
                item.transform.SetSiblingIndex(i);
                item.gameObject.SetActive(true);
                item.Bind(suggestionBuffer[i], OnSuggestionSelected);
                activeItems.Add(item);
            }
        }

        private GmSuggestionItem GetOrCreateItem()
        {
            if (itemPool.Count > 0)
                return itemPool.Dequeue();

            return Instantiate(suggestionItemPrefab, suggestionRoot);
        }

        private void ClearSuggestionItems()
        {
            for (int i = 0; i < activeItems.Count; i++)
            {
                GmSuggestionItem item = activeItems[i];
                if (item == null)
                    continue;

                item.ResetItem();
                item.gameObject.SetActive(false);
                itemPool.Enqueue(item);
            }

            activeItems.Clear();
        }

        private void MoveSelection(int direction)
        {
            if (activeItems.Count == 0)
                return;

            if (selectedSuggestionIndex < 0)
                selectedSuggestionIndex = 0;
            else
                selectedSuggestionIndex = Mathf.Clamp(selectedSuggestionIndex + direction, 0, activeItems.Count - 1);

            RefreshSelectionVisuals();
        }

        private void CompleteSelectedSuggestion()
        {
            if (selectedSuggestionIndex < 0 || selectedSuggestionIndex >= activeItems.Count)
                return;

            OnSuggestionSelected(activeItems[selectedSuggestionIndex].CommandInfo);
        }

        private void RefreshSelectionVisuals()
        {
            for (int i = 0; i < activeItems.Count; i++)
                activeItems[i].SetSelected(i == selectedSuggestionIndex);
        }

        private void SetSuggestionVisible(bool visible)
        {
            if (suggestionRoot != null && suggestionRoot.gameObject.activeSelf != visible)
                suggestionRoot.gameObject.SetActive(visible);
        }

        private void ReactivateInputFieldNextFrame()
        {
            if (!isActiveAndEnabled || inputField == null)
                return;

            if (reactivateInputCoroutine != null)
                StopCoroutine(reactivateInputCoroutine);

            reactivateInputCoroutine = StartCoroutine(CoReactivateInputFieldNextFrame());
        }

        private System.Collections.IEnumerator CoReactivateInputFieldNextFrame()
        {
            yield return null;

            if (inputField == null || !isActiveAndEnabled)
            {
                reactivateInputCoroutine = null;
                yield break;
            }

            inputField.ActivateInputField();
            inputField.MoveTextEnd(false);
            reactivateInputCoroutine = null;
        }
    }
}
