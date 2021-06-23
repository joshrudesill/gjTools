using Rhino.PlugIns;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// Plug-in Description Attributes - all of these are optional.
// These will show in Rhino's option dialog, in the tab Plug-ins.
[assembly: PlugInDescription(DescriptionType.Address, "845 Minnehaha Ave E. St.Paul, MN 55106")]
[assembly: PlugInDescription(DescriptionType.Country, "United States of America")]
[assembly: PlugInDescription(DescriptionType.Email, "CADOEM@vomela.com")]
[assembly: PlugInDescription(DescriptionType.Phone, "N/A")]
[assembly: PlugInDescription(DescriptionType.Fax, "N/A")]
[assembly: PlugInDescription(DescriptionType.Organization, "The Vomela Companies")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "N/A")]
[assembly: PlugInDescription(DescriptionType.WebSite, "vomela.com")]

// Icons should be Windows .ico files and contain 32-bit images in the following sizes: 16, 24, 32, 48, and 256.
[assembly: PlugInDescription(DescriptionType.Icon, "gjTools.EmbeddedResources.plugin-utility.ico")]

// The following GUID is for the ID of the typelib if this project is exposed to COM
// This will also be the Guid of the Rhino plug-in
[assembly: Guid("9119D947-B1FF-48B6-9E92-B3D04CACFD69")]
