namespace ConsoleAppFramework;

public static class ConsoleAppBaseCode
{
    public const string GeneratedCodeHeader = """
// <auto-generated/>
#nullable enable
#pragma warning disable

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

    public const string InitializationCode = """
// <auto-generated/>
#nullable enable
#pragma warning disable

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

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public class ConsoleAppFrameworkGeneratorOptionsAttribute : Attribute
{
    public bool DisableNamingConversion { get; set; }
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
            var i = version.IndexOf('+');
            if (i != -1)
            {
                version = version.Substring(0, i);
            }
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

    internal partial class ConsoleAppBuilder
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
            BuildAndSetServiceProvider();
            try
            {
                RunCore(args);
            }
            finally
            {
                if (ServiceProvider is IDisposable d)
                {
                    d.Dispose();
                }
            }
        }

        public async Task RunAsync(string[] args)
        {
            BuildAndSetServiceProvider();
            try
            {
                Task? task = null;
                RunAsyncCore(args, ref task!);
                if (task != null)
                {
                    await task;
                }
            }
            finally
            {
                if (ServiceProvider is IAsyncDisposable ad)
                {
                    await ad.DisposeAsync();
                }
                else if (ServiceProvider is IDisposable d)
                {
                    d.Dispose();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void AddCore(string commandName, Delegate command);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void RunCore(string[] args);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        partial void RunAsyncCore(string[] args, ref Task result);

        partial void BuildAndSetServiceProvider();

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
}
