using System.Collections;

namespace PFGraph
{
    public partial class EditorCoroutineService : CoroutineService<EditorCoroutine>
    {
        public override EditorCoroutine StartCoroutine(IEnumerator enumerator)
        {
            EditorCoroutine coroutine = new EditorCoroutine(enumerator);
            coroutineQueue.Enqueue(coroutine);
            return coroutine;
        }

        public override void StopCoroutine(EditorCoroutine coroutine)
        {
            coroutine.Stop();
        }
    }
}