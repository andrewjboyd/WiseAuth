using Shouldly;

namespace WiseAuth.UnitTests;

public class WiseAuthMetadataTests
{
    [Test]
    public void EndpointId_PositiveEnumValue_ConvertsToUlong()
    {
        var metadata = new WiseAuthMetadata<SampleEndpoints>(SampleEndpoints.Value3);

        metadata.EndpointId.ShouldBe(3UL);
    }

    [Test]
    public void EndpointId_ZeroEnumValue_ConvertsToZero()
    {
        var metadata = new WiseAuthMetadata<ZeroValueEnum>(ZeroValueEnum.Zero);

        metadata.EndpointId.ShouldBe(0UL);
    }

    [Test]
    public void EndpointId_UlongBackedEnumMaxValue_ConvertsWithoutLoss()
    {
        var metadata = new WiseAuthMetadata<UlongBackedEnum>(UlongBackedEnum.Max);

        metadata.EndpointId.ShouldBe(ulong.MaxValue);
    }

    [Test]
    public void EndpointId_NegativeEnumValue_ThrowsOverflowException()
    {
        Should.Throw<OverflowException>(() => new WiseAuthMetadata<NegativeValueEnum>(NegativeValueEnum.Negative));
    }

    [Test]
    public void EndpointId_ByteBackedEnumValue_ConvertsToUlong()
    {
        var metadata = new WiseAuthMetadata<ByteBackedEnum>(ByteBackedEnum.Value);

        metadata.EndpointId.ShouldBe(5UL);
    }

    [Test]
    public void EndpointId_SByteBackedPositiveValue_ConvertsToUlong()
    {
        var metadata = new WiseAuthMetadata<SByteBackedEnum>(SByteBackedEnum.Value);

        metadata.EndpointId.ShouldBe(5UL);
    }

    [Test]
    public void EndpointId_SByteBackedNegativeValue_ThrowsOverflowException()
    {
        Should.Throw<OverflowException>(() => new WiseAuthMetadata<SByteNegativeEnum>(SByteNegativeEnum.Negative));
    }

    [Test]
    public void EndpointId_ShortBackedPositiveValue_ConvertsToUlong()
    {
        var metadata = new WiseAuthMetadata<ShortBackedEnum>(ShortBackedEnum.Value);

        metadata.EndpointId.ShouldBe(5UL);
    }

    [Test]
    public void EndpointId_ShortBackedNegativeValue_ThrowsOverflowException()
    {
        Should.Throw<OverflowException>(() => new WiseAuthMetadata<ShortNegativeEnum>(ShortNegativeEnum.Negative));
    }

    [Test]
    public void EndpointId_UShortBackedEnumValue_ConvertsToUlong()
    {
        var metadata = new WiseAuthMetadata<UShortBackedEnum>(UShortBackedEnum.Value);

        metadata.EndpointId.ShouldBe(5UL);
    }

    [Test]
    public void EndpointId_UIntBackedEnumValue_ConvertsToUlong()
    {
        var metadata = new WiseAuthMetadata<UIntBackedEnum>(UIntBackedEnum.Value);

        metadata.EndpointId.ShouldBe(5UL);
    }

    [Test]
    public void EndpointId_LongBackedPositiveValue_ConvertsToUlong()
    {
        var metadata = new WiseAuthMetadata<LongBackedEnum>(LongBackedEnum.Value);

        metadata.EndpointId.ShouldBe(5UL);
    }

    [Test]
    public void EndpointId_LongBackedNegativeValue_ThrowsOverflowException()
    {
        Should.Throw<OverflowException>(() => new WiseAuthMetadata<LongNegativeEnum>(LongNegativeEnum.Negative));
    }

    [Test]
    public void ClaimType_CustomAttributeOnControllerNestedEnum_ReturnsAttributeNameIgnoringConvention()
    {
        var metadata = new WiseAuthMetadata<AttributedController.Permissions>(AttributedController.Permissions.View);

        WiseAuthMetadata<AttributedController.Permissions>.ClaimType.ShouldBe("CustomClaimName");
    }

    [Test]
    public void ClaimType_NoAttributeTopLevelEnum_ReturnsCapitalizedTypeName()
    {
        var metadata = new WiseAuthMetadata<standaloneEnum>(standaloneEnum.Value);

        WiseAuthMetadata<standaloneEnum>.ClaimType.ShouldBe("StandaloneEnum");
    }

    [Test]
    public void ClaimType_NoAttributeNestedInControllerClass_StripsControllerSuffix()
    {
        var metadata = new WiseAuthMetadata<ProductController.Permissions>(ProductController.Permissions.View);

        WiseAuthMetadata<ProductController.Permissions>.ClaimType.ShouldBe("ProductPermissions");
    }

    [Test]
    public void ClaimType_NoAttributeNestedInNonControllerClass_KeepsFullClassName()
    {
        var metadata = new WiseAuthMetadata<Widget.Permissions>(Widget.Permissions.View);

        WiseAuthMetadata<Widget.Permissions>.ClaimType.ShouldBe("WidgetPermissions");
    }

    [Test]
    public void ClaimType_NoAttributeNestedTwoLevelsDeep_StripsControllerFromAllButLastSegment()
    {
        var metadata = new WiseAuthMetadata<Outer.InnerController.Permissions>(Outer.InnerController.Permissions.View);

        WiseAuthMetadata<Outer.InnerController.Permissions>.ClaimType.ShouldBe("OuterInnerPermissions");
    }

    [Test]
    public void ClaimType_NoAttributeGlobalNamespaceEnum_ReturnsCapitalizedTypeNameConsistentlyWithNamespacedEnum()
    {
        var metadata = new WiseAuthMetadata<GlobalEnum>(GlobalEnum.Value);

        WiseAuthMetadata<GlobalEnum>.ClaimType.ShouldBe("GlobalEnum");
    }

    [Test]
    public void ClaimType_NoAttributeGlobalNamespaceNestedInControllerClass_StripsControllerSuffixConsistentlyWithNamespacedEnum()
    {
        var metadata = new WiseAuthMetadata<GlobalController.Permissions>(GlobalController.Permissions.View);

        WiseAuthMetadata<GlobalController.Permissions>.ClaimType.ShouldBe("GlobalPermissions");
    }
}

public enum SampleEndpoints { Value1 = 1, Value2 = 2, Value3 = 3 }

public enum ZeroValueEnum { Zero = 0 }

public enum UlongBackedEnum : ulong { Max = ulong.MaxValue }

public enum NegativeValueEnum { Negative = -1 }

internal enum ByteBackedEnum : byte { Value = 5 }

internal enum SByteBackedEnum : sbyte { Value = 5 }

internal enum SByteNegativeEnum : sbyte { Negative = -1 }

internal enum ShortBackedEnum : short { Value = 5 }

internal enum ShortNegativeEnum : short { Negative = -1 }

internal enum UShortBackedEnum : ushort { Value = 5 }

internal enum UIntBackedEnum : uint { Value = 5 }

internal enum LongBackedEnum : long { Value = 5 }

internal enum LongNegativeEnum : long { Negative = -1 }

internal enum standaloneEnum { Value = 1 }

internal class AttributedController
{
    [ClaimType("CustomClaimName")]
    public enum Permissions { View = 1 }
}

internal class ProductController
{
    public enum Permissions { View = 1 }
}

internal class Widget
{
    public enum Permissions { View = 1 }
}

internal class Outer
{
    public class InnerController
    {
        public enum Permissions { View = 1 }
    }
}
