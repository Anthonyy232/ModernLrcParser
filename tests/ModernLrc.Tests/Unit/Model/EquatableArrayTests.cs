using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

public sealed class EquatableArrayTests
{
    [Fact]
    public void Empty_HasZeroCount()
    {
        EquatableArray<int>.Empty.Count.ShouldBe(0);
    }

    [Fact]
    public void Default_NormalizesToEmptyForEnumeration()
    {
        // The struct's default value should never throw on Count or iteration.
        EquatableArray<int> def = default;
        def.Count.ShouldBe(0);
        Should.NotThrow(() => { foreach (var _ in def) { } });
    }

    [Fact]
    public void Equals_SameElements_ReturnsTrue()
    {
        EquatableArray<int> a = [1, 2, 3];
        EquatableArray<int> b = [1, 2, 3];
        a.Equals(b).ShouldBeTrue();
        (a == b).ShouldBeTrue();
        (a != b).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentElements_ReturnsFalse()
    {
        EquatableArray<int> a = [1, 2, 3];
        EquatableArray<int> b = [1, 2, 4];
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void Equals_DifferentLengths_ReturnsFalse()
    {
        EquatableArray<int> a = [1, 2, 3];
        EquatableArray<int> b = [1, 2];
        a.Equals(b).ShouldBeFalse();
    }

    [Fact]
    public void GetHashCode_SameElements_SameHash()
    {
        EquatableArray<int> a = [1, 2, 3];
        EquatableArray<int> b = [1, 2, 3];
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Indexer_ReturnsElementAtPosition()
    {
        EquatableArray<string> a = ["x", "y", "z"];
        a[0].ShouldBe("x");
        a[2].ShouldBe("z");
    }

    [Fact]
    public void Indexer_OutOfRange_Throws()
    {
        EquatableArray<int> a = [1, 2, 3];
        Should.Throw<IndexOutOfRangeException>(() => _ = a[5]);
    }

    [Fact]
    public void AsSpan_ProducesEquivalentSpan()
    {
        EquatableArray<int> a = [10, 20, 30];
        ReadOnlySpan<int> s = a.AsSpan();
        s.Length.ShouldBe(3);
        s[1].ShouldBe(20);
    }

    [Fact]
    public void ImplicitConversionFromImmutableArray_Wraps()
    {
        EquatableArray<int> a = System.Collections.Immutable.ImmutableArray.Create(1, 2);
        a.Count.ShouldBe(2);
        a[0].ShouldBe(1);
    }

    [Fact]
    public void Foreach_IteratesEveryElement_InOrder()
    {
        // We could not credibly assert "no boxing" without runtime allocation tracking
        // (and that varies across runtimes), so this test only pins what consumers can
        // observe: foreach yields each element exactly once, in source order.
        EquatableArray<int> a = [1, 2, 3];
        var collected = new List<int>();
        foreach (var x in a) collected.Add(x);
        collected.Count.ShouldBe(3);
        collected[0].ShouldBe(1);
        collected[1].ShouldBe(2);
        collected[2].ShouldBe(3);
    }

    [Fact]
    public void Equals_BoxedToObject_StillUsesContentEquality()
    {
        EquatableArray<int> a = [1, 2];
        object boxed = (EquatableArray<int>)[1, 2];
        a.Equals(boxed).ShouldBeTrue();
    }
}
