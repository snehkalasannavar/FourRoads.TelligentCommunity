﻿using System.Reflection;
using System.Web;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("FourRoads.TelligentCommunity92.RenderingHelper")]
[assembly: AssemblyDescription("Allows other plugins access to the rendering pipeline after all content is rendered")]


[assembly: PreApplicationStartMethod(typeof(FourRoads.TelligentCommunity.RenderingHelper.RenderingHelperApplicationStart), "Start")]