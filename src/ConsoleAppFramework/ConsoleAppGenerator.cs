﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Reflection;

namespace ConsoleAppFramework;

[Generator(LanguageNames.CSharp)]
public partial class ConsoleAppGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(EmitConsoleAppTemplateSource);

        // ConsoleApp.Create(Action<IServiceCollection> configure)
        var hasDependencyInjection = context.MetadataReferencesProvider
            .Where(x =>
            {
                return x.Display?.EndsWith("Microsoft.Extensions.DependencyInjection.Abstractions.dll") ?? false;
            })
            .Collect()
            .Select((x, ct) => x.Length != 0);

        context.RegisterSourceOutput(hasDependencyInjection, EmitConsoleAppCreateConfigure);

        // ConsoleApp.Run
        var runSource = context.SyntaxProvider
            .CreateSyntaxProvider((node, ct) =>
            {
                if (node.IsKind(SyntaxKind.InvocationExpression))
                {
                    var invocationExpression = (node as InvocationExpressionSyntax);
                    if (invocationExpression == null) return false;

                    var expr = invocationExpression.Expression as MemberAccessExpressionSyntax;
                    if ((expr?.Expression as IdentifierNameSyntax)?.Identifier.Text == "ConsoleApp")
                    {
                        var methodName = expr?.Name.Identifier.Text;
                        if (methodName is "Run" or "RunAsync")
                        {
                            return true;
                        }
                    }

                    return false;
                }

                return false;
            }, (context, ct) =>
            {
                var reporter = new DiagnosticReporter();
                var node = (InvocationExpressionSyntax)context.Node;
                var wellknownTypes = new WellKnownTypes(context.SemanticModel.Compilation);
                var parser = new Parser(reporter, node, context.SemanticModel, wellknownTypes, DelegateBuildType.MakeCustomDelegateWhenHasDefaultValueOrTooLarge, []);
                var isRunAsync = (node.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text == "RunAsync";

                var command = parser.ParseAndValidateForRun();
                return new CommanContext(command, isRunAsync, reporter, node);
            })
            .WithTrackingName("ConsoleApp.Run.0_CreateSyntaxProvider"); // annotate for IncrementalGeneratorTest

        context.RegisterSourceOutput(runSource, EmitConsoleAppRun);

        // ConsoleAppBuilder
        var builderSource = context.SyntaxProvider
            .CreateSyntaxProvider((node, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                if (node.IsKind(SyntaxKind.InvocationExpression))
                {
                    var invocationExpression = (node as InvocationExpressionSyntax);
                    if (invocationExpression == null) return false;

                    var expr = invocationExpression.Expression as MemberAccessExpressionSyntax;
                    var methodName = expr?.Name.Identifier.Text;
                    if (methodName is "Add" or "UseFilter" or "Run" or "RunAsync")
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }, (context, ct) => new BuilderContext( // no equality check
                (InvocationExpressionSyntax)context.Node,
                ((context.Node as InvocationExpressionSyntax)!.Expression as MemberAccessExpressionSyntax)!.Name.Identifier.Text,
                context.SemanticModel,
                ct))
            .WithTrackingName("ConsoleApp.Builder.0_CreateSyntaxProvider")
            .Where(x =>
            {
                var model = x.Model.GetTypeInfo((x.Node.Expression as MemberAccessExpressionSyntax)!.Expression, x.CancellationToken);
                return model.Type?.Name is "ConsoleAppBuilder";
            })
            .WithTrackingName("ConsoleApp.Builder.1_Where")
            .Collect()
            .Select((x, ct) => new CollectBuilderContext(x, ct))
            .WithTrackingName("ConsoleApp.Builder.2_Collect");

        var registerCommands = context.SyntaxProvider.ForAttributeWithMetadataName("ConsoleAppFramework.RegisterCommandsAttribute",
            (node, token) => true,
            (ctx, token) => ctx)
            .Collect();

        var combined = builderSource.Combine(registerCommands)
            .Select((tuple, token) =>
            {
                var (context, commands) = tuple;
                context.AddRegisterAttributes(commands);
                return context;
            });

        context.RegisterSourceOutput(combined, EmitConsoleAppBuilder);
    }

    public const string ConsoleAppBaseCode = """
// <auto-generated/>
#nullable enable
namespace ConsoleAppFramework;

using System;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

#if !USE_EXTERNAL_CONSOLEAPP_ABSTRACTIONS

internal interface IArgumentParser<T>
{
    static abstract bool TryParse(ReadOnlySpan<char> s, out T result);
}

internal record class ConsoleAppContext(string CommandName, string[] Arguments, object? State);

internal abstract class ConsoleAppFilter(ConsoleAppFilter next)
{
    protected readonly ConsoleAppFilter Next = next;

    public abstract Task InvokeAsync(ConsoleAppContext context, CancellationToken cancellationToken);
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
internal sealed class ConsoleAppFilterAttribute<T> : Attribute
    where T : ConsoleAppFilter
{
}

internal sealed class ArgumentParseFailedException(string message) : Exception(message)
{
}

#endif

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class FromServicesAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class ArgumentAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class CommandAttribute : Attribute
{
    public string Command { get; }

    public CommandAttribute(string command)
    {
        this.Command = command;
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class RegisterCommandsAttribute : Attribute
{
    public string CommandPath { get; }

    public RegisterCommandsAttribute()
    {
        this.CommandPath = "";
    }

    public RegisterCommandsAttribute(string commandPath)
    {
        this.CommandPath = commandPath;
    }
}

internal static partial class ConsoleApp
{
    public static IServiceProvider? ServiceProvider { get; set; }
    public static TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public static System.Text.Json.JsonSerializerOptions? JsonSerializerOptions { get; set; }
    public static string? Version { get; set; }

    static Action<string>? logAction;
    public static Action<string> Log
    {
        get => logAction ??= Console.WriteLine;
        set => logAction = value;
    }

    static Action<string>? logErrorAction;
    public static Action<string> LogError
    {
        get => logErrorAction ??= (static msg => Log(msg));
        set => logErrorAction = value;
    }

    /// <summary>
    /// <para>You can pass second argument that generates new Run overload.</para>
    /// ConsoleApp.Run(args, (int x, int y) => { });<br/>
    /// ConsoleApp.Run(args, Foo);<br/>
    /// ConsoleApp.Run(args, &amp;Foo);<br/>
    /// </summary>
    public static void Run(string[] args)
    {
    }

    /// <summary>
    /// <para>You can pass second argument that generates new RunAsync overload.</para>
    /// ConsoleApp.RunAsync(args, (int x, int y) => { });<br/>
    /// ConsoleApp.RunAsync(args, Foo);<br/>
    /// ConsoleApp.RunAsync(args, &amp;Foo);<br/>
    /// </summary>
    public static Task RunAsync(string[] args)
    {
        return Task.CompletedTask;
    }

    public static ConsoleAppBuilder Create() => new ConsoleAppBuilder();

    public static ConsoleAppBuilder Create(IServiceProvider serviceProvider)
    {
        ConsoleApp.ServiceProvider = serviceProvider;
        return ConsoleApp.Create();
    }

    static void ThrowArgumentParseFailed(string argumentName, string value)
    {
        throw new ArgumentParseFailedException($"Argument '{argumentName}' failed to parse, provided value: {value}");
    }

    static void ThrowRequiredArgumentNotParsed(string name)
    {
        throw new ArgumentParseFailedException($"Required argument '{name}' was not specified.");
    }

    static void ThrowArgumentNameNotFound(string argumentName)
    {
        throw new ArgumentParseFailedException($"Argument '{argumentName}' is not recognized.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryIncrementIndex(ref int index, int length)
    {
        if ((index + 1) < length)
        {
            index += 1;
            return true;
        }
        return false;
    }

    static bool TryParseParamsArray<T>(ReadOnlySpan<string> args, ref T[] result, ref int i)
       where T : IParsable<T>
    {
        result = new T[args.Length - i];
        var resultIndex = 0;
        for (; i < args.Length; i++)
        {
            if (!T.TryParse(args[i], null, out result[resultIndex++]!)) return false;
        }
        return true;
    }

    static bool TrySplitParse<T>(ReadOnlySpan<char> s, out T[] result)
       where T : ISpanParsable<T>
    {
        if (s.StartsWith("["))
        {
            try
            {
                result = System.Text.Json.JsonSerializer.Deserialize<T[]>(s, JsonSerializerOptions)!;
                return true;
            }
            catch
            {
                result = default!;
                return false;
            }
        }

        var count = s.Count(',') + 1;
        result = new T[count];

        var source = s;
        var destination = result.AsSpan();
        Span<Range> ranges = stackalloc Range[Math.Min(count, 128)];

        while (true)
        {
            var splitCount = source.Split(ranges, ',');
            var parseTo = splitCount;
            if (splitCount == 128 && source[ranges[^1]].Contains(','))
            {
                parseTo = splitCount - 1;
            }

            for (int i = 0; i < parseTo; i++)
            {
                if (!T.TryParse(source[ranges[i]], null, out destination[i]!))
                {
                    return false;
                }
            }
            destination = destination.Slice(parseTo);

            if (destination.Length != 0)
            {
                source = source[ranges[^1]];
                continue;
            }
            else
            {
                break;
            }
        }

        return true;
    }

    static void ValidateParameter(object? value, ParameterInfo parameter, ValidationContext validationContext, ref StringBuilder? errorMessages)
    {
        validationContext.DisplayName = parameter.Name ?? "";
        validationContext.Items.Clear();

        foreach (var validator in parameter.GetCustomAttributes<ValidationAttribute>(false))
        {
            var result = validator.GetValidationResult(value, validationContext);
            if (result != null)
            {
                if (errorMessages == null)
                {
                    errorMessages = new StringBuilder();
                }
                errorMessages.AppendLine(result.ErrorMessage);
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static bool TryShowHelpOrVersion(ReadOnlySpan<string> args, int requiredParameterCount, int helpId)
    {
        if (args.Length == 0)
        {
            if (requiredParameterCount == 0) return false;
            
            ShowHelp(helpId);
            return true;
        }

        if (args.Length == 1)
        {
            switch (args[0])
            {
                case "--version":
                    ShowVersion();
                    return true;
                case "-h":
                case "--help":
                    ShowHelp(helpId);
                    return true;
                default:
                    break;
            }
        }

        return false;
    }

    static void ShowVersion()
    {
        if (Version != null)
        {
            Log(Version);
            return;
        }

        var asm = Assembly.GetEntryAssembly();
        var version = "1.0.0";
        var infoVersion = asm!.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (infoVersion != null)
        {
            version = infoVersion.InformationalVersion;
        }
        else
        {
            var asmVersion = asm!.GetCustomAttribute<AssemblyVersionAttribute>();
            if (asmVersion != null)
            {
                version = asmVersion.Version;
            }
        }
        Log(version);
    }

    static partial void ShowHelp(int helpId);

    static async Task RunWithFilterAsync(string commandName, string[] args, ConsoleAppFilter invoker)
    {
        using var posixSignalHandler = PosixSignalHandler.Register(Timeout);
        try
        {
            await Task.Run(() => invoker.InvokeAsync(new ConsoleAppContext(commandName, args, null), posixSignalHandler.Token)).WaitAsync(posixSignalHandler.TimeoutToken);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                Environment.ExitCode = 130;
                return;
            }

            Environment.ExitCode = 1;
            if (ex is ValidationException or ArgumentParseFailedException)
            {
                LogError(ex.Message);
            }
            else
            {
                LogError(ex.ToString());
            }
        }
    }

    sealed class PosixSignalHandler : IDisposable
    {
        public CancellationToken Token => cancellationTokenSource.Token;
        public CancellationToken TimeoutToken => timeoutCancellationTokenSource.Token;

        CancellationTokenSource cancellationTokenSource;
        CancellationTokenSource timeoutCancellationTokenSource;
        TimeSpan timeout;

        PosixSignalRegistration? sigInt;
        PosixSignalRegistration? sigQuit;
        PosixSignalRegistration? sigTerm;

        PosixSignalHandler(TimeSpan timeout)
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.timeoutCancellationTokenSource = new CancellationTokenSource();
            this.timeout = timeout;
        }

        public static PosixSignalHandler Register(TimeSpan timeout)
        {
            var handler = new PosixSignalHandler(timeout);

            Action<PosixSignalContext> handleSignal = handler.HandlePosixSignal;

            handler.sigInt = PosixSignalRegistration.Create(PosixSignal.SIGINT, handleSignal);
            handler.sigQuit = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handleSignal);
            handler.sigTerm = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handleSignal);

            return handler;
        }

        void HandlePosixSignal(PosixSignalContext context)
        {
            context.Cancel = true;
            cancellationTokenSource.Cancel();
            timeoutCancellationTokenSource.CancelAfter(timeout);
        }

        public void Dispose()
        {
            sigInt?.Dispose();
            sigQuit?.Dispose();
            sigTerm?.Dispose();
            timeoutCancellationTokenSource.Dispose();
        }
    }

    struct SyncAsyncDisposeWrapper<T>(T value) : IDisposable
        where T : IAsyncDisposable
    {
        public readonly T Value => value;

        public void Dispose()
        {
            value.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    internal partial struct ConsoleAppBuilder
    {
        public ConsoleAppBuilder()
        {
        }

        public void Add(string commandName, Delegate command)
        {
            AddCore(commandName, command);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public void Add<T>() { }

        [System.Diagnostics.Conditional("DEBUG")]
        public void Add<T>(string commandPath) { }

        [System.Diagnostics.Conditional("DEBUG")]
        public void UseFilter<T>() where T : ConsoleAppFilter { }

        public void Run(string[] args)
        {
            RunCore(args);
        }

        public Task RunAsync(string[] args)
        {
            Task? task = null;
            RunAsyncCore(args, ref task!);
            return task ?? Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void AddCore(string commandName, Delegate command);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void RunCore(string[] args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void RunAsyncCore(string[] args, ref Task result);

        static partial void ShowHelp(int helpId);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool TryShowHelpOrVersion(ReadOnlySpan<string> args, int requiredParameterCount, int helpId)
        {
            if (args.Length == 0)
            {
                if (requiredParameterCount == 0) return false;
            
                ShowHelp(helpId);
                return true;
            }

            if (args.Length == 1)
            {
                switch (args[0])
                {
                    case "--version":
                        ShowVersion();
                        return true;
                    case "-h":
                    case "--help":
                        ShowHelp(helpId);
                        return true;
                    default:
                        break;
                }
            }

            return false;
        }
    }
}
""";

    static void EmitConsoleAppTemplateSource(IncrementalGeneratorPostInitializationContext context)
    {
        context.AddSource("ConsoleApp.cs", ConsoleAppBaseCode);
    }

    const string GeneratedCodeHeader = """
// <auto-generated/>
#nullable enable
#pragma warning disable CS0108 // hides inherited member
#pragma warning disable CS0162 // Unreachable code
#pragma warning disable CS0164 // This label has not been referenced
#pragma warning disable CS0219 // Variable assigned but never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8601 // Possible null reference assignment
#pragma warning disable CS8602
#pragma warning disable CS8604 // Possible null reference argument for parameter
#pragma warning disable CS8619
#pragma warning disable CS8620
#pragma warning disable CS8631 // The type cannot be used as type parameter in the generic type or method
#pragma warning disable CS8765 // Nullability of type of parameter
#pragma warning disable CS9074 // The 'scoped' modifier of parameter doesn't match overridden or implemented member
#pragma warning disable CA1050 // Declare types in namespaces.
#pragma warning disable CS1998
#pragma warning disable CS8625
        
namespace ConsoleAppFramework;
        
using System;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using System.ComponentModel.DataAnnotations;

""";

    static void EmitConsoleAppRun(SourceProductionContext sourceProductionContext, CommanContext commandContext)
    {
        if (commandContext.DiagnosticReporter.HasDiagnostics)
        {
            commandContext.DiagnosticReporter.ReportToContext(sourceProductionContext);
            return;
        }
        var command = commandContext.Command;
        if (command == null) return;

        if (command.HasFilter)
        {
            sourceProductionContext.ReportDiagnostic(DiagnosticDescriptors.CommandHasFilter, commandContext.Node.GetLocation());
            return;
        }

        var sb = new SourceBuilder(0);
        sb.AppendLine(GeneratedCodeHeader);
        using (sb.BeginBlock("internal static partial class ConsoleApp"))
        {
            var emitter = new Emitter();
            var withId = new Emitter.CommandWithId(null, command, -1);
            emitter.EmitRun(sb, withId, command.IsAsync);
        }
        sourceProductionContext.AddSource("ConsoleApp.Run.g.cs", sb.ToString());

        var help = new SourceBuilder(0);
        help.AppendLine(GeneratedCodeHeader);
        using (help.BeginBlock("internal static partial class ConsoleApp"))
        {
            var emitter = new Emitter();
            emitter.EmitHelp(help, command);
        }
        sourceProductionContext.AddSource("ConsoleApp.Run.Help.g.cs", help.ToString());
    }

    static void EmitConsoleAppBuilder(SourceProductionContext sourceProductionContext, CollectBuilderContext collectBuilderContext)
    {
        var reporter = collectBuilderContext.DiagnosticReporter;
        var hasRun = collectBuilderContext.HasRun;
        var hasRunAsync = collectBuilderContext.HasRunAsync;

        if (reporter.HasDiagnostics)
        {
            reporter.ReportToContext(sourceProductionContext);
            return;
        }

        if (!hasRun && !hasRunAsync) return;

        var sb = new SourceBuilder(0);
        sb.AppendLine(GeneratedCodeHeader);

        var delegateSignatures = new List<string>();

        // with id number
        var commandIds = collectBuilderContext.Commands
            .Select((x, i) =>
            {
                var command = new Emitter.CommandWithId(
                    FieldType: x!.BuildDelegateSignature(Emitter.CommandWithId.BuildCustomDelegateTypeName(i), out var delegateDef),
                    Command: x!,
                    Id: i
                );
                if (delegateDef != null)
                {
                    delegateSignatures.Add(delegateDef);
                }
                return command;
            })
            .ToArray();

        using (sb.BeginBlock("internal static partial class ConsoleApp"))
        {
            foreach (var d in delegateSignatures)
            {
                sb.AppendLine(d);
            }

            var emitter = new Emitter();
            emitter.EmitBuilder(sb, commandIds, hasRun, hasRunAsync);
        }
        sourceProductionContext.AddSource("ConsoleApp.Builder.g.cs", sb.ToString());

        // Build Help

        var help = new SourceBuilder(0);
        help.AppendLine(GeneratedCodeHeader);
        using (help.BeginBlock("internal static partial class ConsoleApp"))
        using (help.BeginBlock("internal partial struct ConsoleAppBuilder"))
        {
            var emitter = new Emitter();
            emitter.EmitHelp(help, commandIds!);
        }
        sourceProductionContext.AddSource("ConsoleApp.Builder.Help.g.cs", help.ToString());
    }

    static void EmitConsoleAppCreateConfigure(SourceProductionContext sourceProductionContext, bool hasDependencyInjection)
    {
        var code = """
// <auto-generated/>
#nullable enable
namespace ConsoleAppFramework;
            
using System;
using Microsoft.Extensions.DependencyInjection;

internal static partial class ConsoleApp
{
    public static ConsoleAppBuilder Create(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        ConsoleApp.ServiceProvider = services.BuildServiceProvider();
        return ConsoleApp.Create();
    }
}
""";

        // emit empty if not exists
        if (!hasDependencyInjection)
        {
            code = "";
        }

        sourceProductionContext.AddSource("ConsoleApp.Create.g.cs", code);
    }

    class CommanContext(Command? command, bool isAsync, DiagnosticReporter diagnosticReporter, InvocationExpressionSyntax node) : IEquatable<CommanContext>
    {
        public Command? Command => command;
        public DiagnosticReporter DiagnosticReporter => diagnosticReporter;
        public InvocationExpressionSyntax Node => node;
        public bool IsAsync => isAsync;

        public bool Equals(CommanContext other)
        {
            // has diagnostics, always go to modified(don't cache)
            if (diagnosticReporter.HasDiagnostics || other.DiagnosticReporter.HasDiagnostics) return false;
            if (command == null || other.Command == null) return false; // maybe has diagnostics

            if (isAsync != other.IsAsync) return false;
            return command.Equals(other.Command);
        }
    }

    class CollectBuilderContext : IEquatable<CollectBuilderContext>
    {
        public Command[] Commands { get; private set; } = [];
        public DiagnosticReporter DiagnosticReporter { get; }
        public CancellationToken CancellationToken { get; }
        public bool HasRun { get; }
        public bool HasRunAsync { get; }

        FilterInfo[]? globalFilters { get; }

        public CollectBuilderContext(ImmutableArray<BuilderContext> contexts, CancellationToken cancellationToken)
        {
            this.DiagnosticReporter = new DiagnosticReporter();
            this.CancellationToken = cancellationToken;

            // validation, invoke in loop is not allowed.
            foreach (var item in contexts)
            {
                if (item.Name is "Run" or "RunAsync") continue;
                foreach (var n in item.Node.Ancestors())
                {
                    if (n.Kind() is SyntaxKind.WhileStatement or SyntaxKind.DoStatement or SyntaxKind.ForStatement or SyntaxKind.ForEachStatement)
                    {
                        DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.AddInLoopIsNotAllowed, item.Node.GetLocation());
                        return;
                    }
                }
            }

            var methodGroup = contexts.ToLookup(x =>
            {
                if (x.Name == "Add" && ((x.Node.Expression as MemberAccessExpressionSyntax)?.Name.IsKind(SyntaxKind.GenericName) ?? false))
                {
                    return "Add<T>";
                }

                return x.Name;
            });

            globalFilters = methodGroup["UseFilter"]
                .OrderBy(x => x.Node.GetLocation().SourceSpan) // sort by line number
                .Select(x =>
                {
                    var genericName = (x.Node.Expression as MemberAccessExpressionSyntax)?.Name as GenericNameSyntax;
                    var genericType = genericName!.TypeArgumentList.Arguments[0];
                    var type = x.Model.GetTypeInfo(genericType).Type;
                    if (type == null) return null!;

                    var filter = FilterInfo.Create(type);

                    if (filter == null)
                    {
                        DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.FilterMultipleConsturtor, genericType.GetLocation());
                        return null!;
                    }

                    return filter!;
                })
                .ToArray();

            // don't emit if exists failure
            if (DiagnosticReporter.HasDiagnostics)
            {
                globalFilters = null;
                return;
            }

            var names = new HashSet<string>();
            var commands1 = methodGroup["Add"]
                .Select(x =>
                {
                    var wellKnownTypes = new WellKnownTypes(x.Model.Compilation);
                    var parser = new Parser(DiagnosticReporter, x.Node, x.Model, wellKnownTypes, DelegateBuildType.MakeCustomDelegateWhenHasDefaultValueOrTooLarge, globalFilters);
                    var command = parser.ParseAndValidateForBuilderDelegateRegistration();

                    // validation command name duplicate
                    if (command != null && !names.Add(command.Name))
                    {
                        var location = x.Node.ArgumentList.Arguments[0].GetLocation();
                        DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.DuplicateCommandName, location, command!.Name);
                        return null;
                    }

                    return command;
                })
                .ToArray(); // evaluate first.

            var commands2 = methodGroup["Add<T>"]
                .SelectMany(x =>
                {
                    var wellKnownTypes = new WellKnownTypes(x.Model.Compilation);
                    var parser = new Parser(DiagnosticReporter, x.Node, x.Model, wellKnownTypes, DelegateBuildType.None, globalFilters);
                    var commands = parser.ParseAndValidateForBuilderClassRegistration();

                    // validation command name duplicate
                    foreach (var command in commands)
                    {
                        if (command != null && !names.Add(command.Name))
                        {
                            DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.DuplicateCommandName, x.Node.GetLocation(), command!.Name);
                            return [null];
                        }
                    }

                    return commands;
                });

            if (DiagnosticReporter.HasDiagnostics)
            {
                return;
            }

            // set properties
            this.Commands = commands1.Concat(commands2!).ToArray()!; // not null if no diagnostics
            this.HasRun = methodGroup["Run"].Any();
            this.HasRunAsync = methodGroup["RunAsync"].Any();
        }

        // from ForAttributeWithMetadataName
        public void AddRegisterAttributes(ImmutableArray<GeneratorAttributeSyntaxContext> contexts)
        {
            if (contexts.Length == 0 || DiagnosticReporter.HasDiagnostics)
            {
                return;
            }

            var names = new HashSet<string>(Commands.Select(x => x.Name));

            var list = new List<Command>();
            foreach (var ctx in contexts)
            {
                string? commandPath = null;
                var attrData = ctx.Attributes[0]; // AllowMultiple = false
                if (attrData.ConstructorArguments.Length != 0)
                {
                    commandPath = attrData.ConstructorArguments[0].Value as string;
                }

                var wellKnownTypes = new WellKnownTypes(ctx.SemanticModel.Compilation);
                var parser = new Parser(DiagnosticReporter, ctx.TargetNode, ctx.SemanticModel, wellKnownTypes, DelegateBuildType.None, globalFilters ?? []);

                var commands = parser.CreateCommandsFromType((ITypeSymbol)ctx.TargetSymbol, commandPath);

                foreach (var command in commands)
                {
                    if (command != null)
                    {
                        if (!names.Add(command.Name))
                        {
                            DiagnosticReporter.ReportDiagnostic(DiagnosticDescriptors.DuplicateCommandName, ctx.TargetNode.GetLocation(), command!.Name);
                            break;
                        }
                        else
                        {
                            list.Add(command);
                        }
                    }
                }
            }

            Commands = Commands.Concat(list).ToArray();
        }

        public bool Equals(CollectBuilderContext other)
        {
            if (DiagnosticReporter.HasDiagnostics || other.DiagnosticReporter.HasDiagnostics) return false;
            if (HasRun != other.HasRun) return false;
            if (HasRunAsync != other.HasRunAsync) return false;

            return Commands.AsSpan().SequenceEqual(other.Commands);
        }
    }

    // intermediate structure(no equatable)
    readonly struct BuilderContext(InvocationExpressionSyntax node, string name, SemanticModel model, CancellationToken cancellationToken) : IEquatable<BuilderContext>
    {
        public InvocationExpressionSyntax Node => node;
        public string Name => name;
        public SemanticModel Model => model;
        public CancellationToken CancellationToken => cancellationToken;

        public bool Equals(BuilderContext other)
        {
            return Node == other.Node; // no means.
        }
    }
}