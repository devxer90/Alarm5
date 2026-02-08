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
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace ALARm_Report.Forms
{
    public class PRU : ALARm.Core.Report.GraphicDiagrams
    {
        public override void Process(Int64 parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            // ===== 1) Выбор путей =====
            List<long> admTracksId = new List<long>();
            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();
                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;

                admTracksId = choiceForm.admTracksIDs ?? new List<long>();
            }
            if (admTracksId.Count == 0) return;

            // ===== 2) Справочники =====
            this.MainTrackStructureRepository = MainTrackStructureService.GetRepository();

            var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;
            if (distance == null)
            {
                MessageBox.Show("Не удалось определить ПЧ (distance).");
                return;
            }

            var road = AdmStructureService.GetRoadName(distance.Id, AdmStructureConst.AdmDistance, true);
            distance.Name = (distance.Name ?? "").Replace("ПЧ-", "");

            var tripProcesses = RdStructureService.GetTripsOnDistance(parentId, period);
            if (tripProcesses == null || tripProcesses.Count == 0)
            {
                MessageBox.Show(Properties.Resources.paramDataMissing);
                return;
            }

            // ===== 3) Формирование отчёта =====
            XDocument htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                XDocument xdReport = new XDocument(new XElement("report"));

                foreach (var tripProces in tripProcesses)
                {
                    // получаем trip (проезд)
                    var trip = RdStructureService.GetTrip(tripProces.Id);
                    if (trip == null)
                        continue;

                    // Один проезд = один tripElem
                    var tripElem = new XElement("trip",
                        new XAttribute("version", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                        new XAttribute("tripId", trip.Id),
                        new XAttribute("tripDate", trip.Trip_date.ToString("dd.MM.yyyy HH:mm")),
                        new XAttribute("direction", tripProces.Direction),
                        new XAttribute("directioncode", tripProces.DirectionCode),
                        new XAttribute("check", tripProces.GetProcessTypeName ?? ""),
                        new XAttribute("road", road ?? ""),
                        new XAttribute("distance", distance.Code ?? ""),
                        new XAttribute("periodDate", period?.Period ?? ""),
                        new XAttribute("chief", tripProces.Chief ?? ""),
                        new XAttribute("ps", tripProces.Car ?? "")
                    );

                    // все километры по этому проезду (один раз)
                    var allKilometers = RdStructureService.GetKilometersByTrip(trip) ?? new List<Kilometer>();
                    if (allKilometers.Count == 0)
                        continue;

                    // список ПрУ по этому проезду (один раз)
                    var listS3 = RdStructureService.GetS3(allKilometers.First().Trip.Id, "ПрУ") as List<S3>;
                    listS3 = listS3 ?? new List<S3>();

                    bool foundAny = false;

                    // directions container (как в твоём XSL: directions/tracks/note)
                    var xeDirections = new XElement("directions");

                    foreach (var track_id in admTracksId)
                    {
                        var trackNameObj = AdmStructureService.GetTrackName(track_id);
                        string trackName = trackNameObj?.ToString() ?? track_id.ToString();

                        // блок пути
                        var xeTracks = new XElement("tracks",
                            new XAttribute("trackId", track_id),
                            new XAttribute("track", trackName)
                        );

                        // километры только этого пути
                        var kmsByTrack = allKilometers.Where(k => k.Track_id == track_id).ToList();

                        // если по пути нет км — всё равно добавим пустую строку (чтобы в форме был путь)
                        if (kmsByTrack.Count == 0)
                        {
                            xeTracks.Add(new XElement("note",
                                new XAttribute("km", ""),
                                new XAttribute("m", ""),
                                new XAttribute("Otkl", ""),
                                new XAttribute("len", ""),
                                new XAttribute("vpz", "")
                            ));
                            xeDirections.Add(xeTracks);
                            continue;
                        }

                        // данные ПрУ только по этому пути
                        // ВАЖНО: сравнение делаем по s3.Put, который обычно хранит номер пути ("1","2"...)
                        // поэтому используем trackName (как строку)
                        var pruRows = listS3
                            .Where(o => o.Ots == "ПрУ" &&
                                        o.Put != null &&
                                        o.Put.ToString().Trim() == trackName.Trim())
                            .ToList();

                        if (pruRows.Count == 0)
                        {
                            xeTracks.Add(new XElement("note",
                                new XAttribute("km", ""),
                                new XAttribute("m", ""),
                                new XAttribute("Otkl", ""),
                                new XAttribute("len", ""),
                                new XAttribute("vpz", "")
                            ));
                            xeDirections.Add(xeTracks);
                            continue;
                        }

                        // группируем по км, чтобы не гонять лишнее
                        foreach (var km in kmsByTrack)
                        {
                            // строки ПрУ на этом километре
                            var pruByKm = pruRows.Where(o => o.Km == km.Number).ToList();
                            if (pruByKm.Count == 0)
                                continue;

                            // паспорт/скорости
                            try { km.LoadTrackPasport(MainTrackStructureRepository, trip.Trip_date); } catch { }

                            // безопасный vpz
                            string vpz = "";
                            try
                            {
                                if (km.Speeds != null && km.Speeds.Count > 0)
                                    vpz = km.Speeds.Last().Passenger + "/" + km.Speeds.Last().Freight;
                            }
                            catch { vpz = ""; }

                            foreach (var s3 in pruByKm)
                            {
                                foundAny = true;

                                xeTracks.Add(new XElement("note",
                                    new XAttribute("km", s3.Km),
                                    new XAttribute("m", s3.Meter),
                                    new XAttribute("Otkl", s3.Otkl),
                                    new XAttribute("len", s3.Len),
                                    new XAttribute("vpz", vpz)
                                ));
                            }
                        }

                        // если после фильтров нет ни одной note — добавим пустую (чтобы таблица не ломалась)
                        if (!xeTracks.Elements("note").Any())
                        {
                            xeTracks.Add(new XElement("note",
                                new XAttribute("km", ""),
                                new XAttribute("m", ""),
                                new XAttribute("Otkl", ""),
                                new XAttribute("len", ""),
                                new XAttribute("vpz", "")
                            ));
                        }

                        xeDirections.Add(xeTracks);
                    }

                    tripElem.Add(xeDirections);

                    // добавляем проезд даже если пусто — если хочешь как раньше (печатать пустые тоже)
                    // если надо печатать только когда есть данные — оставь foundAny
                    if (foundAny)
                        xdReport.Root.Add(tripElem);
                    else
                        xdReport.Root.Add(tripElem); // оставил как у тебя: добавлять в любом случае
                }

                // ===== 4) XSL Transform =====
                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }

            // ===== 5) Сохранить и открыть =====
            try
            {
                var outPath = Path.Combine(Path.GetTempPath(), "report.html");
                htReport.Save(outPath);

                // твой путь сохранения
                htReport.Save($@"G:\form\2.Характеристики положения пути в плане и профиле\14.Ведомость отступлений по ПрУ.html");
            }
            catch
            {
                MessageBox.Show("Ошибка сохранения файлы");
            }
            finally
            {
                System.Diagnostics.Process.Start(Path.Combine(Path.GetTempPath(), "report.html"));
            }
        }
    }
}
