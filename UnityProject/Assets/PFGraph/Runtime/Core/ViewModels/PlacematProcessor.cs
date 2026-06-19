using System;

namespace PFGraph
{
    [ViewModel(typeof(PlacematData))]
    public class PlacematProcessor : ViewModel, IGraphElementProcessor, IGraphElementProcessor_Scope
    {
        private readonly PlacematData model;
        private readonly Type modelType;
        private BaseGraphProcessor owner;

        public PlacematData Model => model;
        object IGraphElementProcessor.Model => model;
        public Type ModelType => modelType;
        Type IGraphElementProcessor.ModelType => modelType;

        public BaseGraphProcessor Owner
        {
            get => owner;
            internal set => owner = value;
        }

        public long ID => model.id;

        public string Title
        {
            get => model.title;
            set => SetFieldValue(ref model.title, value, nameof(PlacematData.title));
        }

        public InternalVector2Int Position
        {
            get => model.position;
            set => SetFieldValue(ref model.position, value, nameof(PlacematData.position));
        }

        public InternalVector2Int Size
        {
            get => model.size;
            set => SetFieldValue(ref model.size, value, nameof(PlacematData.size));
        }

        public InternalColor Color
        {
            get => model.color;
            set => SetFieldValue(ref model.color, value, nameof(PlacematData.color));
        }

        public PlacematProcessor(PlacematData model)
        {
            this.model = model;
            modelType = model.GetType();
            this.model.position = model.position == default ? InternalVector2Int.zero : model.position;
            this.model.size = model.size == default ? new InternalVector2Int(420, 260) : model.size;
        }
    }
}