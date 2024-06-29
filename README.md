# SubtleEngineering
Set of analyzers for a better engineering experience.

## SubtleEngineering.Analyzers

### Require using statement/decleratiob modifier for classes

This analyzer checks if a class is decorated with the `RequireUsingAttribute` attribute and if it is it verifies that the class is instantiated within a using statement or using declaration.

In cases where the caller knows that they are doing something safe, they can suppress the waring by calling the `ExcludeFromUsingAsync` or `ExcludeFromUsing` methods.`

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

### Avoid using relative namespaces in using directives

This analyzer checks for using statements that import relative namespaces.

#### SE1010: DoNotUseRelativeImportUsingStatements

**What it looks for**: This analyzer checks for using statements that import relative namespaces.

**Why**: Using relative namespaces in using directives can lead to confusion and can be prone to errors as the codebase evolves or copied around. In some cases, with classes that have popular names, this may actually silently add bugs.

**How it helps engineering**: By enforcing the use of absolute namespace paths in using directives, it helps to ensure that the correct namespaces are being used, which can increase the readability and maintainability of the codebase.

##### Code fix available
A code fix is available for SE1010 - changing the relative namespace into an absolute one.

