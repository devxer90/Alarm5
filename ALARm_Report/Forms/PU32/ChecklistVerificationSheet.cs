using ALARm.Core;
using ALARm.Core.Report;
using ALARm.Services;
using ALARm_Report.controls;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace ALARm_Report.Forms
{
    public class ChecklistVerificationSheet : Report
    {
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            // ===== 1) выбор путей =====
            List<long> admTracksId = new List<long>();
            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();
                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;

                admTracksId = choiceForm.admTracksIDs ?? new List<long>();
                if (admTracksId.Count == 0)
                    return;
            }

            // ===== 2) справочники =====
            var road = AdmStructureService.GetRoadName(parentId, AdmStructureConst.AdmDistance, true) ?? "";
            var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;
            if (distance == null)
            {
                MessageBox.Show("Не удалось определить ПЧ (distance).");
                return;
            }

            // trips
            var tripProcesses = RdStructureService.GetMainParametersProcess(period, distance.Code) ?? new List<MainParametersProcess>();
            if (tripProcesses.Count == 0)
            {
                MessageBox.Show(Properties.Resources.paramDataMissing);
                return;
            }

            // ===== 3) строим XML =====
            var xdReport = new XDocument(new XElement("report"));

            foreach (var tripProcess in tripProcesses)
            {
                var trip = RdStructureService.GetTrip(tripProcess.Id);
                if (trip == null)
                    continue;

                long tripId = trip.Id;

                // дата проезда (если у тебя в tripProcess есть Date_Vrem — используем её, иначе trip.Trip_date)
                DateTime tripDateTime =
                    (tripProcess.Date_Vrem != DateTime.MinValue)
                        ? tripProcess.Date_Vrem
                        : trip.Trip_date;

                var tripElem = new XElement("trip",
                    new XAttribute("version", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                    new XAttribute("check", tripProcess.GetProcessTypeName ?? ""),
                    new XAttribute("road", road),
                    new XAttribute("distance", distance.Code ?? ""),
                    new XAttribute("periodDate", period.Period ?? ""),
                    new XAttribute("chief", tripProcess.Chief ?? ""),
                    new XAttribute("ps", tripProcess.Car ?? ""),
                    new XAttribute("tripId", tripId),
                    new XAttribute("tripDate", tripDateTime.ToString("dd.MM.yyyy HH:mm:ss"))
                );

                var directions = new XElement("directions");

                // каждый выбранный путь
                foreach (var trackId in admTracksId)
                {
                    var trackNameObj = AdmStructureService.GetTrackName(trackId);
                    string trackName = trackNameObj?.ToString() ?? trackId.ToString();

                    // если путь не число — всё равно попробуем, но аккуратно
                    int trackNum = 0;
                    int.TryParse(trackName, NumberStyles.Integer, CultureInfo.InvariantCulture, out trackNum);

                    List<Kilometer> kilometers = null;
                    try
                    {
                        kilometers = RdStructureService.GetKilometersByTripdistanceperiod(trip, int.Parse(distance.Code), trackNum);
                    }
                    catch
                    {
                        kilometers = new List<Kilometer>();
                    }

                    // tracks node (в нём и заголовок направления/пути, и строки)
                    var xeTracks = new XElement("tracks",
                        new XAttribute("directioncode", tripProcess.DirectionCode ?? ""),
                        new XAttribute("directionname", tripProcess.DirectionName ?? ""),
                        new XAttribute("track", trackName)
                    );

                    bool hasAnyRow = false;

                    if (kilometers != null && kilometers.Count > 0)
                    {
                        foreach (var km in kilometers)
                        {
                            // контрольные участки
                            var sections = MainTrackStructureService.GetMtoObjectsByCoord(
                                tripProcess.Date_Vrem,
                                km.Number,
                                MainTrackStructureConst.MtoCheckSection,
                                tripProcess.DirectionName,
                                trackName
                            ) as List<CheckSection>;

                            if (sections == null || sections.Count == 0)
                                continue;

                            foreach (var sect in sections)
                            {
                                var check = RdStructureService.CheckVerify(
                                    tripProcess.Trip_id,
                                    sect.Start_Km * 1000 + sect.Start_M,
                                    sect.Final_Km * 1000 + sect.Final_M
                                );

                                if (check == null || check.Count == 0)
                                    continue;

                                hasAnyRow = true;

                                var row = new XElement("row",
                                    new XAttribute("km", km.Number),
                                    new XAttribute("date", tripDateTime.ToString("dd.MM.yyyy")),

                                    // Уровень (измеренные)
                                    new XAttribute("lvl_mo", check[0].Trip_mo_level.ToString("0.0", CultureInfo.InvariantCulture)),
                                    new XAttribute("lvl_sko", check[0].Trip_sko_level.ToString("0.0", CultureInfo.InvariantCulture)),

                                    // Уровень (установленные)
                                    new XAttribute("lvl_set_mo", sect.Avg_level.ToString("0.0", CultureInfo.InvariantCulture)),
                                    new XAttribute("lvl_set_sko", sect.Sko_level.ToString("0.0", CultureInfo.InvariantCulture)),

                                    // Шаблон/Ширина (измеренные)
                                    new XAttribute("sh_mo", check[0].Trip_mo_gauge.ToString("0.0", CultureInfo.InvariantCulture)),
                                    new XAttribute("sh_sko", check[0].Trip_sko_gauge.ToString("0.0", CultureInfo.InvariantCulture)),

                                    // Шаблон/Ширина (установленные)
                                    new XAttribute("sh_set_mo", sect.Avg_width.ToString("0.0", CultureInfo.InvariantCulture)),
                                    new XAttribute("sh_set_sko", sect.Sko_width.ToString("0.0", CultureInfo.InvariantCulture))
                                );

                                xeTracks.Add(row);
                            }
                        }
                    }

                    // если данных нет — добавляем пустую строку (чтобы “путь” не пропадал)
                    if (!hasAnyRow)
                    {
                        xeTracks.Add(new XElement("row",
                            new XAttribute("km", "-"),
                            new XAttribute("date", "-"),
                            new XAttribute("lvl_mo", "-"),
                            new XAttribute("lvl_sko", "-"),
                            new XAttribute("lvl_set_mo", "-"),
                            new XAttribute("lvl_set_sko", "-"),
                            new XAttribute("sh_mo", "-"),
                            new XAttribute("sh_sko", "-"),
                            new XAttribute("sh_set_mo", "-"),
                            new XAttribute("sh_set_sko", "-")
                        ));
                    }

                    directions.Add(xeTracks);
                }

                tripElem.Add(directions);
                xdReport.Root.Add(tripElem);
            }

            // ===== 4) XSL -> HTML =====
            var htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                var transform = new XslCompiledTransform();

                // !!! ВАЖНО: убираем BOM/пробелы ДО <?xml ... ?>
                var xsl = (template.Xsl ?? "").TrimStart('\uFEFF', ' ', '\t', '\r', '\n');

                using (var sr = new StringReader(xsl))
                using (var xr = XmlReader.Create(sr))
                {
                    transform.Load(xr);
                    transform.Transform(xdReport.CreateReader(), writer);
                }
            }

            // ===== 5) сохранить/открыть =====
            try
            {
                var outPath = Path.Combine(Path.GetTempPath(), "report.html");
                htReport.Save(outPath);
                System.Diagnostics.Process.Start(outPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения файла: " + ex.Message);
            }
        }

        public override string ToString()
        {
            return "Ведомость результатов измерений ширины колеи и уровня на контрольных участках";
        }
    }
}
