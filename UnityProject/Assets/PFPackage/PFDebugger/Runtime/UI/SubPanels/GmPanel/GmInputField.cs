using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PFDebugger
{
    public class GmInputField : InputField
    {
        private const EventModifiers BlockingModifiers =
            EventModifiers.Shift |
            EventModifiers.Control |
            EventModifiers.Alt |
            EventModifiers.Command;

        private readonly Event processingEvent = new Event();

        public event Action NavigateUpRequested;
        public event Action NavigateDownRequested;
        public event Action CompleteRequested;

        public bool UseSuggestionNavigation { get; set; }

        public override void OnUpdateSelected(BaseEventData eventData)
        {
            if (!UseSuggestionNavigation)
            {
                base.OnUpdateSelected(eventData);
                return;
            }

            if (!isFocused)
                return;

            bool consumedEvent = false;

            while (Event.PopEvent(processingEvent))
            {
                switch (processingEvent.rawType)
                {
                    case EventType.KeyUp:
                        break;

                    case EventType.KeyDown:
                        consumedEvent = true;

                        if (TryHandleSuggestionNavigation(processingEvent))
                            break;

                        if (Input.compositionString.Length > 0 &&
                            processingEvent.character == 0 &&
                            processingEvent.modifiers == EventModifiers.None)
                        {
                            break;
                        }

                        if (processingEvent.keyCode == KeyCode.Return || processingEvent.keyCode == KeyCode.KeypadEnter)
                        {
                            if (lineType != LineType.MultiLineNewline)
                            {
                                SendOnSubmit();
                                DeactivateInputField();
                                break;
                            }
                        }
                        else if (processingEvent.keyCode == KeyCode.Escape)
                        {
                            ProcessEvent(processingEvent);
                            DeactivateInputField();
                            break;
                        }

                        ProcessEvent(processingEvent);
                        UpdateLabel();
                        break;

                    case EventType.ValidateCommand:
                    case EventType.ExecuteCommand:
                        if (processingEvent.commandName == "SelectAll")
                        {
                            SelectAll();
                            consumedEvent = true;
                        }
                        break;
                }
            }

            if (consumedEvent)
                UpdateLabel();

            eventData.Use();
        }

        private bool TryHandleSuggestionNavigation(Event inputEvent)
        {
            if ((inputEvent.modifiers & BlockingModifiers) != 0)
                return false;

            switch (inputEvent.keyCode)
            {
                case KeyCode.UpArrow:
                    NavigateUpRequested?.Invoke();
                    return true;

                case KeyCode.DownArrow:
                    NavigateDownRequested?.Invoke();
                    return true;

                case KeyCode.Tab:
                    CompleteRequested?.Invoke();
                    return true;

                default:
                    return false;
            }
        }
    }
}
