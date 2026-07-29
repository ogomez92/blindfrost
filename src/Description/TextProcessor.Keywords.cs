namespace WildfrostAccessibility
{
    /// <summary>
    /// Keyword lookups over the cache declared in TextProcessor.cs: seeding it
    /// from a KeywordData, resolving a keyword id to title/body/note through
    /// addressables, and the "Title: body. Note: note" explanation text.
    /// </summary>
    public static partial class TextProcessor
    {
        /// <summary>
        /// Seed the keyword cache from a KeywordData we already hold, so callers
        /// can pass its name as an extra keyword without an addressables lookup.
        /// </summary>
        public static void CacheKeyword(KeywordData kwData)
        {
            if (kwData == null || string.IsNullOrEmpty(kwData.name)) return;
            string key = CacheKey(kwData.name);
            if (_keywordCache.ContainsKey(key)) return;

            // title/body/note are localized properties and can throw while
            // localization loads — don't cache on failure so a later call retries
            try
            {
                _keywordCache[key] = new KeywordInfo
                {
                    Title = string.IsNullOrEmpty(kwData.title) ? kwData.name : kwData.title,
                    Description = kwData.body,
                    Note = kwData.note,
                    Found = true,
                };
            }
            catch { }
        }

        /// <summary>
        /// AddressableLoader files keywords under their lower-cased name, so a
        /// lookup only lands when the id is lower-cased too. The cache follows the
        /// same rule, or an id cached from a KeywordData ("Snow") would never be
        /// found again by the tag that names it ("snow").
        /// </summary>
        private static string CacheKey(string keywordName)
        {
            return keywordName.ToLowerInvariant();
        }

        /// <summary>Safe display title for a keyword, falling back to its id.</summary>
        public static string GetKeywordTitle(KeywordData kwData)
        {
            if (kwData == null) return null;
            CacheKeyword(kwData);
            return _keywordCache.TryGetValue(CacheKey(kwData.name), out var info)
                ? info.Title
                : kwData.name;
        }

        /// <summary>
        /// "Title: body. Note: note" for a keyword's hover panel, or null when the
        /// keyword has no body text. Used by describers replicating panel content.
        /// </summary>
        public static string GetKeywordExplanation(KeywordData kwData)
        {
            if (kwData == null) return null;
            CacheKeyword(kwData);

            if (!_keywordCache.TryGetValue(CacheKey(kwData.name), out var info)
                || string.IsNullOrEmpty(info.Description))
                return null;

            string text = $"{info.Title}: {ProcessRawText(info.Description)}";
            if (!string.IsNullOrEmpty(info.Note))
                text += $". Note: {ProcessRawText(info.Note)}";
            return text;
        }

        /// <summary>
        /// Get keyword title and description, with caching.
        /// </summary>
        private static KeywordInfo GetKeywordInfo(string keywordName)
        {
            string key = CacheKey(keywordName);
            if (_keywordCache.TryGetValue(key, out var cached))
                return cached;

            var info = new KeywordInfo { Title = keywordName };

            try
            {
                var kwData = AddressableLoader.Get<KeywordData>("KeywordData", key);
                if (kwData != null)
                {
                    info.Title = kwData.title ?? keywordName;
                    info.Description = kwData.body;
                    info.Note = kwData.note;
                    info.Found = true;
                }
            }
            catch
            {
                DebugLogger.Log(DebugLogger.LogCategory.Game, "TextProcessor",
                    $"Failed to load keyword: {keywordName}");
            }

            // Only a hit is worth caching: a miss here can just mean addressables
            // were not ready yet, and a cached miss would silence that keyword's
            // icon for the rest of the session
            if (info.Found)
                _keywordCache[key] = info;
            return info;
        }

    }
}
