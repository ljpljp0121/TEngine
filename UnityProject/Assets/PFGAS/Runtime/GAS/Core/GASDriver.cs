using UnityEngine;

namespace PFGAS.Runtime
{
    /// <summary>
    /// 将 Unity Update 转发给全局 GAS 调度器的场景驱动组件。
    /// </summary>
    public class GASDriver : MonoBehaviour
    {
        private GAS gas => GAS.I;

        private void Update()
        {
            gas.Tick(Time.deltaTime,Time.unscaledDeltaTime);
        }
    }
}
