﻿namespace SMLHelper.V2.Utility
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading;

    internal class EnumTypeCache
    {
        internal int Index;
        internal string Name;
    }

    internal class EnumCacheManager<T>
    {
        internal readonly string EnumTypeName;
        internal readonly int StartingIndex;
        internal bool cacheLoaded = false;

        private List<EnumTypeCache> cacheList = new List<EnumTypeCache>();
        private readonly HashSet<int> BannedIDs;
        private readonly int LargestBannedID;

        private static bool fileInUse = false;

        internal Dictionary<T, EnumTypeCache> customEnumTypes = new Dictionary<T, EnumTypeCache>();

        internal EnumCacheManager(string enumTypeName, int startingIndex, IEnumerable<int> bannedIDs)
        {
            EnumTypeName = enumTypeName;
            StartingIndex = startingIndex;

            int largestID = 0;
            BannedIDs = new HashSet<int>();
            foreach (int id in bannedIDs)
            {
                BannedIDs.Add(id);
                largestID = Math.Max(largestID, id);
            }

            LargestBannedID = largestID;
        }

        #region Caching

        private string GetCacheDirectoryPath()
        {
            var saveDir = @"./QMods/Modding Helper/" + $"{EnumTypeName}Cache";

            if (!Directory.Exists(saveDir))
                Directory.CreateDirectory(saveDir);

            return saveDir;
        }

        private string GetCachePath()
        {
            return Path.Combine(GetCacheDirectoryPath(), $"{EnumTypeName}Cache.txt");
        }

        internal void LoadCache()
        {
            if (cacheLoaded) return;

            var savePathDir = GetCachePath();

            if (!File.Exists(savePathDir))
            {
                SaveCache();
                return;
            }

            WaitForFile();

            fileInUse = true;
            var allText = File.ReadAllLines(savePathDir);
            fileInUse = false;

            foreach (var line in allText)
            {
                string[] split = line.Split(':');
                var name = split[0];
                var index = split[1];

                var cache = new EnumTypeCache()
                {
                    Name = name,
                    Index = int.Parse(index)
                };

                cacheList.Add(cache);
            }

            Logger.Log($"Loaded {EnumTypeName} Cache!");

            cacheLoaded = true;
        }

        internal void SaveCache()
        {
            var savePathDir = GetCachePath();
            var stringBuilder = new StringBuilder();

            foreach (var entry in customEnumTypes)
            {
                cacheList.Add(entry.Value);

                stringBuilder.AppendLine(string.Format("{0}:{1}", entry.Value.Name, entry.Value.Index));
            }

            WaitForFile();

            fileInUse = true;
            File.WriteAllText(savePathDir, stringBuilder.ToString());
            fileInUse = false;
        }

        internal EnumTypeCache GetCacheForTypeName(string name)
        {
            LoadCache();

            foreach (var cache in cacheList)
            {
                if (cache.Name == name)
                    return cache;
            }

            return null;
        }

        internal EnumTypeCache GetCacheForIndex(int index)
        {
            LoadCache();

            foreach (var cache in cacheList)
            {
                if (cache.Index == index)
                    return cache;
            }

            return null;
        }

        internal int GetLargestIndexFromCache()
        {
            LoadCache();

            var index = StartingIndex;

            foreach (var cache in cacheList)
            {
                if (cache.Index > index)
                    index = cache.Index;
            }

            return index;
        }

        internal int GetNextFreeIndex()
        {
            LoadCache();

            var freeIndex = GetLargestIndexFromCache() + 1;

            if (BannedIDs != null && BannedIDs.Contains(freeIndex))
            {
                freeIndex = LargestBannedID + 1;
            }

            return freeIndex;
        }

        internal bool IsIndexValid(int index)
        {
            LoadCache();

            var count = 0;

            foreach (var cache in cacheList)
            {
                if (cache.Index == index)
                    count++;
            }

            return count >= 2 && (!BannedIDs?.Contains(index) ?? false);
        }

        private void WaitForFile()
        {
            if (!fileInUse)
                return;

            const short maxWaits = 10;

            short waits = 0;
            while (fileInUse && waits < maxWaits)
            {
                waits++;
                Logger.Log($"{EnumTypeName} cache file in use. Wait cycle {waits}.");                
                Thread.Sleep(100);
            }

            if (!fileInUse)
                return;

            if (waits == maxWaits)
                Logger.Log($"Maximum wait time exceeded for {EnumTypeName} cache file.");
        }

        #endregion
    }
}
