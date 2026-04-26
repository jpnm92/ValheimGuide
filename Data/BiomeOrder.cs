namespace ValheimGuide.Data
{
    public static class BiomeOrder
    {
        /// <summary>
        /// Returns sort order from a stage ID string (e.g. "blackforest").
        /// Normalises to the display-tier format and delegates to FromTier.
        /// </summary>
        public static int FromStageId(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return 80;

            switch (stageId.ToLowerInvariant())
            {
                case "meadows": return FromTier("Meadows");
                case "blackforest": return FromTier("Black Forest");
                case "swamp": return FromTier("Swamp");
                case "mountain": return FromTier("Mountain");
                case "plains": return FromTier("Plains");
                case "mistlands": return FromTier("Mistlands");
                case "ashlands": return FromTier("Ashlands");
                case "deepnorth": return FromTier("DeepNorth");
                default: return 80;
            }
        }

        /// <summary>
        /// Returns sort order from a display tier string (e.g. "Black Forest").
        /// Used by EncyclopediaIndex and TherzieDataGenerator sorting.
        /// </summary>
        public static int FromTier(string tier)
        {
            switch (tier)
            {
                case "Meadows": return 0;
                case "Black Forest": return 10;
                case "Swamp": return 20;
                case "Mountain": return 30;
                case "Plains": return 40;
                case "Mistlands": return 50;
                case "Ashlands": return 60;
                case "DeepNorth": return 70;
                case "Other": return 80;
                default: return 90;
            }
        }
    }
}
