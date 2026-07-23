namespace WiseAuth;

internal static class EnumPowerOfTwoValidator
{
    public static bool IsValid<T>()
        where T : struct, Enum
    {
        ulong[] numbers;
        try
        {
            // Convert.ToUInt64 throws OverflowException for negative values, which
            // doubles as the "greater than 0" check and avoids detouring through
            // Int64 (which can't represent a ulong-backed value of 2^63 or higher).
            numbers = Enum.GetValues<T>().Select(f => Convert.ToUInt64(f)).OrderBy(v => v).ToArray();
        }
        catch (OverflowException)
        {
            return false;
        }

        // Ensure there are values, the first value is 1, that there are no duplicates
        // and that every value is double the previous one with no gaps in the powers of two
        return numbers.Length > 0
               && numbers[0] == 1
               && numbers.Distinct().Count() == numbers.Length
               && IsSequentialPowersOfTwo(numbers);
    }

    private static bool IsSequentialPowersOfTwo(ulong[] arr)
    {
        for (var i = 1; i < arr.Length; i++)
        {
            if (arr[i] != arr[i - 1] * 2)
                return false;
        }
        return true;
    }
}
