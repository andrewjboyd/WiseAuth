using Shouldly;

namespace WiseAuth.UnitTests;

public class EnumPowerOfTwoValidatorTests
{
    [Test]
    public void IsValid_OneValueEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidOneValueEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_ManyValueEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidMultiValueEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_ManyUnorderedEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidUnorderedMultiValueEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_BasicEnum_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidBasicEnum>();

        result.ShouldBeFalse();
    }

    [Test]
    public void IsValid_DuplicateEnumValues_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidDuplicateValueEnum>();

        result.ShouldBeFalse();
    }

    [Test]
    public void IsValid_LessThanZeroValues_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidLessThanZeroEnum>();

        result.ShouldBeFalse();
    }

    [Test]
    public void IsValid_FirstValueIsTwo_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidFirstValueNotOne>();

        result.ShouldBeFalse();
    }

    [Test]
    public void IsValid_GapInPowersOfTwo_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidGapEnum>();

        result.ShouldBeFalse();
    }

    [Test]
    public void IsValid_EmptyEnum_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidEmptyEnum>();

        result.ShouldBeFalse();
    }

    [Test]
    public void IsValid_ByteBackedValidEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidByteEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_SByteBackedValidEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidSByteEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_ShortBackedValidEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidShortEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_UShortBackedValidEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidUShortEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_UIntBackedValidEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidUIntEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_LongBackedValidEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidLongEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_ULongBackedValidEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidULongEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_ULongBackedFullRangeEnum_IsValid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<ValidULongFullRangeEnum>();

        result.ShouldBeTrue();
    }

    [Test]
    public void IsValid_SByteBackedNegativeEnum_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidSByteNegativeEnum>();

        result.ShouldBeFalse();
    }

    [Test]
    public void IsValid_ShortBackedNegativeEnum_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidShortNegativeEnum>();

        result.ShouldBeFalse();
    }

    [Test]
    public void IsValid_LongBackedNegativeEnum_IsInvalid()
    {
        var result = EnumPowerOfTwoValidator.IsValid<InvalidLongNegativeEnum>();

        result.ShouldBeFalse();
    }

    private enum InvalidBasicEnum { ThisIsInvalid };
    private enum InvalidDuplicateValueEnum { One = 1, Two = 1, };

    private enum InvalidEmptyEnum { };

    private enum InvalidLessThanZeroEnum { One = -1, Two = 2 };

    private enum InvalidFirstValueNotOne { One = 2 };

    private enum InvalidGapEnum { One = 1, Two = 2, Eight = 8 };

    private enum ValidOneValueEnum { One = 1 };

    private enum ValidMultiValueEnum { Value1 = 1, Value2 = 2, Value4 = 4, Value8 = 8, Value16 = 16, Value32 = 32 };

    private enum ValidUnorderedMultiValueEnum { Value16 = 16, Value2 = 2, Value1 = 1, Value32 = 32, Value8 = 8, Value4 = 4, };

    private enum ValidByteEnum : byte { Value1 = 1, Value2 = 2, Value4 = 4, Value8 = 8 };

    private enum ValidSByteEnum : sbyte { Value1 = 1, Value2 = 2, Value4 = 4, Value8 = 8 };

    private enum ValidShortEnum : short { Value1 = 1, Value2 = 2, Value4 = 4, Value8 = 8 };

    private enum ValidUShortEnum : ushort { Value1 = 1, Value2 = 2, Value4 = 4, Value8 = 8 };

    private enum ValidUIntEnum : uint { Value1 = 1, Value2 = 2, Value4 = 4, Value8 = 8 };

    private enum ValidLongEnum : long { Value1 = 1, Value2 = 2, Value4 = 4, Value8 = 8 };

    private enum ValidULongEnum : ulong { Value1 = 1, Value2 = 2, Value4 = 4, Value8 = 8 };

    // Covers the full ulong range including 2^63 (9,223,372,036,854,775,808), which is
    // one greater than Int64.MaxValue and previously crashed IsValid<T>() with an
    // unhandled OverflowException instead of validating correctly.
    private enum ValidULongFullRangeEnum : ulong
    {
        Bit0 = 1,
        Bit1 = 2,
        Bit2 = 4,
        Bit3 = 8,
        Bit4 = 16,
        Bit5 = 32,
        Bit6 = 64,
        Bit7 = 128,
        Bit8 = 256,
        Bit9 = 512,
        Bit10 = 1024,
        Bit11 = 2048,
        Bit12 = 4096,
        Bit13 = 8192,
        Bit14 = 16384,
        Bit15 = 32768,
        Bit16 = 65536,
        Bit17 = 131072,
        Bit18 = 262144,
        Bit19 = 524288,
        Bit20 = 1048576,
        Bit21 = 2097152,
        Bit22 = 4194304,
        Bit23 = 8388608,
        Bit24 = 16777216,
        Bit25 = 33554432,
        Bit26 = 67108864,
        Bit27 = 134217728,
        Bit28 = 268435456,
        Bit29 = 536870912,
        Bit30 = 1073741824,
        Bit31 = 2147483648,
        Bit32 = 4294967296,
        Bit33 = 8589934592,
        Bit34 = 17179869184,
        Bit35 = 34359738368,
        Bit36 = 68719476736,
        Bit37 = 137438953472,
        Bit38 = 274877906944,
        Bit39 = 549755813888,
        Bit40 = 1099511627776,
        Bit41 = 2199023255552,
        Bit42 = 4398046511104,
        Bit43 = 8796093022208,
        Bit44 = 17592186044416,
        Bit45 = 35184372088832,
        Bit46 = 70368744177664,
        Bit47 = 140737488355328,
        Bit48 = 281474976710656,
        Bit49 = 562949953421312,
        Bit50 = 1125899906842624,
        Bit51 = 2251799813685248,
        Bit52 = 4503599627370496,
        Bit53 = 9007199254740992,
        Bit54 = 18014398509481984,
        Bit55 = 36028797018963968,
        Bit56 = 72057594037927936,
        Bit57 = 144115188075855872,
        Bit58 = 288230376151711744,
        Bit59 = 576460752303423488,
        Bit60 = 1152921504606846976,
        Bit61 = 2305843009213693952,
        Bit62 = 4611686018427387904,
        Bit63 = 9223372036854775808,
    };

    private enum InvalidSByteNegativeEnum : sbyte { One = -1, Two = 2 };

    private enum InvalidShortNegativeEnum : short { One = -1, Two = 2 };

    private enum InvalidLongNegativeEnum : long { One = -1, Two = 2 };
}