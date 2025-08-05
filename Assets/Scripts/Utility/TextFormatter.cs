using System.Globalization;
using UnityEngine;

public static class TextFormatter
{
    /// <summary>
    /// Converts snake_case to Title Case with spaces
    /// Example: "big_cats" -> "Big Cats"
    /// Example: "hoofed_mammals" -> "Hoofed Mammals"
    /// </summary>
    public static string FormatSuitName(string snakeCaseText)
    {
        if (string.IsNullOrEmpty(snakeCaseText))
            return string.Empty;

        // Replace underscores with spaces and convert to Title Case
        string spacedText = snakeCaseText.Replace('_', ' ');
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(spacedText.ToLower());
    }

    /// <summary>
    /// Converts lowercase to Title Case (first letter capitalized)
    /// Example: "lion" -> "Lion"
    /// Example: "tiger" -> "Tiger"
    /// Handles special cases like "clown-fish" -> "Clown-Fish"
    /// </summary>
    public static string FormatCardName(string cardName)
    {
        if (string.IsNullOrEmpty(cardName))
            return string.Empty;

        // Handle hyphenated names
        if (cardName.Contains("-"))
        {
            string[] parts = cardName.Split('-');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
                }
            }
            return string.Join("-", parts);
        }

        // Handle parentheses cases like "snake(-boa" -> "Snake(-Boa"
        if (cardName.Contains("("))
        {
            string[] parts = cardName.Split('(');
            if (parts.Length == 2)
            {
                string firstPart = FormatSingleWord(parts[0]);
                string secondPart = FormatSingleWord(parts[1]);
                return firstPart + "(" + secondPart;
            }
        }

        // Standard single word formatting
        return FormatSingleWord(cardName);
    }

    /// <summary>
    /// Formats a single word to Title Case
    /// </summary>
    private static string FormatSingleWord(string word)
    {
        if (string.IsNullOrEmpty(word))
            return string.Empty;

        return char.ToUpper(word[0]) + word.Substring(1).ToLower();
    }

    /// <summary>
    /// Formats hint text to proper sentence case
    /// Example: "the king of the jungle" -> "The king of the jungle"
    /// </summary>
    public static string FormatHint(string hint)
    {
        if (string.IsNullOrEmpty(hint))
            return string.Empty;

        // Capitalize first letter, keep rest as is
        return char.ToUpper(hint[0]) + hint.Substring(1);
    }
}