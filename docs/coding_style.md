﻿# Coding Style

## General

When contributing to the project, and if otherwise not mentioned in this document, our coding conventions
follow the Microsoft [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
and standard [Naming Guidelines](https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines).

## Class Members

Members and types should always have the lowest possible visibility.

Ordering of class members should be the following:

1. Constants
2. Nested enum declarations
3. Fields
4. Abstract members
5. Properties
6. Constructors
7. Methods 
8. Nested types

Methods should be ordered from higher to lower accessibility level (public, internal, protected, private) and the other categories from lower to higher.

Static fields and properties should be placed before instance ones. 

Static methods are preferred to be after instance methods.

Once grouped as specified above, methods which are called by other methods in the same group should be placed below the callers.

```csharp
public int PublicMethod() => 42;

int CallerOne() => Leaf();

int CallerTwo() => Leaf() + PublicMethod();

int Leaf() =>  42;
```

### Local functions

There are no strict rules on when to use local functions. It should be decided on a case-by-case basis.

By default, you should prefer methods over local functions. Use local functions if it makes the code significantly easier to understand. For example:
- Accessing the method's local state directly, instead of using parameters, reduces noise.
- The name of the function would not make sense at the class level.

Local functions should always be placed at the end of a method.

```csharp
public int MethodWithLocalFunction(int x)
{
    return LocalFunction(x);
    
    int LocalFunction(int x) => x;
}
```

### Separation

Individual members must be separated by empty line, except sequence of constants, fields, single-line properties and abstract members. These members should not be separated by empty lines.

```csharp
private const int ValueA = 42;
private const int ValueB = 24;

private int valueA;
private int valueB;

protected abstract int AbstractA { get; }
protected abstract void AbstractB();

public SemanticModel Model { get; }
public SyntaxNode Node { get; }

public int ComplexProperty
{
    get => 42;
    set
    {
        // ...
    }
}

public Constructor() { }

public void MethodA() =>
    MethodB();

public void MethodB()
{
    // ...
}
```

## Naming conventions

Generic words in class names that don't convey meaning (e.g. `Helper`) should be avoided. Overwordy and complex names should be avoided as well.

Short names can be used as parameter and variable names, namely `SyntaxTree tree`, `SemanticModel model`, `SyntaxNode node` and `CancellationToken cancel`.

Variable name `testSubject` is recommended in unit tests that really test a single unit.

## Multi-line statements

* Operators (`&&`, `||`, `and`, `or`, `+`, `:`, `?`, `??` and others) are placed at the beginning of a line.
    * Indented at the same level if the syntax at the beginning of the previous line is a sibling.
      ```csharp
      void Foo() =>
          A
          && B; // A and B are siblings => we don't indent
      ```
    * Indented one level further otherwise.  
      ```csharp
      return A
          && B; // "return" is the parent of A and B => we indent
      ```
* Dot before an invocation `.Method()` is placed at the beginning of a line.
* The comma separating arguments is placed at the end of a line.
* Method declaration parameters should be on the same line. If S103 is violated, parameters should be placed each on a separate line; the first parameter should NOT be on the same line with the declaration.
    ```csharp
    public void MethodWithManyParameters(
                                        int firstParameter,
                                        string secondParameter,
                                        Function<int, string, string> complexParameter);
    ```
* Long ternary operator statements should have `?` and `:` on separate lines, aligned with a left-most single indendation.
    ```csharp
    object.Property is SomeType something
    && something.AnotherProperty is OtherType other
    && other.Value == 42
        ? object.Parent.Value
        : object;
    ```
* Chained invocations and member accesses violating S103 can have a chain of properties on the first line. Every other `.Invocation()` or `.Member` should be on a separate line, aligned with a left-most single indendation.
    ```csharp
    object.Property.Children
        .Select(x => x.Something)
        .Where(x => x != null)
        .OrderBy(x => x.Rank)
        .ToArray()
        .Length;
    ```
  * Exception from this rule: Chains of assertions can have supporting properties, `.Should()` and assertion on the same line.
    ```csharp
    values.Should().HaveCount(2)
        .And.ContainSingle(x => x.HasConstraint(BoolConstraint.True))
        .And.ContainSingle(x => x.HasConstraint(BoolConstraint.False));
    ```
* Method invocation arguments should be placed on the same line only when they are few and simple. Otherwise, they should be placed on separate lines. The first argument should be on a separate line, aligned with a left-most single indendation.
    ```csharp
    object.MethodName(
        firstArgument,
        x => x.Bar(),
        thirdArgument.Property);
    ```
  * Exception from this rule: chained LINQ queries where the alignment of parameter expressions should be right-most.
    ```csharp
    someEnumerable.Where(x => x.Condition1
                              && x.Condition2);
    ```
  * Exception from this rule: Lambda parameter name and arrow token should be on the same line as the invocation.
    ```
    context.RegisterSyntaxNodeAction(c =>
        {
            // Action
        }
    ```
* When using an arrow property or an arrow method, the `=>` token must be on the same line as the declaration. Regarding the expression body:
  * for properties: it should be on the same line as the property declaration. It should be on the following line only when it is too long and would trigger S103.
  * for methods: it should be on the same line only for trivial cases: literal or simple identifier. Member access, indexer, invocation, and other complex structures should be on the following line.

## Code structure

* Field and property initializations are done directly in the member declaration instead of in a constructor.
* For multiple conditions before the core method logic:
  * chain conditions in the same `if` statement together with positive logic for best readability (i.e. `if (first && second) { DoSomething(); }`) 
  * when chained conditions cannot be used, use early returns
  * otherwise, use nested conditions
* Use positive logic.
* Use `is {}` and `is not null` as null-checks (instead of `!= null`).

## Comments

* Code should contain as few comments as necessary in favor of well-named members and variables.
* Comments should generally be on separate lines.
* Comments on the same line with code are acceptable for short lines of code and short comments.
* Documentation comments for abstract methods and their implementations should be placed only on the abstract method, to avoid duplication. _When reading the implementation, the IDE offers the tooling to peek in the base class and read the method comment._

## ToDo

* `ToDo` can be used to mark part of the code that will need to be updated at a later time. It can be used to
track updates that should be done at some point, but that either cannot be done at that moment, or can be fixed later.
Ideally, a `ToDo` comment should be followed by an issue number (what needs to be done should be in the github issues).

## Regions

Generally, as we do not want to have classes that are too long, regions are not necessary and should be avoided.
It can still be used when and where it makes sense. For instance, when a class having a specific purpose is
implementing generic interfaces (such as `IComparable`, `IDisposable`), it can make sense to have regions 
for the implementation of these interfaces.