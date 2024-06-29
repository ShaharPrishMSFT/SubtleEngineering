# SubtleEngineering
Set of analyzers for a better engineering experience.

## SubtleEngineering.Analyzers

### Code Quality Analyzer: Require using statement/declaration modifier for classes

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

###  Code Quality Analyzer: Avoid using relative namespaces in using directives

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


###  Code Quality Analyzer: Define and protect Exhaustive initialization types

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



#### Code fix available
A code fix is available for SE1030 and SE1031 - will add required to properties that are missing it.


