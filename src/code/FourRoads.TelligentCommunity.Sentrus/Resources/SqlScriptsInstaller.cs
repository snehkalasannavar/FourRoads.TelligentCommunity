﻿using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.Sentrus.Resources
{
    public class SqlScriptsInstaller : FourRoads.Common.TelligentCommunity.Plugins.Base.SqlScriptsInstaller
    {
        protected override string ProjectName
        {
            get { return "LastLogin"; }
        }

        protected override string BaseResourcePath
        {
            get { return "FourRoads.TelligentCommunity.Sentrus.Resources."; }
        }

        protected override EmbeddedResourcesBase EmbeddedResources
        {
            get { return new EmbeddedResources(); }
        }
    }
}
