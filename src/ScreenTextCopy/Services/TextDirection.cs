namespace ScreenTextCopy.Services;

/// <summary>
/// Lightweight, dependency-free helpers for deciding how bidirectional text
/// should be displayed and which language it is written in.
///
/// The OCR engine returns text in *logical* order (correct). The visual
/// scrambling users see comes from rendering that text in a control whose
/// base flow direction does not match the text's own base direction. We
/// therefore pick the base direction from the text itself instead of
/// inheriting it from the (possibly Persian) UI shell.
/// </summary>
public static class TextDirection
{
    /// <summary>
    /// Returns true when the paragraph should be laid out right-to-left, using
    /// the Unicode "first strong character" heuristic (the same rule Word and
    /// browsers use for auto-direction).
    /// </summary>
    public static bool IsRightToLeft(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (char c in text)
        {
            if (IsStrongRtl(c))
                return true;
            if (IsStrongLtr(c))
                return false;
        }

        return false;
    }

    /// <summary>
    /// Best-effort source-language detection for translation, based on the
    /// dominant script. Returns an ISO code (e.g. "fa", "ar", "en", "ru").
    /// Falls back to English when no non-Latin script dominates.
    /// </summary>
    public static string DetectLanguage(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "en";

        int persianArabic = 0, cyrillic = 0, cjk = 0, hangul = 0, latin = 0;

        foreach (char c in text)
        {
            if (c is >= '؀' and <= 'ۿ' or >= 'ݐ' and <= 'ݿ' or >= 'ﭐ' and <= '﷿' or >= 'ﹰ' and <= '﻿')
                persianArabic++;
            else if (c is >= 'Ѐ' and <= 'ӿ')
                cyrillic++;
            else if (c is >= '一' and <= '鿿' or >= '぀' and <= 'ヿ')
                cjk++;
            else if (c is >= '가' and <= '힣')
                hangul++;
            else if (char.IsLetter(c) && c < 0x0250)
                latin++;
        }

        int max = Math.Max(persianArabic, Math.Max(cyrillic, Math.Max(cjk, Math.Max(hangul, latin))));
        if (max == 0)
            return "en";

        if (max == persianArabic)
            return ContainsPersianOnlyLetters(text) ? "fa" : "ar";
        if (max == cyrillic)
            return "ru";
        if (max == cjk)
            return "zh";
        if (max == hangul)
            return "ko";
        return "en";
    }

    /// <summary>
    /// Distinguishes Persian from Arabic by the presence of Persian-specific
    /// letters (پ چ ژ گ ک ی) that do not appear in standard Arabic.
    /// </summary>
    private static bool ContainsPersianOnlyLetters(string text)
    {
        foreach (char c in text)
        {
            if (c is 'پ' or 'چ' or 'ژ' or 'گ' or 'ک' or 'ی' or '۰' or '۱'
                or '۲' or '۳' or '۴' or '۵' or '۶' or '۷' or '۸' or '۹')
                return true;
        }
        return false;
    }

    private static bool IsStrongRtl(char c)
        // Hebrew, Arabic, Persian, and their presentation forms.
        => c is >= '֐' and <= '׿'
              or >= '؀' and <= 'ۿ'
              or >= '܀' and <= '޿'
              or >= 'ݐ' and <= 'ݿ'
              or >= 'יִ' and <= '﷿'
              or >= 'ﹰ' and <= '﻿';

    private static bool IsStrongLtr(char c)
        // Latin (incl. accented), Greek, Cyrillic, CJK, Hangul: any strong
        // left-to-right letter below the RTL Hebrew block start (U+0590).
        => char.IsLetter(c) && c < '֐';
}
