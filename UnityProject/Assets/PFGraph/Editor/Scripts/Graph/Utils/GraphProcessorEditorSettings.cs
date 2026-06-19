using System.Runtime.CompilerServices;

namespace PFGraph
{
    public class GraphProcessorEditorSettings
    {
        public static EditorPrefsVariable<bool> MiniMapActive { get; } = new EditorPrefsVariable<bool>(GetKey());
        public static EditorPrefsVariable<bool> GridSnapActive { get; } = new EditorPrefsVariable<bool>(GetKey(), true);
        public static EditorPrefsVariable<int> GridSnapSize { get; } = new EditorPrefsVariable<int>(GetKey(), 16);
        public static EditorPrefsVariable<bool> InspectorActive { get; } =
            new EditorPrefsVariable<bool>(GetKey(), true);
        public static EditorPrefsVariable<bool> ItemLibraryActive { get; } =
            new EditorPrefsVariable<bool>(GetKey(), false);
        public static EditorPrefsVariable<int> CommandHistoryLimit { get; } =
            new EditorPrefsVariable<int>(GetKey(), 200);

        public static string GetKey([CallerMemberName] string memberName = null)
        {
            return $"{nameof(GraphProcessorEditorSettings)}.{memberName}";
        }
    }
}