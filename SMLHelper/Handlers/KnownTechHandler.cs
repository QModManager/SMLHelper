﻿namespace SMLHelper.V2.Handlers
{
    using Interfaces;
    using Patchers;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// A handler class for configuring custom unlocking conditions for item blueprints.
    /// </summary>
    public class KnownTechHandler : IKnownTechHandler
    {
        private static readonly KnownTechHandler singleton = new KnownTechHandler();

        /// <summary>
        /// Main entry point for all calls to this handler.
        /// </summary>
        public static IKnownTechHandler Main => singleton;

        private KnownTechHandler()
        {
            // Hides constructor
        }

        /// <summary>
        /// Allows you to unlock a TechType on game start.
        /// </summary>
        /// <param name="techType"></param>
        public static void UnlockOnStart(TechType techType)
        {
            Main.UnlockOnStart(techType);
        }

        /// <summary>
        /// Allows you to unlock a TechType on game start.
        /// </summary>
        /// <param name="techType"></param>
        void IKnownTechHandler.UnlockOnStart(TechType techType)
        {
            KnownTechPatcher.UnlockedAtStart.Add(techType);
        }

        internal void AddAnalysisTech(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock, string UnlockMessage = "NotificationBlueprintUnlocked", FMODAsset UnlockSound = null, UnityEngine.Sprite UnlockSprite = null)
        {
            if (KnownTechPatcher.AnalysisTech.TryGetValue(techTypeToBeAnalysed, out KnownTech.AnalysisTech existingEntry))
            {
                existingEntry.unlockMessage = existingEntry.unlockMessage ?? UnlockMessage;
                existingEntry.unlockSound = existingEntry.unlockSound ?? UnlockSound;
                existingEntry.unlockPopup = existingEntry.unlockPopup ?? UnlockSprite;
                existingEntry.unlockTechTypes.AddRange(techTypesToUnlock);
            }
            else
            {
                KnownTechPatcher.AnalysisTech.Add(techTypeToBeAnalysed, new KnownTech.AnalysisTech()
                {
                    techType = techTypeToBeAnalysed,
                    unlockMessage = UnlockMessage,
                    unlockSound = UnlockSound,
                    unlockPopup = UnlockSprite,
                    unlockTechTypes = new List<TechType>(techTypesToUnlock)
                });
            }
        }

        /// <summary>
        /// Allows you to define which TechTypes are unlocked when a certain TechType is unlocked, i.e., "analysed".
        /// If there is already an exisitng AnalysisTech entry for a TechType, all the TechTypes in "techTypesToUnlock" will be
        /// added to the existing AnalysisTech entry unlocks.
        /// </summary>
        /// <param name="techTypeToBeAnalysed">This TechType is the criteria for all of the "unlock TechTypes"; when this TechType is unlocked, so are all the ones in that list</param>
        /// <param name="techTypesToUnlock">The TechTypes that will be unlocked when "techTypeToSet" is unlocked.</param>
        /// <param name="UnlockMessage">The message that shows up on the right when the blueprint is unlocked. </param>
        /// <param name="UnlockSound">The sound that plays when you unlock the blueprint.</param>
        /// <param name="UnlockSprite">The sprite that shows up when you unlock the blueprint.</param>
        public static void SetAnalysisTechEntry(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock, string UnlockMessage = "NotificationBlueprintUnlocked", FMODAsset UnlockSound = null, UnityEngine.Sprite UnlockSprite = null)
        {
            singleton.AddAnalysisTech(techTypeToBeAnalysed, techTypesToUnlock, UnlockMessage, UnlockSound, UnlockSprite);
        }

        /// <summary>
        /// Allows you to define which TechTypes are unlocked when a certain TechType is unlocked, i.e., "analysed".
        /// If there is already an exisitng AnalysisTech entry for a TechType, all the TechTypes in "techTypesToUnlock" will be
        /// added to the existing AnalysisTech entry unlocks.
        /// </summary>
        /// <param name="techTypeToBeAnalysed">This TechType is the criteria for all of the "unlock TechTypes"; when this TechType is unlocked, so are all the ones in that list</param>
        /// <param name="techTypesToUnlock">The TechTypes that will be unlocked when "techTypeToSet" is unlocked.</param>
        void IKnownTechHandler.SetAnalysisTechEntry(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock)
        {
            singleton.AddAnalysisTech(techTypeToBeAnalysed, techTypesToUnlock);
        }

        /// <summary>
        /// Allows you to define which TechTypes are unlocked when a certain TechType is unlocked, i.e., "analysed".
        /// If there is already an exisitng AnalysisTech entry for a TechType, all the TechTypes in "techTypesToUnlock" will be
        /// added to the existing AnalysisTech entry unlocks.
        /// </summary>
        /// <param name="techTypeToBeAnalysed">This TechType is the criteria for all of the "unlock TechTypes"; when this TechType is unlocked, so are all the ones in that list</param>
        /// <param name="techTypesToUnlock">The TechTypes that will be unlocked when "techTypeToSet" is unlocked.</param>
        /// <param name="UnlockMessage">The message that shows up on the right when the blueprint is unlocked. </param>
        void IKnownTechHandler.SetAnalysisTechEntry(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock, string UnlockMessage)
        {
            singleton.AddAnalysisTech(techTypeToBeAnalysed, techTypesToUnlock, UnlockMessage);
        }

        /// <summary>
        /// Allows you to define which TechTypes are unlocked when a certain TechType is unlocked, i.e., "analysed".
        /// If there is already an exisitng AnalysisTech entry for a TechType, all the TechTypes in "techTypesToUnlock" will be
        /// added to the existing AnalysisTech entry unlocks.
        /// </summary>
        /// <param name="techTypeToBeAnalysed">This TechType is the criteria for all of the "unlock TechTypes"; when this TechType is unlocked, so are all the ones in that list</param>
        /// <param name="techTypesToUnlock">The TechTypes that will be unlocked when "techTypeToSet" is unlocked.</param>
        /// <param name="UnlockSound">The sound that plays when you unlock the blueprint.</param>
        void IKnownTechHandler.SetAnalysisTechEntry(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock, FMODAsset UnlockSound)
        {
            singleton.AddAnalysisTech(techTypeToBeAnalysed, techTypesToUnlock, "NotificationBlueprintUnlocked", UnlockSound);
        }

        /// <summary>
        /// Allows you to define which TechTypes are unlocked when a certain TechType is unlocked, i.e., "analysed".
        /// If there is already an exisitng AnalysisTech entry for a TechType, all the TechTypes in "techTypesToUnlock" will be
        /// added to the existing AnalysisTech entry unlocks.
        /// </summary>
        /// <param name="techTypeToBeAnalysed">This TechType is the criteria for all of the "unlock TechTypes"; when this TechType is unlocked, so are all the ones in that list</param>
        /// <param name="techTypesToUnlock">The TechTypes that will be unlocked when "techTypeToSet" is unlocked.</param>
        /// <param name="UnlockSprite">The sprite that shows up when you unlock the blueprint.</param>
        void IKnownTechHandler.SetAnalysisTechEntry(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock, Sprite UnlockSprite)
        {
            singleton.AddAnalysisTech(techTypeToBeAnalysed, techTypesToUnlock, "NotificationBlueprintUnlocked", null, UnlockSprite);
        }

        /// <summary>
        /// Allows you to define which TechTypes are unlocked when a certain TechType is unlocked, i.e., "analysed".
        /// If there is already an exisitng AnalysisTech entry for a TechType, all the TechTypes in "techTypesToUnlock" will be
        /// added to the existing AnalysisTech entry unlocks.
        /// </summary>
        /// <param name="techTypeToBeAnalysed">This TechType is the criteria for all of the "unlock TechTypes"; when this TechType is unlocked, so are all the ones in that list</param>
        /// <param name="techTypesToUnlock">The TechTypes that will be unlocked when "techTypeToSet" is unlocked.</param>
        /// <param name="UnlockMessage">The message that shows up on the right when the blueprint is unlocked. </param>
        /// <param name="UnlockSound">The sound that plays when you unlock the blueprint.</param>
        void IKnownTechHandler.SetAnalysisTechEntry(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock, string UnlockMessage, FMODAsset UnlockSound)
        {
            singleton.AddAnalysisTech(techTypeToBeAnalysed, techTypesToUnlock, UnlockMessage, UnlockSound, null);
        }

        /// <summary>
        /// Allows you to define which TechTypes are unlocked when a certain TechType is unlocked, i.e., "analysed".
        /// If there is already an exisitng AnalysisTech entry for a TechType, all the TechTypes in "techTypesToUnlock" will be
        /// added to the existing AnalysisTech entry unlocks.
        /// </summary>
        /// <param name="techTypeToBeAnalysed">This TechType is the criteria for all of the "unlock TechTypes"; when this TechType is unlocked, so are all the ones in that list</param>
        /// <param name="techTypesToUnlock">The TechTypes that will be unlocked when "techTypeToSet" is unlocked.</param>
        /// <param name="UnlockMessage">The message that shows up on the right when the blueprint is unlocked. </param>
        /// <param name="UnlockSprite">The sprite that shows up when you unlock the blueprint.</param>
        void IKnownTechHandler.SetAnalysisTechEntry(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock, string UnlockMessage, Sprite UnlockSprite)
        {
            singleton.AddAnalysisTech(techTypeToBeAnalysed, techTypesToUnlock, UnlockMessage, null, UnlockSprite);
        }

        /// <summary>
        /// Allows you to define which TechTypes are unlocked when a certain TechType is unlocked, i.e., "analysed".
        /// If there is already an exisitng AnalysisTech entry for a TechType, all the TechTypes in "techTypesToUnlock" will be
        /// added to the existing AnalysisTech entry unlocks.
        /// </summary>
        /// <param name="techTypeToBeAnalysed">This TechType is the criteria for all of the "unlock TechTypes"; when this TechType is unlocked, so are all the ones in that list</param>
        /// <param name="techTypesToUnlock">The TechTypes that will be unlocked when "techTypeToSet" is unlocked.</param>
        /// <param name="UnlockSound">The sound that plays when you unlock the blueprint.</param>
        /// <param name="UnlockSprite">The sprite that shows up when you unlock the blueprint.</param>
        void IKnownTechHandler.SetAnalysisTechEntry(TechType techTypeToBeAnalysed, IEnumerable<TechType> techTypesToUnlock, FMODAsset UnlockSound, Sprite UnlockSprite)
        {
            singleton.AddAnalysisTech(techTypeToBeAnalysed, techTypesToUnlock, "NotificationBlueprintUnlocked", UnlockSound, UnlockSprite);
        }
    }
}
