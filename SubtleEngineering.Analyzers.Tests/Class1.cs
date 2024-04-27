namespace SubtleEngineering.Analyzers.Tests;

using SubtleEngineering.Analyzers.Decorators;
using System.Threading.Tasks;

[RequireUsing]
public class Class1 : IDisposable
{
    public static void DoIt()
    {
        using var x = new Class1();
        using (var c = new Class1())
        {
            var c3 = new Class1();
            var c2 = Class1.Create();
        }
    }

    public void Dispose()
    {
    }

    [RequireUsing]
#pragma warning disable RequireUsingId // Type must be instantiated within a using statement
    public static Class1 Create() => new Class1();
#pragma warning restore RequireUsingId // Type must be instantiated within a using statement
}

[RequireUsing]
public class Class2 : IAsyncDisposable
{
    public static async Task DoIt()
    {
        await using var x = new Class2();
        await using (var c = Class2.Create())
        {
            var c3 = new Class2();
            var c2 = Class2.Create();
        }
    }

    public void Dispose()
    {
    }

    [RequireUsing]
#pragma warning disable RequireUsingId // Type must be instantiated within a using statement
    public static Class2 Create() => new Class2();

    public async ValueTask DisposeAsync()
    {
        await ValueTask.CompletedTask;
    }
#pragma warning restore RequireUsingId // Type must be instantiated within a using statement
}

[RequireUsing]
public class Class3
{
}