namespace ValheimGuide.Data
{
    public static class BiomeOrder
    {
        public static int FromStageId(string stageId)
        {
            if (string.IsNullOrEmpty(stageId)) return 80;

            string id = stageId.ToLowerInvariant();
            if (id.Contains("meadows")) return 0;
            if (id.Contains("blackforest")) return 10;
            if (id.Contains("swamp")) return 20;
            if (id.Contains("mountain")) return 30;
            if (id.Contains("plains")) return 40;
            if (id.Contains("mistlands")) return 50;
            if (id.Contains("ashlands")) return 60;
            if (id.Contains("deepnorth")) return 70;
            return 80;
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
