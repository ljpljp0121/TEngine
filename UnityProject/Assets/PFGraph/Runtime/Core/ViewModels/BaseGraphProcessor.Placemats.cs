using System.Collections.Generic;

namespace PFGraph
{
    public partial class BaseGraphProcessor
    {
        private Dictionary<long, PlacematProcessor> placemats;

        public IReadOnlyDictionary<long, PlacematProcessor> Placemats => placemats;

        private void InitPlacemats()
        {
            if (model.placemats == null)
                model.placemats = new List<PlacematData>();

            placemats = new Dictionary<long, PlacematProcessor>(model.placemats.Count);
            for (var i = 0; i < model.placemats.Count; i++)
            {
                var placemat = model.placemats[i];
                if (placemat == null)
                {
                    ReportDiagnostic($"[MissingPlacemat] Null placemat at index {i} removed.");
                    model.placemats.RemoveAt(i--);
                    continue;
                }

                if (placemats.ContainsKey(placemat.id))
                {
                    ReportDiagnostic($"[DuplicatePlacemat] Placemat id={placemat.id} duplicated, later entry removed.");
                    model.placemats.RemoveAt(i--);
                    continue;
                }

                var vm = ViewModelFactory.ProduceViewModel(placemat) as PlacematProcessor;
                vm.Owner = this;
                placemats.Add(vm.ID, vm);
            }
        }

        public PlacematProcessor NewPlacemat(InternalVector2Int position)
        {
            var data = new PlacematData()
            {
                id = GraphProcessorUtil.GenerateId(),
                position = position,
            };
            return ViewModelFactory.ProduceViewModel(data) as PlacematProcessor;
        }

        public void AddPlacemat(PlacematProcessor placemat)
        {
            if (placemat == null || placemats.ContainsKey(placemat.ID))
                return;

            placemat.Owner = this;
            placemats.Add(placemat.ID, placemat);
            model.placemats.Add(placemat.Model);
            graphEvents.Publish(new AddPlacematEventArgs(placemat));
        }

        public void RemovePlacemat(long id)
        {
            if (!placemats.TryGetValue(id, out var placemat))
                return;

            placemats.Remove(id);
            model.placemats.Remove(placemat.Model);
            graphEvents.Publish(new RemovePlacematEventArgs(placemat));
        }
    }
}