using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EPEK.Models
{
    public class CriteriaItem
    {
        public double numeric { get; set; }
        public string unit { get; set; }

        public CriteriaItem()
        {
            numeric = -1.0;
            unit = "";
        }

        public CriteriaItem(string criteriaString)
        {
            if (criteriaString.Trim().EndsWith("%"))
            {
                unit = "%";
                numeric = Convert.ToDouble(criteriaString.Substring(0, criteriaString.Length - 1));
            }
            else if (criteriaString.Trim().ToLower().EndsWith("cgy"))
            {
                unit = "cGy";
                numeric= Convert.ToDouble(criteriaString.Substring(0, criteriaString.Length - 3));
            }
            else if (criteriaString.Trim().ToLower().EndsWith("cc"))
            {
                unit = "cc";
                numeric = Convert.ToDouble(criteriaString.Substring(0, criteriaString.Length - 2));
            }
            else
            {
                unit = "";
                numeric = Convert.ToDouble(criteriaString.Trim());
            }
        }
    }

    public class Criteria
    {
        public bool has_major { get; }
        public bool has_minor { get; }
        public CriteriaItem major { get; }
        public CriteriaItem minor { get; }

        public Criteria()
        {
            has_major = false;
            has_minor = false;
            major = new CriteriaItem();
            minor = new CriteriaItem();
        }
        public Criteria(CriteriaItem theMinor)
        {
            has_major = false;
            has_minor = true;
            major = new CriteriaItem();
            minor = theMinor;
        }

        public Criteria(CriteriaItem theMinor, CriteriaItem theMajor)
        {
            has_major = true;
            has_minor = true;
            major = theMajor;
            minor = theMinor;
        }

        public Criteria(string strInput)
        {
            has_major = false;
            has_minor = false;
            major = new CriteriaItem();
            minor = new CriteriaItem();
            //
            // Input format: ##, minor(##), major(##), or minormajor(##, ##).
            //

            strInput = strInput.Trim();
            if (strInput.StartsWith("[") && strInput.EndsWith("]"))
            {
                strInput = "minormajor(" + strInput.Substring(1, strInput.Length - 2) + ")";
            }

            string[] parts = strInput.Trim().Split(new char[] { '(', ',', ')' });

            if (parts[0].ToLower() == "minor")
            {
                has_minor = true;
                minor = new CriteriaItem(parts[1]);
            }
            else if (parts[0].ToLower() == "major")
            {
                has_major = true;
                major = new CriteriaItem(parts[1]);
            }
            else if (parts[0].ToLower() == "minormajor")
            {
                has_minor = true;
                minor = new CriteriaItem(parts[1]);
                has_major = true;
                major = new CriteriaItem(parts[2]);
            }
            else if (parts[0].ToLower() == "majorminor")
            {
                has_minor = true;
                minor = new CriteriaItem(parts[2]);
                has_major = true;
                major = new CriteriaItem(parts[1]);
            }
            else
            {
                has_major = true;
                major = new CriteriaItem(parts[0]);
            }
        }
        
        public string ToString(string digitFormat="")
        {
            if (digitFormat.Length > 0)
            {
                digitFormat = ":" + digitFormat;
            }

            if (has_major && has_minor)
            {
                return String.Format("[{0"+digitFormat+"}{1}, {2"+digitFormat+"}{3}]", minor.numeric, minor.unit, major.numeric, major.unit);
            }
            else if (has_minor)
            {
                return String.Format("{0"+digitFormat+"}{1}", minor.numeric, minor.unit);
            }
            else if (has_major)
            {
                return String.Format("{0"+digitFormat+"}{1}", major.numeric, major.unit);
            }

            return "";
        }
    }
}
