namespace ValheimGuide.Data
{
    public static class BiomeOrder
    {
        public static int FromStageId(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return 80;

            switch (stageId.ToLowerInvariant())
            {
                case "meadows": return 0;
                case "blackforest": return 10;
                case "swamp": return 20;
                case "mountain": return 30;
                case "plains": return 40;
                case "mistlands": return 50;
                case "ashlands": return 60;
                case "deepnorth": return 70;
                default: return 80;
            }
        }

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
