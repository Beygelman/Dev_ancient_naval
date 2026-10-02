using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;

namespace DevAncientNaval.Presentation.UI;

/// <summary>English-origin message catalog, including complete-sentence templates for already formatted game messages.</summary>
internal static class LocalizedMessages
{
    private static readonly Dictionary<string, Catalog> Catalogs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex Placeholder = new(@"\{(\d+)\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Plural = new(@"\[\[plural:(\d+):([^|]*)\|([^|]*)\|([^\]]*)\]\]",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static bool _initialized;

    internal static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        foreach (var locale in new[] { "uk", "nl" })
        {
            string path = $"res://data/localization/{locale}.json";
            if (Godot.FileAccess.FileExists(path)) LoadCatalog(locale, Godot.FileAccess.GetFileAsString(path));
        }
    }

    internal static void LoadCatalog(string locale, string json)
    {
        var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new ArgumentException("A localization catalog must be a JSON object.");
        if (entries.Any(e => string.IsNullOrEmpty(e.Key) || e.Value is null ||
            !Placeholder.Matches(e.Key).Select(m => m.Value).ToHashSet().SetEquals(Placeholder.Matches(e.Value).Select(m => m.Value))))
            throw new ArgumentException("A translated message must retain its original placeholders.");
        Catalogs[Normalize(locale)] = new Catalog(entries, Normalize(locale));
    }

    internal static string Translate(string source, string locale)
    {
        if (string.IsNullOrEmpty(source)) return source;
        Initialize();
        return Catalogs.TryGetValue(Normalize(locale), out var catalog) ? catalog.Translate(source, 0) : source;
    }

    private static string Normalize(string locale) => (locale ?? "en").Split('-', '_')[0].ToLowerInvariant();

    private sealed class Catalog
    {
        private readonly Dictionary<string, string> _exact;
        private readonly Dictionary<string, string> _results = new(StringComparer.Ordinal);
        private readonly Template[] _templates;
        private readonly string _locale;

        internal Catalog(Dictionary<string, string> entries, string locale)
        {
            _exact = entries;
            _locale = locale;
            _templates = entries.Where(e => Placeholder.IsMatch(e.Key))
                .Select(e => new Template(e.Key, e.Value))
                .OrderByDescending(t => t.LiteralLength).ToArray();
        }

        internal string Translate(string source, int depth)
        {
            if (_exact.TryGetValue(source, out var exact)) return exact;
            if (_results.TryGetValue(source, out var cached)) return cached;
            if (depth > 4) return source;
            foreach (var template in _templates)
            {
                if (template.Prefix.Length > 0 && !source.StartsWith(template.Prefix, StringComparison.Ordinal)) continue;
                if (template.Suffix.Length > 0 && !source.EndsWith(template.Suffix, StringComparison.Ordinal)) continue;
                Match match;
                try { match = template.Pattern.Match(source); }
                catch (RegexMatchTimeoutException) { continue; }
                if (!match.Success) continue;
                string translated = Placeholder.Replace(template.Target, p =>
                {
                    var value = match.Groups["p" + p.Groups[1].Value].Value;
                    return value == source ? value : Translate(value, depth + 1);
                });
                translated = Plural.Replace(translated, p =>
                {
                    string value = match.Groups["p" + p.Groups[1].Value].Value;
                    int form = 2;
                    if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long number))
                    {
                        long last = Math.Abs(number % 10), lastTwo = Math.Abs(number % 100);
                        form = _locale == "nl" ? number == 1 ? 0 : 2 :
                            last == 1 && lastTwo != 11 ? 0 : last is >= 2 and <= 4 && lastTwo is not (>= 12 and <= 14) ? 1 : 2;
                    }
                    return p.Groups[form + 2].Value;
                });
                Remember(source, translated);
                return translated;
            }
            // Compound combat messages and multiline cards consist of catalogued complete clauses.
            // Separators are structural boundaries, not word-by-word guessing.
            foreach (string separator in new[] { "\n", " · ", ". ", ", " })
            {
                if (!source.Contains(separator, StringComparison.Ordinal)) continue;
                var parts = source.Split(separator, StringSplitOptions.None);
                var translated = parts.Select(p => Translate(p, depth + 1)).ToArray();
                if (parts.Where((p, i) => p != translated[i]).Any())
                {
                    string result = string.Join(separator, translated);
                    Remember(source, result);
                    return result;
                }
            }
            Remember(source, source);
            return source;
        }

        private void Remember(string source, string translated)
        {
            if (_results.Count >= 2048) _results.Clear();
            _results[source] = translated;
        }
    }

    private sealed class Template
    {
        internal Regex Pattern { get; }
        internal string Target { get; }
        internal int LiteralLength { get; }
        internal string Prefix { get; }
        internal string Suffix { get; }
        internal Template(string source, string target)
        {
            Target = target;
            var slots = Placeholder.Matches(source);
            Prefix = source[..slots[0].Index];
            Suffix = source[(slots[^1].Index + slots[^1].Length)..];
            LiteralLength = source.Length - slots.Sum(s => s.Length);
            string pattern = "\\A";
            int end = 0;
            var seen = new HashSet<string>();
            foreach (Match slot in slots)
            {
                pattern += Regex.Escape(source[end..slot.Index]);
                string group = "p" + slot.Groups[1].Value;
                pattern += seen.Add(group) ? $"(?<{group}>.*?)" : $"\\k<{group}>";
                end = slot.Index + slot.Length;
            }
            pattern += Regex.Escape(source[end..]) + "\\z";
            Pattern = new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline,
                TimeSpan.FromMilliseconds(30));
        }
    }
}
