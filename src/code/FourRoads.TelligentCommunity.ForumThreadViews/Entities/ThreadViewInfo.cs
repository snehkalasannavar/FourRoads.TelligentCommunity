using System;

namespace FourRoads.TelligentCommunity.ForumThreadViews.Entities
{
    [Serializable]
    public class ThreadViewInfo
    {
        public Guid ApplicationId { get; set; }
        public Guid ContentId { get; set; }
    }
}