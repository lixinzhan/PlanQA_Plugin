using System;
using System.Windows;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Text;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace EPEK
{
    class QAProtocol
    {
        // Lists for containing info readin from protocol file
        List<string> structureList = new List<string>();
        List<string> metricList = new List<string>();
        List<string> relationList = new List<string>();
        List<string> criteriaList = new List<string>();

        Dictionary<string, Structure> rtStructureDic = new Dictionary<string, Structure>();

        dynamic[] metricValues;
        dynamic[] criteriaValues;
        dynamic[] metricNumericalValues;
        bool[] meetCriteria;

        int numItems;
        
        PlanSetup plan;

        DoseValue globalDmax; // = new DoseValue(-1, DoseValue.DoseUnit.cGy);
        double ptvVolume;

        public QAProtocol() // Default Constructor
        {
            structureList.Clear();
            metricList.Clear();
            relationList.Clear();
            criteriaList.Clear();

            numItems = 0;

            globalDmax = new DoseValue(-1, DoseValue.DoseUnit.cGy);
            ptvVolume = -1.0;
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
                structureList.Add(parts.FirstOrDefault());
                parts = parts.LastOrDefault().Trim().Split(new char[] { ' ', '\t' }, 2, 
                    StringSplitOptions.RemoveEmptyEntries);
                metricList.Add(parts.FirstOrDefault().Trim());
                parts = parts.LastOrDefault().Trim().Split(new char[] { ' ', '\t' }, 2, 
                    StringSplitOptions.RemoveEmptyEntries);
                relationList.Add(parts.FirstOrDefault().Trim());
                criteriaList.Add(parts.LastOrDefault().Trim());
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
                globalDmax = plan.GetDoseAtVolume(body, 0, VolumePresentation.Relative, 
                    DoseValuePresentation.Absolute);
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
            CompareMetricToCriteria();

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
                    try
                    {
                        double structVolume = rtStructureDic[structureList[i]].Volume; // to verify struct exists.
                        metricValues[i] = plan.GetDoseAtVolume(rtStructureDic[structureList[i]], 0,
                            VolumePresentation.Relative, DoseValuePresentation.Absolute);
                    }
                    catch
                    {   // in case structure not exist.
                        metricValues[i] = new DoseValue(0, DoseValue.DoseUnit.cGy); // make it float.
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
                        metricValues[i] = plan.GetVolumeAtDose(rtStructureDic[structureList[i]],
                            dosevalue, VolumePresentation.Relative);
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
                            structVolume = 0.0;
                        }
                        metricValues[i] = (metricValues[i] * structVolume / ptvVolume) * 100.0; // to %
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
                    metricValues[i] = plan.GetDoseAtVolume(rtStructureDic[structureList[i]],
                        value, volpres, DoseValuePresentation.Absolute);
                }
                else
                {
                    metricValues[i] = "Failed interpreting metric: " + metricList[i];
                }
            }
            // System.Windows.MessageBox.Show("Parsing metric done");
            return;
        }

        private void ParsingCriteria()
        {
            // parsing criteria. All values are converted to absolute if not yet.
            criteriaValues = new dynamic[criteriaList.Count()];
            metricNumericalValues = new dynamic[criteriaValues.Count()];
            for (int i = 0; i < criteriaList.Count(); i++)
            {
                if (criteriaList[i].ToUpper() == "GDMAX")
                {
                    criteriaValues[i] = globalDmax.Dose;
                }
                else if (criteriaList[i].ToUpper() == "RTOG0915_R50")
                {
                    criteriaValues[i] = RTOG0915_R50();
                }
                else if (criteriaList[i].ToUpper() == "RTOG0915_D2CM")
                {
                    criteriaValues[i] = RTOG0915_D2cm();
                }
                else if (criteriaList[i].ToUpper() == "RTOG0813_R50")
                {
                    criteriaValues[i] = RTOG0813_R50();
                }
                else if (criteriaList[i].ToUpper() == "RTOG0813_D2CM")
                {
                    criteriaValues[i] = RTOG0813_D2cm();
                }
                else if (criteriaList[i].EndsWith("%"))
                {
                    criteriaValues[i] = Convert.ToDouble(
                        criteriaList[i].Substring(0, criteriaList[i].Length - 1));
                    if (metricList[i].ToUpper().StartsWith("R_"))  // R_??% or R_??cGy is a ratio
                    {
                        criteriaValues[i] = Convert.ToDouble(
                            criteriaList[i].Substring(0, criteriaList[i].Length - 1)) / 100;
                    }
                }
                else if (criteriaList[i].ToUpper().EndsWith("CGY"))
                {
                    criteriaValues[i] = Convert.ToDouble(
                        criteriaList[i].Substring(0, criteriaList[i].Length - 3));
                }
                else
                {
                    criteriaValues[i] = Convert.ToDouble(criteriaList[i]);
                }

                metricNumericalValues[i] = (metricValues[i] is DoseValue) ? metricValues[i].Dose : metricValues[i];
            }
            // System.Windows.MessageBox.Show("Parsing criteria done");
            return;
        }

        private void CompareMetricToCriteria()
        {
            // Comparing metric with criteria
            meetCriteria = new bool[structureList.Count()];
            for (int i = 0; i < structureList.Count(); i++)
            {
                if (relationList[i] == ">")
                    meetCriteria[i] = (metricNumericalValues[i] > criteriaValues[i]) ? true : false;
                else if (relationList[i] == ">=")
                    meetCriteria[i] = (metricNumericalValues[i] >= criteriaValues[i]) ? true : false;
                else if (relationList[i] == "<")
                    meetCriteria[i] = (metricNumericalValues[i] < criteriaValues[i]) ? true : false;
                else if (relationList[i] == "<=")
                    meetCriteria[i] = (metricNumericalValues[i] <= criteriaValues[i]) ? true : false;
                else if (relationList[i] == "=")
                    meetCriteria[i] = (metricNumericalValues[i] == criteriaValues[i]) ? true : false;
                else if (relationList[i] == "==")
                    meetCriteria[i] = (metricNumericalValues[i] == criteriaValues[i]) ? true : false;
                else if (relationList[i] == "!=")
                    meetCriteria[i] = (metricNumericalValues[i] != criteriaValues[i]) ? true : false;
                else
                    meetCriteria[i] = false;
            }
            // System.Windows.MessageBox.Show("Comparison done");
            return;
        }

        public void DisplayResults()
        {
            Patient patient = plan.Course.Patient;
            Course course = plan.Course;

            string message = "";

            message += ("Patient: " + patient.LastName + ", " + patient.FirstName + " (" + patient.Id + ")\n");
            message += ("Course:  " + course.Id + "      Plan:  " + plan.Id + "\n");
            message += string.Format("Prescription: {0},  {1:0.##%}\n\n", 
                plan.TotalPrescribedDose, plan.PrescribedPercentage);

            message += string.Format("Global Dmax:\t{0}  or  {1:0.##%}\n",
                globalDmax, globalDmax / plan.TotalPrescribedDose);
            message += string.Format("PTV Volume:\t{0:0.00} cc\n\n", ptvVolume);

            for (int i = 0; i < structureList.Count(); i++)
            {
                if (rtStructureDic[structureList[i]] != null)
                {
                    message += (meetCriteria[i]) ? "    " : "??  ";
                    double factor = (criteriaList[i].EndsWith("%") && metricList[i].StartsWith("R_")) ? 100 : 1;
                    message += string.Format("{0,-15}\t {1}\t= {2:0.00}",
                        rtStructureDic[structureList[i]].Id+":", metricList[i], metricValues[i] * factor);
                    if (criteriaList[i].ToUpper().StartsWith("GDMAX") || 
                        criteriaList[i].ToUpper().StartsWith("RTOG")) 
                    {
                        if (metricList[i].ToUpper().StartsWith("R_") ||
                            metricList[i].ToUpper().StartsWith("P_")) 
                        {
                            message += string.Format(" ({0:0.00})\n", criteriaValues[i]);
                        }
                        else
                        {
                            message += string.Format(" ({0:0.0})\n", criteriaValues[i]);
                        }
                        
                    }
                    else 
                    {
                        message += (" (" + criteriaList[i] + ")\n");

                    }
                }
            }

            foreach (KeyValuePair<string, Structure> pair in rtStructureDic)
            {
                if (pair.Value == null)
                {
                    message += string.Format("    {0:-15}\t structure not found!\n", pair.Key + ":");
                }
            }


            message += ("\n\t*** USE AT YOUR OWN RISK!!! ***\n");
            message += ("\n\t*** NO guarentee for anything with this program! ***\n\n");
            message += ("Eclipse Plan Evaluation Plugin -- Ver 0.5, (ɔ) 2017-2018.\n");
            message += ("Your feedbacks are always welcome!");

            System.Windows.MessageBox.Show(message, "Eclipse Plan Evaluation Kit");

            return;
        }

        ~QAProtocol() // Destructor
        {
            structureList.Clear();
            metricList.Clear();
            relationList.Clear();
            criteriaList.Clear();

            metricValues = null;
            criteriaValues = null;
            metricValues = null;
            meetCriteria = null;

            GC.Collect();
        }

        // RTOG0915 48/4
        // R50: Ratio of 50% Presc Isodose Volume to PTV Volume, R50%.
        private double RTOG0915_R50()
        {
            double[] xarray = new double[] { 1.8, 3.8, 7.4, 13.2, 22, 34, 50, 70, 95, 126, 163 };
            double[] yarray = new double[] { 5.9, 5.5, 5.1, 4.7, 4.5, 4.3, 4.0, 3.5, 3.3, 3.1, 2.9 };

            double myx = rtStructureDic["PTV"].Volume;

            return LinearInterpolate(myx, xarray, yarray);
        }

        // RTOG0915 48/4
        // D_2cm(%): Maximum Dose (in % of dose prescribed) @ 2cm from PTV in any direction.
        private double RTOG0915_D2cm()
        {
            double[] xarray = new double[] { 1.8, 3.8, 7.4, 13.2, 22, 34, 50, 70, 95, 126, 163 };
            double[] yarray = new double[] { 50, 50, 50, 50, 54, 58, 62, 66, 70, 73, 77 };

            double myx = rtStructureDic["PTV"].Volume;

            return LinearInterpolate(myx, xarray, yarray);
        }

        // RTOG0813 50/5
        // Same R50 defination as in RTOG0915.
        private double RTOG0813_R50()
        {
            return RTOG0915_R50();
        }

        // RTOG0813 50/5
        // Same D_scm definition as in RTOG0915
        private double RTOG0813_D2cm()
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
