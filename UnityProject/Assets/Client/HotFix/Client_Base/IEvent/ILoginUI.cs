using TEngine;

namespace Client_Event
{
    [EventInterface(EEventGroup.GroupUI)]
    public interface ILoginUI
    {
        void ShowLoginUI();

        void CloseLoginUI();
    }
}