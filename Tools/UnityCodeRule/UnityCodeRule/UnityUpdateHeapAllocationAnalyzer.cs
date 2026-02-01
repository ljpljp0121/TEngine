using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace UnityCodeRule;

/// <summary>
/// 检测 Unity MonoBehaviour Update 方法及其调用链中的堆分配。
/// 允许值类型（struct）但报告会导致 GC 压力的类分配。
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class UnityUpdateHeapAllocationAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "AB0001";

    private static readonly LocalizableString Title = new LocalizableResourceString(
        nameof(Resources.AB0001Title),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString MessageFormat = new LocalizableResourceString(
        nameof(Resources.AB0001MessageFormat),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString Description = new LocalizableResourceString(
        nameof(Resources.AB0001Description),
        Resources.ResourceManager,
        typeof(Resources));

    private const string Category = "Performance";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        // 使用 CompilationStartAction 构建调用图缓存
        context.RegisterCompilationStartAction(AnalyzeCompilationStart);
    }

    private void AnalyzeCompilationStart(CompilationStartAnalysisContext context)
    {
        // 调用图缓存：Key = MonoBehaviour 类型, Value = Update 方法调用的所有方法集合
        var callGraphCache = new ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<IMethodSymbol>>();
        var stopwatch = Stopwatch.StartNew();
        var analyzerState = new AnalyzerState();

        // 阶段 1: 分析对象创建时检查调用图
        context.RegisterOperationAction(c =>
        {
            AnalyzeObjectCreation(c, callGraphCache, analyzerState);
        }, OperationKind.ObjectCreation);

        // 阶段 2: 编译结束时报告性能数据
        context.RegisterCompilationEndAction(c =>
        {
            stopwatch.Stop();
            ReportPerformance(c, analyzerState, stopwatch.ElapsedMilliseconds);
        });
    }

    /// <summary>
    /// 分析对象创建操作，检查是否在 Update 调用链中
    /// </summary>
    private void AnalyzeObjectCreation(
        OperationAnalysisContext context,
        ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<IMethodSymbol>> callGraph,
        AnalyzerState state)
    {
        var objectCreation = (IObjectCreationOperation)context.Operation;

        // 允许 struct 分配（值类型）
        if (objectCreation.Type?.IsValueType != false)
            return;

        // 获取包含方法
        var containingMethod = GetContainingMethodSymbol(context);
        if (containingMethod == null) return;

        // 检查包含类型是否是 MonoBehaviour
        var containingType = containingMethod.ContainingType;
        if (!InheritsFromMonoBehaviour(containingType))
            return;

        // 检查是否在 Update 方法或其调用的方法中
        bool isInUpdateCallChain = false;

        // 方式 1: 直接在 Update 中
        if (containingMethod.Name == "Update")
        {
            isInUpdateCallChain = true;
        }
        // 方式 2: 检查是否在 Update 调用的方法中
        else if (IsInUpdateCallChain(containingMethod, containingType, callGraph, context, state))
        {
            isInUpdateCallChain = true;
        }

        if (isInUpdateCallChain)
        {
            var diagnostic = Diagnostic.Create(
                Rule,
                objectCreation.Syntax.GetLocation(),
                objectCreation.Type.Name,
                $"{containingType.Name}.{containingMethod.Name}");

            context.ReportDiagnostic(diagnostic);
        }
    }

    /// <summary>
    /// 检查指定方法是否在 Update 调用链中
    /// </summary>
    private bool IsInUpdateCallChain(
        IMethodSymbol method,
        INamedTypeSymbol containingType,
        ConcurrentDictionary<INamedTypeSymbol, ConcurrentBag<IMethodSymbol>> callGraph,
        OperationAnalysisContext context,
        AnalyzerState state)
    {
        // 获取或构建该类型的调用图
        var calledMethods = callGraph.GetOrAdd(containingType, type =>
        {
            state.AnalyzedTypes++;
            return BuildUpdateCallGraph(type, context.Compilation, state);
        });

        return calledMethods.Contains(method, SymbolEqualityComparer.Default);
    }

    /// <summary>
    /// 构建指定 MonoBehaviour 类型的 Update 调用图
    /// </summary>
    private ConcurrentBag<IMethodSymbol> BuildUpdateCallGraph(
        INamedTypeSymbol type,
        Compilation compilation,
        AnalyzerState state)
    {
        var calledMethods = new ConcurrentBag<IMethodSymbol>();
        var visited = new ConcurrentDictionary<IMethodSymbol, bool>();

        // 找到 Update 方法
        var updateMethod = FindUpdateMethod(type);
        if (updateMethod == null) return calledMethods;

        // 递归获取 Update 调用的所有方法
        CollectCalledMethods(updateMethod, calledMethods, visited, type, compilation, state);

        return calledMethods;
    }

    /// <summary>
    /// 查找 Update 方法
    /// </summary>
    private IMethodSymbol? FindUpdateMethod(INamedTypeSymbol type)
    {
        var members = type.GetMembers("Update");
        foreach (var member in members)
        {
            if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
            {
                return method;
            }
        }
        return null;
    }

    /// <summary>
    /// 递归收集指定方法调用的所有方法
    /// </summary>
    private void CollectCalledMethods(
        IMethodSymbol method,
        ConcurrentBag<IMethodSymbol> result,
        ConcurrentDictionary<IMethodSymbol, bool> visited,
        INamedTypeSymbol containingType,
        Compilation compilation,
        AnalyzerState state)
    {
        if (method == null) return;

        // TryAdd 返回 true 表示是新添加的（首次访问），false 表示已存在
        // 如果已存在，跳过以防止无限递归
        if (!visited.TryAdd(method, true))
        {
            // 已访问过，跳过
            return;
        }

        // 尝试获取方法的语法节点
        var syntaxReferences = method.DeclaringSyntaxReferences;
        if (syntaxReferences.Length == 0) return;

        foreach (var syntaxRef in syntaxReferences)
        {
            var syntaxNode = syntaxRef.GetSyntax();
            if (syntaxNode == null) continue;

            // 获取语义模型
            var semanticModel = compilation.GetSemanticModel(syntaxNode.SyntaxTree);

            // 遍历方法体中的所有操作
            foreach (var descendant in syntaxNode.DescendantNodes())
            {
                if (descendant.IsKind(SyntaxKind.InvocationExpression))
                {
                    var invocationOperation = semanticModel.GetOperation(descendant) as IInvocationOperation;
                    if (invocationOperation != null)
                    {
                        var targetMethod = invocationOperation.TargetMethod;
                        if (targetMethod != null)
                        {
                            // 只分析同一类内的方法调用（避免跨类分析的复杂度）
                            if (SymbolEqualityComparer.Default.Equals(targetMethod.ContainingType, containingType))
                            {
                                result.Add(targetMethod);
                                state.TotalMethods++;
                                // 递归追踪被调用方法中的调用
                                CollectCalledMethods(targetMethod, result, visited, containingType, compilation, state);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取包含当前操作的 MethodSymbol
    /// </summary>
    private static IMethodSymbol? GetContainingMethodSymbol(OperationAnalysisContext context)
    {
        var node = context.Operation.Syntax;
        while (node != null)
        {
            if (node.IsKind(SyntaxKind.MethodDeclaration))
            {
                var semanticModel = context.Compilation.GetSemanticModel(node.SyntaxTree);
                var methodSymbol = semanticModel.GetDeclaredSymbol(node);
                return methodSymbol as IMethodSymbol;
            }

            node = node.Parent;
        }

        return null;
    }

    /// <summary>
    /// 检查类型是否继承自 MonoBehaviour
    /// </summary>
    private static bool InheritsFromMonoBehaviour(INamedTypeSymbol? typeSymbol)
    {
        var current = typeSymbol;
        while (current != null)
        {
            if (current.Name == "MonoBehaviour" && current.ContainingNamespace.Name == "UnityEngine")
                return true;

            current = current.BaseType;
        }

        return false;
    }

    /// <summary>
    /// 报告分析器性能数据
    /// </summary>
    private void ReportPerformance(
        CompilationAnalysisContext context,
        AnalyzerState state,
        long elapsedMs)
    {
        // 使用 Debugger.IsAttached 确保只在调试时输出
        if (Debugger.IsAttached)
        {
            var message = $"AB0001 分析器性能: 分析 {state.AnalyzedTypes} 个 MonoBehaviour 类型，" +
                         $"追踪 {state.TotalMethods} 个方法调用，耗时 {elapsedMs}ms";

            // 输出到调试器
            Debug.WriteLine(message);
        }
    }

    /// <summary>
    /// 分析器状态跟踪
    /// </summary>
    private class AnalyzerState
    {
        public int AnalyzedTypes { get; set; }
        public int TotalMethods { get; set; }
    }
}
