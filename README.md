# SubtleEngineering
Set of analyzers for a better engineering experience.

## SubtleEngineering.Analyzers

### Code Quality Analyzer: Require using statement/declaration modifier for classes (SE1000, SE1001, SE1002)

This analyzer checks if a class is decorated with the `RequireUsingAttribute` attribute and if it is it verifies that the class is instantiated within a using statement or using declaration.

In cases where the caller knows that they are doing something safe, they can suppress the waring by calling the `ExcludeFromUsingAsync` or `ExcludeFromUsing` methods.`

#### Example

```csharp
[RequireUsing]
public class DisposableClass : IDisposable
{
	public void Dispose()
	{
		// Dispose implementation
	}
}

public class Caller
{
	public void Call()
	{
		// This will raise a warning
		var disposable = new DisposableClass();

		// This will not raise a warning
		using (var disposable = new DisposableClass())
		{
			// Do something with disposable
		}

		// This will not raise a warning
		var disposable = new DisposableClass().ExcludeFromUsing();
	}
}
```

#### Change log

* 2024-10-04: Added support for duck-typed ref struct usage (when a ref struct has a public `Dispose` or `DisposeAsync` methods)

#### SE1000: TypeMustBeInstantiatedWithinAUsingStatement

**What it looks for**: This analyzer checks if a type or method is being instantiated or called outside a using statement or using declaration. This also includes types or methods with the `RequireUsingAttribute` attribute.

**Why**: In C#, a using statement ensures that the Dispose method is called on an object when it is no longer needed, crucial for RIAA scenarios. For types or methods that need to be disposed of properly, it is important to use them within a using statement or declaration.

**How it helps engineering**: It helps in enforcing best practices in resource management, thus preventing potential memory leaks or RIAA scenarios which can negatively impact the application performance.


#### SE1001: TypesDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable

**What it looks for:** This analyzer checks if a class with the RequireUsingAttribute attribute implements the IDisposable or IAsyncDisposable interfaces.

**Why:** The RequireUsingAttribute is intended for types that need to be disposed of properly, and these types should implement the IDisposable or IAsyncDisposable interfaces.

**How it helps engineering**: It ensures that only types that can be properly disposed of are marked with the RequireUsingAttribute, helping to prevent improper resource management.

#### SE1002: MethodsReturnsDecoratedWithTheRequireUsingAttributeMustInheritFromIDisposable

**What it looks for:** This analyzer checks if a method with the RequireUsingAttribute attribute returns a type that implements the IDisposable or IAsyncDisposable interfaces.

**Why**: The RequireUsingAttribute is intended for methods that return types that need to be disposed of properly, and these returned types should implement the IDisposable or IAsyncDisposable interfaces.

**How** it helps engineering: It ensures that methods marked with the RequireUsingAttribute return types that can be properly disposed of, helping to enforce best practices in resource management.

###  Code Quality Analyzer: Avoid using relative namespaces in using directives (SE1010)

This analyzer checks for using statements that import relative namespaces.

#### Example

```csharp
namespace MyNamesace.MySubNamespace1
{
	// ...
}

namespace MyNamesace.MySubNamespace2
{
	using MySubNamespace1; // This will raise a warning
}
```

#### SE1010: DoNotUseRelativeImportUsingStatements

**What it looks for**: This analyzer checks for using statements that import relative namespaces.

**Why**: Using relative namespaces in using directives can lead to confusion and can be prone to errors as the codebase evolves or copied around. In some cases, with classes that have popular names, this may actually silently add bugs.

**How it helps engineering**: By enforcing the use of absolute namespace paths in using directives, it helps to ensure that the correct namespaces are being used, which can increase the readability and maintainability of the codebase.

#### Code fix available
A code fix is available for SE1010 - changing the relative namespace into an absolute one.


###  Code Quality Analyzer: Define and protect Exhaustive initialization types (SE103X)

Applies to types decorated with the `ExhaustiveInitializationAttribute` attribute.

This analyzer verifies that all properties in the type are initialized in the constructor or have the `required` modifier.

#### Example

```csharp
[ExhaustiveInitialization]
public class MyClass
{
	public int MyProperty { get; set; } // This will raise a warning because it's not initialized in the constructor or marked as required
}
```

#### SE1030: TypeInitializationIsNonExhaustive

**What it looks for**: This analyzer checks for types that are not designed to be exhaustively initialized.

**Why**: In certain cases, it's advantageous to ensure that all properties of a type are initialized in the constructor or marked as required. This can help prevent omission bugs and make the code more robust. A good example for this are types that are used as DTOs or models where they need to be converted to and from - marking both class as ExhaustiveInitialization can help ensure that all properties are properly initialized.

**How it helps engineering**: By enforcing exhaustive initialization, it helps to ensure that all properties of a type are properly initialized, which can help prevent bugs and make the code more robust.

#### SE1031: PropertyInitializationIsNonExhaustive

**What it looks for**: This is a warning on specific properties that are not required, or otherwise initialized in the constructor.

**Why**: See SE1030

**How it helps engineering**: See SE1030

#### SE1032: OnlyOneConstructorAllowed

**What it looks for**: When a class (not record) or a struct (not a record struct) contains multiple constructors, and not all properties are required, the analyzer will raise a warning that it does not know how to handle it.

**Why**: Reduce chances of mis-creating the class.

**How it helps engineering**: See SE1030

#### SE1033: NotAllowedOnType

**What it looks for**: Primary constructors are not allowed on classes and structs that are not records.

**Why**: There's no good way to inspect what the primary constructor is doing, and so exhaustiveness cannot be enforced.

**How it helps engineering**: See SE1030

#### SE1034: PrimaryDefaultConstructorValuesNotAllowed

**What it looks for**: Primary constructors are not allowed to have default values on implicit constructor properties.

**Why**: Default arguments on the primary constructor are not allowed on exhauste initialization types.

**How it helps engineering**: See SE1030


#### Code fix available
A code fix is available for SE1030 and SE1031 - will add required to properties that are missing it.


### Code quality: Hide obsolete members and elements from Intellisense (SE104X)

When obsoleting members, it's often a good idea to hide it from intellisense so that developers have less of a chance of using it accidentlly.

#### SE1040: ObsoleteELementsShouldBeHidden

**What it looks for**: When an entity is marked with the [Obsolete] attribute, but not with the [EditorBrowsable(EditorBrowsableState.Never)] attribute

**Why**: When obsoleting an entity, it's preferable that developers stop seeing it in their intellisense. Note that if it needs to be visible, the developer may also decide to use other values of EditorBrowsableState.

**How it helps engineering**: Less chance of deprecated elements being used in new code.

### Code quality: Exhaustive switch statements/expressions (SE105X)

When a developer wants to make sure they always check all possible values of an enum, then can mark the enum value as exhaustive - the analyzer will then make sure all known values are checked.

Note: The `ForceExhaustive` method that is used for this, does nothing but return the enum value to the caller. It's just used to mark the requirement for the analyzer.

```csharp
public enum MyState
{
	Default,
	Created = Default,
	Started,
	Running,
	Completed,
	Abandoned // Newly added enum value
}

public string? MetricName(MyState state)
{
	return state.ForceExhaustive() switch // This will give an SE1050 warning/error because MyState.Abandoned is missing here.
	{
		MyState.Created => "Created", // Created covers Default, so it does not need to be mentioned in the switch
		MyState.Started => null,
		MyState.Running => "Active",
		MyState.Completed => "Done",
		_ => throw InvalidOperationException($"Unrecognized enum value {state}")
	}
}
```

#### SE1050: Exhaustive check missing

**What it looks for**: When a switch statement or expression is applied to a value of an enum, and that enum has been passed to the Extension method `ForceExhaustive`, the analyzer will go through the switch and make sure all possible enum values are inspected.

**Why**: In some cases, a developer knows that all values of an enum need to be considered in certaina areas of the code. In such cases, the developer can mark the code with `ForceExhaustive` and if anyone adds an enum value later on, the code the developer wrote will get the warning/error. 

**How it helps engineering**: It' harder to make mistakes as code evolves.

#### SE1051: ForceExhaustive() may only be called on a value being switched on.

This is to make sure developers don't accidetnally use this method in other places. 

#### SE1052: Exhaustive switches only support certain patterns

The only patterns supported right now by an exhaustive switch statement are normal matches and `or` matches:

```csharp
var x = e.ForceExhaustive() switch
{
	MyEnum.Value1 => // Allowed
	MyEnum.Value1 or MyEnum.Value2 => // Allowed
	// No other pattern is allowed:
	MyEnum why => // Disallowed
	MyEnum.Value1 and MyEnum.Value2 => // Disallowed and doesnt make sense.
	etc...
}
```

### Code quality: Warn against using non-static lambdas (SE106X)

For performance-sensitive code, it's sometimes useful for the developer to prevent misuse of a method or class to protect the developer.

Using the [UseStaticLambda] on a field, property or parameter will cause the analyzer to emit a warning if a non-static lambda is passed in.

```csharp
public ref struct MyOptimizedNonAllocUtility
{
	public MyOptimizedNonAllocUtility([UseStaticLambda] Func<int> myDelegate)
	{
		_myDelegate = myDelegate;
	}
}

// Code using the struct
var s = new MyOptimizedNonAllocUtility(() => 42); // This will emit SE1060

// Fixed code:
var s = new MyOptimizedNonAllocUtility(static () => 42); // This will emit SE1060


```

#### SE1060/SE1061

**What it looks for**: Passing in non-static delegates to methods that expect to get a static.

**Why**: For performance-sensitive code that uses delegates, it's easy to sometimes forget that creating a capturing delegate can be expensive. This will remind the developer to use a delegate that will not allocate or allocate less.

**How it helps engineers**: Reduces the chance of making such mistakes.



