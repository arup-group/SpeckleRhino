using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SpeckleGrasshopper
{
    public class SpeckleGrasshopperInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Speckle";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "The Speckle Grasshopper plugin.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("1fc05d2d-cb94-4b50-9c19-20c12a345adb");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Speckle Works";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "https://speckle.works";
            }
        }

    public override string Version => "1.0.0.0";
    public override string AssemblyVersion => "1.0.*";

    public override GH_LibraryLicense License => GH_LibraryLicense.opensource;
  }
}
