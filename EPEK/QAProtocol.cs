using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

using GRCPQA.Controls;

namespace GRCPQA.EPEK
{
    public enum CriteriaViolation { No, Minor, Major };

    public class QAProtocol
    {
        // Lists for containing info readin from protocol file
        List<string> structureList = new List<string>();
        List<string> metricList = new List<string>();
        List<string> relationList = new List<string>();
        List<string> criteriaInputList = new List<string>();
        //List<Criteria> criteriaInputList = new List<Criteria>();
        //List<Criteria> criteriaList = new List<Criteria>();

        Dictionary<string, Structure> rtStructureDic = new Dictionary<string, Structure>();

        dynamic[] metricValues;
        dynamic[] metricNumericalValues;
        //bool[] meetCriteria;
        CriteriaViolation[] criteriaViolations;
        dynamic[] criteriaEntries;

        int numItems;
        
        PlanSetup plan;

        DoseValue globalDmax; // = new DoseValue(-1, DoseValue.DoseUnit.cGy);
        double ptvVolume;

        public QAProtocol() // Default Constructor
        {
            structureList.Clear();
            metricList.Clear();
            relationList.Clear();
            criteriaInputList.Clear();
            //criteriaList.Clear();

            numItems = 0;

            globalDmax = new DoseValue(-1, DoseValue.DoseUnit.cGy);
            ptvVolume = -1.0;
        }

        ~QAProtocol() // Destructor
        {
            structureList.Clear();
            metricList.Clear();
            relationList.Clear();
            criteriaInputList.Clear();
            //criteriaList.Clear();

            metricValues = null;
            metricValues = null;
            criteriaViolations = null;
            criteriaEntries = null;

            GC.Collect();
        }

        public void ReadinProtocol(Stream myStream)
        {
            StreamReader reader = new StreamReader(myStream);
            string line;
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine().Trim().ToString();
                if (line == "" || line.StartsWith("#")) continue;
                string[] parts;
                parts = line.Split(':');
                structureList.Add(parts.FirstOrDefault().Trim().ToUpper());
                parts = parts.LastOrDefault().Trim().Split(new char[] { ' ', '\t' }, 2, 
                    StringSplitOptions.RemoveEmptyEntries);
                metricList.Add(parts.FirstOrDefault().Trim());
                parts = parts.LastOrDefault().Trim().Split(new char[] { ' ', '\t' }, 2, 
                    StringSplitOptions.RemoveEmptyEntries);
                relationList.Add(parts.FirstOrDefault().Trim());
                criteriaInputList.Add(parts.LastOrDefault().Trim());
            }
            numItems = structureList.Count();
            //MessageBox.Show(numItems.ToString());

            return;
        }

        public void ApplyToPlan(PlanSetup myPlan)
        {
            plan = myPlan;
            SetupStructureDictionary();
            Structure body, ptv;

            // get global dmax and PTV volume
            if (rtStructureDic.TryGetValue("BODY", out body) && body != null)
            {
                var dvh = plan.GetDVHCumulativeData(body, DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                globalDmax = dvh.MaxDose;
                //globalDmax = plan.GetDoseAtVolume(body, 0, VolumePresentation.Relative, 
                //    DoseValuePresentation.Absolute);
            }
            //if (rtStructureDic["BODY"] != null)
            //{
            //    globalDmax = plan.GetDoseAtVolume(rtStructureDic["BODY"], 0, 
            //        VolumePresentation.Relative, DoseValuePresentation.Absolute);
            //}
            if (rtStructureDic.TryGetValue("PTV", out ptv))
            {
                ptvVolume = (ptv != null) ? ptv.Volume : -1.0;
            } // cases that ptvVolume returns -1.0 even though PTV exists. Reason to be found.
            // ptvVolume = (rtStructureDic["PTV"] != null) ? rtStructureDic["PTV"].Volume : -1;

            ParsingMetric();
            ParsingCriteria();
            PerformMetricToCriteriaComparison();

            return;
        }

        private void SetupStructureDictionary()
        {
            // Add BODY anyway and obtain global dmax: gdmax
            rtStructureDic.Add("BODY",
                plan.StructureSet.Structures.Where(s => s.Id.ToUpper().Contains("BODY")).FirstOrDefault());

            // Add the rest structures to dictionary. Could be NONE if matching structure not found.
            foreach (string rts in structureList)
            {
                if (!rtStructureDic.ContainsKey(rts))
                {
                    Structure rtStructure;
                    List<Structure> rtsList = plan.StructureSet.Structures.Where(
                        s => s.Id.ToUpper().Contains(rts)).ToList();
                    int n = rtsList.Count();
                    if (n > 1)
                    {
                        int minLength = rtsList.Min(x => x.Id.Length);
                        // If there are RT structures of the same minimum lenth, they can then
                        // be differentiated in protocol file.
                        rtStructure = rtsList.Where(x => x.Id.Length == minLength).FirstOrDefault();
                        rtStructureDic.Add(rts, rtStructure);
                    }
                    else if (n == 1)
                    {
                        rtStructureDic.Add(rts, rtsList[0]);
                    }
                    else // n==0, no matching structure found.
                    {
                        rtStructureDic.Add(rts, null);
                    }
                    //rtStructureDic.Add(rts, plan.StructureSet.Structures.Where(
                    //    s => s.Id.ToUpper().Contains(rts)).FirstOrDefault());
                }
            }
            return;
        }

        private void ParsingMetric()
        {
            // parsing and calculating metric. All results are in absolute
            metricValues = new dynamic[numItems];
            VolumePresentation volpres = new VolumePresentation();
            DoseValue dosevalue = new DoseValue(-1, DoseValue.DoseUnit.cGy);
            double value = -1;
            int length;
            for (int i = 0; i < numItems; i++)
            {
                length = metricList[i].Length;
                //MessageBox.Show(metricList[i]);
                if (metricList[i].ToLower() == "dmax" || metricList[i].ToLower() == "p_dmax")
                {
                    if (rtStructureDic[structureList[i]] is null) continue;
                    try
                    {
                        double structVolume = rtStructureDic[structureList[i]].Volume; // to verify struct exists.
                        var dvh = plan.GetDVHCumulativeData(rtStructureDic[structureList[i]], 
                            DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                        metricValues[i] = dvh.MaxDose;
                        //metricValues[i] = plan.GetDoseAtVolume(rtStructureDic[structureList[i]], 0,
                        //    VolumePresentation.Relative, DoseValuePresentation.Absolute);
                    }
                    catch
                    {   // in case structure not exist.
                        metricValues[i] = new DoseValue(-1, DoseValue.DoseUnit.cGy); // make it float.
                    }
                    if (metricList[i].ToLower() == "p_dmax") // % of struct max to presc 
                        metricValues[i] = 100.0 * metricValues[i] / plan.TotalPrescribedDose;
                }
                else if (metricList[i].ToUpper().StartsWith("V_") || // V_##% returns % relative
                    metricList[i].ToUpper().StartsWith("R_"))        // R_##% returns ratio to VPTV
                {
                    if (metricList[i].EndsWith("%"))
                    {
                        // convert percentage to absolute in cGy
                        value = Convert.ToDouble(metricList[i].Substring(2, length - 3)) 
                            * plan.TotalPrescribedDose.Dose / 100.0;
                        dosevalue = new DoseValue(value, DoseValue.DoseUnit.cGy);
                    }
                    else if (metricList[i].ToLower().EndsWith("cgy"))
                    {
                        value = Convert.ToDouble(metricList[i].Substring(2, length - 5));
                        dosevalue = new DoseValue(value, DoseValue.DoseUnit.cGy);
                    }

                    if (dosevalue.Dose > globalDmax.Dose)
                    {
                        metricValues[i] = 0;
                    }
                    else
                    {
                        DVHData dvh = plan.GetDVHCumulativeData(rtStructureDic[structureList[i]], 
                            DoseValuePresentation.Absolute, VolumePresentation.AbsoluteCm3, 0.01);
                        double ddose = (dosevalue.Unit == DoseValue.DoseUnit.cGy) ? dosevalue.Dose : dosevalue.Dose * 100.0;
                        metricValues[i] = 100 * DVHExtensions.VolumeAtDose(dvh, ddose) / DVHExtensions.VolumeAtDose(dvh, 0.0); // to %

                        //metricValues[i] = plan.GetVolumeAtDose(rtStructureDic[structureList[i]],
                        //    dosevalue, VolumePresentation.Relative);
                    }

                    if (metricList[i].ToUpper().StartsWith("R_"))  // ratio of Abs to VPTV
                    {
                        double structVolume; // if structure not exist, set volume to 0.
                        try
                        {
                            structVolume = rtStructureDic[structureList[i]].Volume;
                        }
                        catch
                        {
                            structVolume = -1.0;
                        }
                        metricValues[i] = (metricValues[i] * structVolume / (100 * ptvVolume));
                        //metricValues[i] = (metricValues[i] / ptvVolume) * 100.0; // to %
                                                                                                //* rtStructureDic[structureList[i]].Volume / (100 * ptvVolume);
                    }
                }
                else if (metricList[i].ToUpper().StartsWith("D_"))  // D_##% or D_##cc
                {
                    if (metricList[i].ToLower().EndsWith("cc"))
                    {
                        volpres = VolumePresentation.AbsoluteCm3;
                        value = Convert.ToDouble(metricList[i].Substring(2, length - 4));
                    }
                    else if (metricList[i].EndsWith("%"))
                    {
                        volpres = VolumePresentation.Relative;
                        value = Convert.ToDouble(metricList[i].Substring(2, length - 3));
                    }
                    try
                    {
                        DVHData dvh = plan.GetDVHCumulativeData(rtStructureDic[structureList[i]], 
                            DoseValuePresentation.Absolute, volpres, 0.01);
                        metricValues[i] = DVHExtensions.DoseAtVolume(dvh, value);
                        //metricValues[i] = plan.GetDoseAtVolume(rtStructureDic[structureList[i]],
                        //    value, volpres, DoseValuePresentation.Absolute);
                    }
                    catch
                    {
                        metricValues[i] = new DoseValue(-1, DoseValue.DoseUnit.cGy);
                    }
                }
                else
                {
                    metricValues[i] = "Unknown Metric: " + metricList[i];
                }
            }
            // System.Windows.MessageBox.Show("Parsing metric done");
            return;
        }

        private void ParsingCriteria()
        {
            // parsing criteria. All values are converted to absolute if not yet.
            criteriaEntries = new dynamic[criteriaInputList.Count()];
            metricNumericalValues = new dynamic[criteriaEntries.Count()];
            for (int i = 0; i < criteriaInputList.Count(); i++)
            {
                if (criteriaInputList[i].ToUpper().Contains("GDMAX"))
                {
                    string newReplace;
                    if (globalDmax.UnitAsString.ToLower() == "cgy")
                        newReplace = globalDmax.Dose.ToString();
                    else
                        newReplace = (globalDmax.Dose * 100.0).ToString();

                    newReplace = criteriaInputList[i].ToUpper().Replace("GDMAX", newReplace);
                    criteriaEntries[i] = new Criteria(newReplace);
                }
                else if (criteriaInputList[i].ToUpper() == "RTOG0915_R50")
                {
                    criteriaEntries[i] = new Criteria(RTOG0915_R50());
                }
                else if (criteriaInputList[i].ToUpper() == "RTOG0915_D2CM")
                {
                    criteriaEntries[i] = new Criteria(RTOG0915_D2cm());
                }
                else if (criteriaInputList[i].ToUpper() == "RTOG0813_R50")
                {
                    criteriaEntries[i] = new Criteria(RTOG0813_R50());
                }
                else if (criteriaInputList[i].ToUpper() == "RTOG0813_D2CM")
                {
                    criteriaEntries[i] = new Criteria(RTOG0813_D2cm());
                }
                else if (criteriaInputList[i].ToUpper() == "LUSTRE_R100")
                {
                    criteriaEntries[i] = new Criteria(LUSTRE_R100());
                }
                else if (criteriaInputList[i].ToUpper() == "LUSTRE_R50")
                {
                    criteriaEntries[i] = new Criteria(LUSTRE_R50());
                }
                else if (criteriaInputList[i].ToUpper() == "LUSTRE_D2CM")
                {
                    criteriaEntries[i] = new Criteria(LUSTRE_D2cm());
                }
                else
                {
                    criteriaEntries[i] = new Criteria(criteriaInputList[i]);
                }

                metricNumericalValues[i] = (metricValues[i] is DoseValue) ? metricValues[i].Dose : metricValues[i];
            }
            // System.Windows.MessageBox.Show("Parsing criteria done");
            return;
        }

        private void PerformMetricToCriteriaComparison()
        {
            // Comparing metric with criteria
            criteriaViolations = new CriteriaViolation [structureList.Count()];
            for (int i = 0; i < structureList.Count(); i++)
            {
                if (metricNumericalValues[i] is null || double.IsNaN(metricNumericalValues[i]))
                {
                    criteriaViolations[i] = CriteriaViolation.No;
                }
                else
                {
                    criteriaViolations[i] = CompareMetricToCriteria(metricNumericalValues[i],
                        relationList[i], criteriaEntries[i]);
                }
            }
            return;
        }

        private CriteriaViolation CompareMetricToCriteria(double metric, string relation, Criteria criteria)
        {
            CriteriaViolation theViolation = CriteriaViolation.No;

            if (relation == ">")
            {
                if (criteria.has_major && metric <= criteria.major.numeric)
                    theViolation = CriteriaViolation.Major;
                else if (criteria.has_minor && metric <= criteria.minor.numeric)
                    theViolation = CriteriaViolation.Minor;
            }
            else if (relation == ">=")
            {
                if (criteria.has_major && metric < criteria.major.numeric)
                    theViolation = CriteriaViolation.Major;
                else if (criteria.has_minor && metric < criteria.minor.numeric)
                    theViolation = CriteriaViolation.Minor;
            }
            else if (relation == "<")
            {
                if (criteria.has_major && metric >= criteria.major.numeric)
                    theViolation = CriteriaViolation.Major;
                else if (criteria.has_minor && metric >= criteria.minor.numeric)
                    theViolation = CriteriaViolation.Minor;
            }
            else if (relation == "<=")
            {
                if (criteria.has_major && metric > criteria.major.numeric)
                    theViolation = CriteriaViolation.Major;
                else if (criteria.has_minor && metric > criteria.minor.numeric)
                    theViolation = CriteriaViolation.Minor;
            }
            else if (relation == "=" || relation == "==")
            {
                if (criteria.has_major && Math.Abs(metric-criteria.major.numeric)>1.0E-6)
                    theViolation = CriteriaViolation.Major;
                else if (criteria.has_minor && Math.Abs(metric-criteria.minor.numeric)>1.0E-6)
                    theViolation = CriteriaViolation.Minor;
            }
            else if (relation == "!=")
            {
                if (criteria.has_major && metric == criteria.major.numeric)
                    theViolation = CriteriaViolation.Major;
                else if (criteria.has_minor && metric == criteria.minor.numeric)
                    theViolation = CriteriaViolation.Minor;
            }

            return theViolation;
        }

        public void DisplayResults()
        {
            Patient patient = plan.Course.Patient;
            Course course = plan.Course;

            string message = "";

            message += ("Patient: #bold#" + patient.LastName + ", " 
                + patient.FirstName + "#normal# (" + patient.Id + ")\n");
            message += ("Course:  " + course.Id + "      Plan:  " + plan.Id + "\n");
            //message += string.Format("Prescription: {0},  {1:0.##%}\n\n", 
            //    plan.TotalPrescribedDose, plan.PrescribedPercentage);
            message += string.Format("Prescription: #bold#{0}#normal#, in #bold#{1}#normal# fractions.\n", 
                plan.TotalPrescribedDose, plan.UniqueFractionation.NumberOfFractions);
            message += string.Format("Prescribed Percentage: {0:0.##%}\n\n", plan.PrescribedPercentage);
            

            message += string.Format("Global Dmax:\t{0}  or  {1:0.##%}\n",
                globalDmax, globalDmax / plan.TotalPrescribedDose);
            message += string.Format("PTV Volume:\t{0:0.00} cc\n\n", ptvVolume);

            int maxLength = 0;
            for (int i=0; i<structureList.Count(); i++)
            {
                if (rtStructureDic[structureList[i]] != null && metricNumericalValues[i] >= 0.0)
                {
                    maxLength = (structureList[i].Length > maxLength) ? structureList[i].Length : maxLength;
                }
            }
            maxLength += 1;

            for (int i = 0; i < structureList.Count(); i++)
            {
                if (rtStructureDic[structureList[i]] != null && metricNumericalValues[i] >= 0.0)
                {
                    if (criteriaViolations[i] == CriteriaViolation.No)
                        message += "    ";
                    else if (criteriaViolations[i] == CriteriaViolation.Minor)
                        message += "#red#?  #normal#";
                    else if (criteriaViolations[i] == CriteriaViolation.Major)
                        message += "#red#X  #normal#";

                    // message += (meetCriteria[i]) ? "    " : "#red#X  #normal#";
                    double factor = 1;
                    if (((criteriaEntries[i].has_major && criteriaEntries[i].major.unit == "%") ||
                         (criteriaEntries[i].has_minor && criteriaEntries[i].minor.unit == "%")) &&
                         metricList[i].StartsWith("R_"))
                    {
                        factor = 100;
                    }
                    string strfmt = "{0,-" + maxLength.ToString() + "}\t {1}\t= {2:0.00}";
                    //message = message + strfmt + "\n";
                    message += string.Format(strfmt, //"{0,-15}\t {1}\t= {2:0.00}",
                        rtStructureDic[structureList[i]].Id + ":", metricList[i], metricValues[i] * factor);

                    //message += String.Format(" ({0}{1})\n", relationList[i], criteriaEntries[i].ToString());

                    if (criteriaInputList[i].ToUpper().Contains("GDMAX") ||
                        criteriaInputList[i].ToUpper().StartsWith("RTOG") ||
                        criteriaInputList[i].ToUpper().StartsWith("LUSTRE"))
                    {
                        if (metricList[i].ToUpper().StartsWith("R_") ||
                            metricList[i].ToUpper().StartsWith("P_"))
                        {
                            message += string.Format(" ({0}{1})\n", relationList[i], criteriaEntries[i].ToString("0.00"));
                        }
                        else
                        {
                            message += string.Format(" ({0}{1})\n", relationList[i], criteriaEntries[i].ToString("0.0"));
                        }

                    }
                    else
                    {
                        if (criteriaEntries[i].ToString().Trim().EndsWith("%") ||
                            (criteriaEntries[i].has_major && criteriaEntries[i].major.unit == "%") ||
                            (criteriaEntries[i].has_minor && criteriaEntries[i].minor.unit == "%") )
                        {
                            message += "%";
                        }
                        message += String.Format(" ({0}{1})\n", relationList[i], criteriaEntries[i].ToString());
                    }
                }
            }

            for (int i=0; i<structureList.Count(); i++)
            {
                if (rtStructureDic[structureList[i]] != null && metricNumericalValues[i] < 0.0)
                {
                    string strfmt = "    {0,-" + maxLength.ToString() + "}\t structure not contoured??\n";
                    message += string.Format(strfmt, // "    {0,-15}:\t structure not contoured??\n", 
                        rtStructureDic[structureList[i]].Id+":");
                }
            }

            foreach (KeyValuePair<string, Structure> pair in rtStructureDic)
            {
                if (pair.Value == null)
                {
                    string strfmt = "    {0,-" + maxLength.ToString() + "}\t structure not found!\n";
                    message += string.Format(strfmt, pair.Key + ":");
                }
            }


            message += ("\nEclipse Plan Evaluation Plugin -- Ver 1.0 (ESAPI_13.6)\n");
            message += ("(ɔ) Lixin Zhan @GRRCC, 2017-2020, MIT License.\n");
            //message += ("\n\t*** Use at your own risk! ***\n\n");

            MsgBox.Show(message, "Eclipse Plan Evaluation Kit");

            return;
        }



        // Lustre protocol. Ratio of 100% presc isodose volume to PTV volume. R100%
        private string LUSTRE_R100()
        {
            double vptv = rtStructureDic["PTV"].Volume;

            double minor, major;

            if (vptv <= 20)
            {
                minor = 1.25;
                major = 1.40;
            }
            else if (vptv <= 40)
            {
                minor = 1.15;
                major = 1.25;
            }
            else
            {
                minor = 1.10;
                major = 1.20;
            }

            return "minormajor(" + minor.ToString() + ", " + major.ToString() + ")";
        }

        // Lustre protocol. Ratio of 50% presc isodose volume to PTV volume. R50%
        private string LUSTRE_R50()
        {
            double vptv = rtStructureDic["PTV"].Volume;

            double minor, major;

            if (vptv <= 20)
            {
                minor = 12.0;
                major = 14.0;
            }
            else if (vptv <= 40)
            {
                minor = 9.0;
                major = 11.0;
            }
            else
            {
                minor = 6.0;
                major = 8.0;
            }

            return "minormajor(" + minor.ToString() + ", " + major.ToString() + ")";
        }

        // Lustre protocol. Maximum Dose (in % of dose prescribed) @ 2cm from PTV in any direction.
        private string LUSTRE_D2cm()
        {
            double vptv = rtStructureDic["PTV"].Volume;

            double minor;
            double major;

            if (vptv <= 20)
            {
                minor = 65.0;
                major = 75.0;
            }
            else
            {
                minor = 70.0;
                major = 80.0;
            }

            return "minormajor(" + minor.ToString() + ", " + major.ToString() + ")";
        }

        // R100: Ratio of 100% Presc Isodose Volume to PTV Volume, R100%
        private string RTOG0915_R100()
        {
            return "minormajor(1.2, 1.5)";
        }

        private string RTOG0813_R100()
        {
            return RTOG0915_R100();
        }

        // RTOG0915 48/4
        // R50: Ratio of 50% Presc Isodose Volume to PTV Volume, R50%.
        private string RTOG0915_R50()
        {
            double[] xarray = new double[] { 1.8, 3.8, 7.4, 13.2, 22, 34, 50, 70, 95, 126, 163 };
            double[] yarray = new double[] { 5.9, 5.5, 5.1, 4.7, 4.5, 4.3, 4.0, 3.5, 3.3, 3.1, 2.9 };
            double[] zarray = new double[] { 7.5, 6.5, 6.0, 5.8, 5.5, 5.3, 5.0, 4.8, 4.4, 4.0, 3.7 };

            double myx = rtStructureDic["PTV"].Volume;

            double minor = LinearInterpolate(myx, xarray, yarray);
            double major = LinearInterpolate(myx, xarray, zarray);

            return "minormajor(" + minor.ToString() + ", " + major.ToString() + ")";
        }

        // RTOG0915 48/4
        // D_2cm(%): Maximum Dose (in % of dose prescribed) @ 2cm from PTV in any direction.
        private string RTOG0915_D2cm()
        {
            double[] xarray = new double[] { 1.8, 3.8, 7.4, 13.2, 22, 34, 50, 70, 95, 126, 163 };
            double[] yarray = new double[] { 50, 50, 50, 50, 54, 58, 62, 66, 70, 73, 77 };
            double[] zarray = new double[] { 57, 57, 58, 58, 63, 68, 77, 86, 89, 91, 94 };

            double myx = rtStructureDic["PTV"].Volume;

            double minor = LinearInterpolate(myx, xarray, yarray);
            double major = LinearInterpolate(myx, xarray, zarray);

            return "minormajor(" + minor.ToString() + ", " + major.ToString() + ")";
        }

        // RTOG0813 50/5
        // Same R50 defination as in RTOG0915.
        private string RTOG0813_R50()
        {
            return RTOG0915_R50();
        }

        // RTOG0813 50/5
        // Same D_scm definition as in RTOG0915
        private string RTOG0813_D2cm()
        {
            return RTOG0915_D2cm();
        }

        // Simple Linear Interpolation from neighbour points.
        private double LinearInterpolate(double x, double[] xarray, double[] yarray)
        {
            if (x <= xarray.First()) return yarray.First();
            if (x >= xarray.Last()) return yarray.Last();

            int index=0;
            for (int i = 0; i < xarray.Count(); i++)
            {
                if (x < xarray[i])
                {
                    index = i;
                    break;
                }
            }
            return yarray[index - 1] + (yarray[index] - yarray[index - 1]) / (xarray[index] - xarray[index - 1]);
        }

    } // class QAProtocol
}
