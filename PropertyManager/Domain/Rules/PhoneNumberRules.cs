namespace PropertyManager.Domain.Rules;

public static class PhoneNumberRules
{
    public static bool IsValid(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        var trimmed = phoneNumber.Trim();
        var index = trimmed.StartsWith('+') ? 1 : 0;
        var digitCount = 0;

        for (; index < trimmed.Length; index++)
        {
            var character = trimmed[index];
            if (char.IsDigit(character))
            {
                digitCount++;
            }
            else if (char.IsWhiteSpace(character) ||
                     character is '-' or '.' or '(' or ')')
            {
                continue;
            }
            else
            {
                return false;
            }
        }

        return digitCount is >= 7 and <= 15;
    }
}
