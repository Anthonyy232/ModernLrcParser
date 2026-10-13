using ModernLrc.Model;

namespace ModernLrc.Tests.Unit.Model;

public sealed class EquatableArrayTests
{
    [Fact]
    public void Default_BehavesAsEmpty()
    {
        EquatableArray<int> def = default;
        def.Count.ShouldBe(0);
        Should.NotThrow(() => { foreach (var _ in def) { } });
        def.ShouldBe(EquatableArray<int>.Empty);
    }

    [Fact]
    public void Equals_ComparesElements()
    {
        EquatableArray<int> a = [1, 2, 3], same = [1, 2, 3], different = [1, 2, 4], shorter = [1, 2];
        (a == same).ShouldBeTrue();
        a.GetHashCode().ShouldBe(same.GetHashCode());
        (a != different).ShouldBeTrue();
        (a != shorter).ShouldBeTrue();
    }

    [Fact]
    public void Equals_BoxedToObject_StillUsesContentEquality()
    {
        EquatableArray<int> a = [1, 2];
        object boxed = (EquatableArray<int>)[1, 2];
        a.Equals(boxed).ShouldBeTrue();
    }
}
