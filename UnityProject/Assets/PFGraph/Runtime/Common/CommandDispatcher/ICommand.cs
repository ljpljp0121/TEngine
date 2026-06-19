namespace PFGraph
{
    public interface ICommand
    {
        void Do();

        void Redo();

        void Undo();
    }
}