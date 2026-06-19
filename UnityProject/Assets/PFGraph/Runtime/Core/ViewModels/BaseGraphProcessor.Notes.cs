using System.Collections.Generic;

namespace PFGraph
{
    public partial class BaseGraphProcessor
    {
        private Dictionary<long, StickyNoteProcessor> notes;

        public IReadOnlyDictionary<long, StickyNoteProcessor> Notes => notes;

        private void InitNotes()
        {
            this.notes = new Dictionary<long, StickyNoteProcessor>(System.Math.Min(model.notes.Count, 4));
            foreach (var note in model.notes)
            {
                notes.Add(note.id, (StickyNoteProcessor)ViewModelFactory.ProduceViewModel(note));
            }
        }

        #region API

        public void AddNote(string title, string content, InternalVector2Int position)
        {
            var note = new StickyNote();
            note.id = GraphProcessorUtil.GenerateId();
            note.position = position;
            note.title = title;
            note.content = content;
            AddNote(ViewModelFactory.ProduceViewModel(note) as StickyNoteProcessor);
        }

        public void AddNote(StickyNoteProcessor note)
        {
            notes.Add(note.ID, note);
            model.notes.Add(note.Model);
            graphEvents.Publish(new AddNoteEventArgs(note));
        }

        public void RemoveNote(long id)
        {
            if (!notes.TryGetValue(id, out var note))
                return;
            notes.Remove(note.ID);
            model.notes.Remove(note.Model);
            graphEvents.Publish(new RemoveNoteEventArgs(note));
        }

        #endregion
    }
}