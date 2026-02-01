using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Xunit;
using Verifier =
    Microsoft.CodeAnalysis.CSharp.Testing.XUnit.AnalyzerVerifier<
        UnityCodeRule.UnityUpdateHeapAllocationAnalyzer>;

namespace UnityCodeRule.Tests;

public class UnityUpdateHeapAllocationAnalyzerTests
{
    [Fact]
    public async Task ClassAllocationInMonoBehaviourUpdate_ShouldReportDiagnostic()
    {
        const string text = @"
using System.Collections.Generic;

namespace UnityEngine
{
    public class MonoBehaviour {}
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    private void Update()
    {
        var list = [|new List<int>()|];
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task StructAllocationInMonoBehaviourUpdate_ShouldNotReportDiagnostic()
    {
        const string text = @"
namespace UnityEngine
{
    public class MonoBehaviour {}

    public struct Vector3
    {
        public Vector3(float x, float y, float z) {}
    }
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    private void Update()
    {
        var position = new UnityEngine.Vector3(0, 0, 0);
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task ClassAllocationInStartMethod_ShouldNotReportDiagnostic()
    {
        const string text = @"
using System.Collections.Generic;

namespace UnityEngine
{
    public class MonoBehaviour {}
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    private void Start()
    {
        var list = new List<int>();
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task ClassAllocationInNonMonoBehaviourClass_ShouldNotReportDiagnostic()
    {
        const string text = @"
using System.Collections.Generic;

public class PlayerController
{
    private void Update()
    {
        var list = new List<int>();
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task ClassAllocationInLateUpdate_ShouldNotReportDiagnostic()
    {
        const string text = @"
using System.Collections.Generic;

namespace UnityEngine
{
    public class MonoBehaviour {}
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    private void LateUpdate()
    {
        var list = new List<int>();
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task MultipleClassAllocationsInUpdate_ShouldReportMultipleDiagnostics()
    {
        const string text = @"
using System.Collections.Generic;

namespace UnityEngine
{
    public class MonoBehaviour {}
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    private void Update()
    {
        var list1 = [|new List<int>()|];
        var list2 = [|new List<string>()|];
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task StringAllocationInUpdate_ShouldReportDiagnostic()
    {
        const string text = @"
namespace UnityEngine
{
    public class MonoBehaviour {}
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    private void Update()
    {
        var message = [|new string('a', 10)|];
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task CustomClassAllocationInUpdate_ShouldReportDiagnostic()
    {
        const string text = @"
namespace UnityEngine
{
    public class MonoBehaviour {}
}

public class PlayerController : UnityEngine.MonoBehaviour
{
    private void Update()
    {
        var data = [|new PlayerData()|];
    }
}

public class PlayerData
{
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task IndirectAllocationInCalledMethod_ShouldReportDiagnostic()
    {
        const string text = @"
namespace UnityEngine { public class MonoBehaviour {} }

public class Test : UnityEngine.MonoBehaviour
{
    private void Update()
    {
        OnUpdate();
    }

    private void OnUpdate()
    {
        var obj = [|new System.Collections.Generic.List<int>()|];
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task DeepCallChain_ShouldReportDiagnostic()
    {
        const string text = @"
namespace UnityEngine { public class MonoBehaviour {} }

public class Test : UnityEngine.MonoBehaviour
{
    private void Update()
    {
        MethodA();
    }

    private void MethodA()
    {
        MethodB();
    }

    private void MethodB()
    {
        var obj = [|new System.Collections.Generic.List<int>()|];
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task AllocationInMethodNotCalledByUpdate_ShouldNotReportDiagnostic()
    {
        const string text = @"
using System.Collections.Generic;

namespace UnityEngine { public class MonoBehaviour {} }

public class Test : UnityEngine.MonoBehaviour
{
    private void Update()
    {
        // Update 不调用 OtherMethod
    }

    private void OtherMethod()
    {
        var obj = new List<int>(); // 不应报告
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }

    [Fact]
    public async Task AllocationInCalledMethodAcrossClasses_ShouldNotReportDiagnostic()
    {
        const string text = @"
using System.Collections.Generic;

namespace UnityEngine { public class MonoBehaviour {} }

public class Test : UnityEngine.MonoBehaviour
{
    private Helper _helper = new Helper();

    private void Update()
    {
        _helper.DoWork(); // 跨类调用，不应报告
    }
}

public class Helper
{
    public void DoWork()
    {
        var obj = new List<int>(); // 在其他类中，不应报告
    }
}
";

        await Verifier.VerifyAnalyzerAsync(text).ConfigureAwait(false);
    }
}
