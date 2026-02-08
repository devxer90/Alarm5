using ALARm.Core;
using ALARm.Core.Report;
using ALARm.Services;
using ALARm_Report.controls;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using WebSocketSharp;

namespace ALARm_Report.Forms
{
    public class ControlAdjustmentProtocol : Report
    {
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            // =====================
            // Выбор путей
            // =====================
            List<long> admTracksId = new List<long>();
            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();
                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;

                admTracksId = choiceForm.admTracksIDs ?? new List<long>();
            }

            string outPath = Path.Combine(Path.GetTempPath(), "report.html");

            var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;
            var roadName = AdmStructureService.GetRoadName(parentId, AdmStructureConst.AdmDistance, true);
            var tripProcesses = RdStructureService.GetProcess(period, parentId, ProcessType.VideoProcess);

            XDocument xdReport = new XDocument();
            XElement report = new XElement("report");

            foreach (var tripProcess in tripProcesses)
            {
                var trip = RdStructureService.GetTrip(tripProcess.Id);
                if (trip == null) continue;

                var tripDateStr = GetTripDate(trip);
                var cap = RdStructureService.ControlAdjustmentProtocol(trip.Id);
                var s3 = RdStructureService.GetS3ByTripId(trip.Id);

                foreach (var trackId in admTracksId)
                {
                    var trackName = Convert.ToString(AdmStructureService.GetTrackName(trackId));
                    if (string.IsNullOrWhiteSpace(trackName)) continue;

                    trip.Track_Id = trackId;

                    XElement tripElem = new XElement("trip",
                        new XAttribute("version", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                        new XAttribute("date_statement", DateTime.Now.ToString("dd.MM.yyyy")),
                        new XAttribute("trip_id", trip.Id),
                        new XAttribute("trip_date", tripDateStr),
                        new XAttribute("check", trip.GetProcessTypeName),
                        new XAttribute("road", roadName ?? ""),
                        new XAttribute("track", trackName),
                        new XAttribute("code", tripProcess.DirectionCode ?? ""),
                        new XAttribute("direction", tripProcess.DirectionName ?? ""),
                        new XAttribute("distance", distance?.Code ?? ""),
                        new XAttribute("periodDate", period?.Period ?? ""),
                        new XAttribute("chief", trip.Chief ?? ""),
                        new XAttribute("ps", trip.Car ?? "")
                    );

                    XElement notes = new XElement("NOTES");
                    bool hasData = false;

                    if (cap != null && s3 != null)
                    {
                        foreach (var elem in cap.Where(e => e.Track == trackName))
                        {
                            var st = s3.FirstOrDefault(o => o.original_id == elem.original_id);

                            XElement note = new XElement("NOTE",
                                new XAttribute("Km", elem.Km),
                                new XAttribute("Mtr", elem.Meter),
                                new XAttribute("vidcorect", "Изменение оценки"),
                                new XAttribute("Comment", elem.comment ?? ""),
                                new XAttribute("Otst", elem.NAME ?? ""),

                                new XAttribute("old_Stepen", st?.Typ ),
                                new XAttribute("old_value", st?.VALUE),
                                new XAttribute("old_length", st?.LENGTH),
                                new XAttribute("old_count", st?.COUNT),
                                new XAttribute("old_strelka", ""),
                                new XAttribute("old_most", ""),

                                new XAttribute("Stepen", elem.Typ),
                                new XAttribute("value", elem.VALUE),
                                new XAttribute("length", elem.LENGTH),
                                new XAttribute("count", elem.COUNT),
                                new XAttribute("strelka", ""),
                                new XAttribute("most", "")
                            );

                            notes.Add(note);
                            hasData = true;
                        }
                    }

                    // =====================
                    // ЕСЛИ ДАННЫХ НЕТ
                    // =====================
                    if (!hasData)
                    {
                        notes.Add(CreateEmptyNote());
                    }

                    tripElem.Add(notes);
                    report.Add(tripElem);
                }
            }

            xdReport.Add(report);

            XDocument htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }

            htReport.Save(outPath);
            System.Diagnostics.Process.Start(outPath);
        }

        // =====================
        // NOTE "нет данных"
        // =====================
        private static XElement CreateEmptyNote()
        {
            return new XElement("NOTE",
                new XAttribute("Km", "нет данных"),
                new XAttribute("Mtr", "нет данных"),
                new XAttribute("vidcorect", "нет данных"),
                new XAttribute("Comment", "нет данных"),
                new XAttribute("Otst", "нет данных"),

                new XAttribute("old_Stepen", "нет данных"),
                new XAttribute("old_value", "нет данных"),
                new XAttribute("old_length", "нет данных"),
                new XAttribute("old_count", "нет данных"),
                new XAttribute("old_strelka", "нет данных"),
                new XAttribute("old_most", "нет данных"),

                new XAttribute("Stepen", "нет данных"),
                new XAttribute("value", "нет данных"),
                new XAttribute("length", "нет данных"),
                new XAttribute("count", "нет данных"),
                new XAttribute("strelka", "нет данных"),
                new XAttribute("most", "нет данных")
            );
        }

        private static string GetTripDate(object trip)
        {
            try
            {
                var t = trip.GetType();
                var p =
                    t.GetProperty("Trip_date") ??
                    t.GetProperty("trip_date") ??
                    t.GetProperty("TripDate");

                if (p == null) return "";
                var v = p.GetValue(trip);
                if (v is DateTime dt)
                    return dt.ToString("dd.MM.yyyy HH:mm:ss");

                return v?.ToString() ?? "";
            }
            catch
            {
                return "";
            }
        }

        public override string ToString()
        {
            return "Протокол корректировки результатов контроля";
        }
    }
}
