namespace PFGAS.Runtime
{
    public sealed partial class AttributeGraph
    {
        private bool HasChangeListeners => AttributeChanged != null || AttributesChanged != null;

        private void TrackOriginalValue(AttributeNode node, bool enabled)
        {
            if (!enabled || originalChangedValues.ContainsKey(node.Id))
            {
                return;
            }

            originalChangedValues.Add(node.Id, node.Value);
        }

        private void RemoveTrackedChange(PFAttributeId attributeId)
        {
            originalChangedValues.Remove(attributeId);
        }

        private AttributeChange[] CollectAttributeChanges()
        {
            if (originalChangedValues.Count == 0)
            {
                return System.Array.Empty<AttributeChange>();
            }

            reusableChanges.Clear();
            foreach (var pair in originalChangedValues)
            {
                if (!nodes.TryGetValue(pair.Key, out var node))
                {
                    continue;
                }

                var oldValue = pair.Value;
                var newValue = node.Value;
                if (PFGASHelper.IsNearlyEqual(oldValue.BaseValue, newValue.BaseValue) &&
                    PFGASHelper.IsNearlyEqual(oldValue.CurrentValue, newValue.CurrentValue))
                {
                    continue;
                }

                reusableChanges.Add(new AttributeChange(
                    pair.Key,
                    oldValue.BaseValue,
                    newValue.BaseValue,
                    oldValue.CurrentValue,
                    newValue.CurrentValue));
            }

            originalChangedValues.Clear();
            if (reusableChanges.Count == 0)
            {
                return System.Array.Empty<AttributeChange>();
            }

            var changes = reusableChanges.ToArray();
            reusableChanges.Clear();
            return changes;
        }

        private void PublishAttributeChanges()
        {
            PublishAttributeChanges(CollectAttributeChanges());
        }

        private void PublishAttributeChanges(AttributeChange[] changes)
        {
            if (changes == null || changes.Length == 0)
            {
                return;
            }

            isPublishingChanges = true;
            try
            {
                if (AttributesChanged != null)
                {
                    AttributesChanged(changes);
                }

                if (AttributeChanged != null)
                {
                    for (var i = 0; i < changes.Length; i++)
                    {
                        AttributeChanged(changes[i]);
                    }
                }
            }
            finally
            {
                isPublishingChanges = false;
            }
        }
    }
}
