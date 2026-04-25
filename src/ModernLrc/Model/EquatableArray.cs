using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace ModernLrc.Model;

/// <summary>
/// Wraps an <see cref="ImmutableArray{T}"/> with element-wise content equality
/// so records that contain it auto-generate correct equality. The default struct
/// value normalizes to <see cref="Empty"/> for read operations.
/// </summary>
[CollectionBuilder(typeof(EquatableArray), nameof(EquatableArray.Create))]
public readonly struct EquatableArray<T> :
    IEquatable<EquatableArray<T>>, IReadOnlyList<T>
{
    private readonly ImmutableArray<T> _items;

    // CA1000: static members on generic types are intentional here — Empty
    // follows the ImmutableArray<T>.Empty / ImmutableArray.Create pattern that is idiomatic in .NET.
#pragma warning disable CA1000
    /// <summary>Empty array singleton.</summary>
    public static EquatableArray<T> Empty { get; } = new(ImmutableArray<T>.Empty);
#pragma warning restore CA1000

    /// <summary>Wrap an existing <see cref="ImmutableArray{T}"/>; default arrays normalize to <see cref="Empty"/>.</summary>
    public EquatableArray(ImmutableArray<T> items)
        => _items = items.IsDefault ? ImmutableArray<T>.Empty : items;

    /// <inheritdoc />
    public int Count => _items.IsDefault ? 0 : _items.Length;

    /// <inheritdoc />
    public T this[int index] => _items[index];

    /// <summary>Underlying <see cref="ImmutableArray{T}"/> (interop for callers expecting it).</summary>
    public ImmutableArray<T> AsImmutableArray() => _items.IsDefault ? ImmutableArray<T>.Empty : _items;

    /// <summary>Read-only span over the underlying buffer.</summary>
    public ReadOnlySpan<T> AsSpan() => _items.IsDefault ? default : _items.AsSpan();

    /// <summary>Struct enumerator (zero-allocation foreach).</summary>
    public ImmutableArray<T>.Enumerator GetEnumerator() => AsImmutableArray().GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)AsImmutableArray()).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)AsImmutableArray()).GetEnumerator();

    /// <inheritdoc />
    public bool Equals(EquatableArray<T> other) => AsSpan().SequenceEqual(other.AsSpan());

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var item in AsSpan())
            hash.Add(item);
        return hash.ToHashCode();
    }

    /// <summary>Equality operator.</summary>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);


    // CA2225: The spec's AsImmutableArray() and AsSpan() already serve as named alternates
    // for the two implicit operators below.
#pragma warning disable CA2225
    /// <summary>Implicit conversion from <see cref="ImmutableArray{T}"/>.</summary>
    public static implicit operator EquatableArray<T>(ImmutableArray<T> items) => new(items);

    /// <summary>Implicit conversion to <see cref="ReadOnlySpan{T}"/>.</summary>
    public static implicit operator ReadOnlySpan<T>(EquatableArray<T> items) => items.AsSpan();
#pragma warning restore CA2225
}

/// <summary>Static factory invoked by <see cref="CollectionBuilderAttribute"/> for collection-expression construction.</summary>
public static class EquatableArray
{
    /// <summary>Create an <see cref="EquatableArray{T}"/> from a span (used by <c>[a, b, c]</c> literals).</summary>
    public static EquatableArray<T> Create<T>(ReadOnlySpan<T> items)
        => items.IsEmpty ? EquatableArray<T>.Empty : new EquatableArray<T>(ImmutableArray.Create(items));

    /// <summary>Create an <see cref="EquatableArray{T}"/> from any sequence. Lets builders
    /// avoid the <c>List → CollectionsMarshal.AsSpan → Create(span)</c> dance with the
    /// CS9080 lifetime tradeoff that pattern requires.</summary>
    public static EquatableArray<T> Create<T>(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        return new EquatableArray<T>(ImmutableArray.CreateRange(items));
    }
}
