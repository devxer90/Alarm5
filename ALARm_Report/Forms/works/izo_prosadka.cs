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
    /// <summary>
    /// Ведомость просадок в зоне изолирующих стыков
    /// 1 проезд = 1 <trip>, внутри него все выбранные пути.
    /// Структура XML:
    /// report
    ///   trip (@tripId, @tripDate, @direction, @directioncode, ...)
    ///     directions
    ///       tracks (@track, @trackId)
    ///         main ...
    /// </summary>
    public class izo_prosadka : ALARm.Core.Report.GraphicDiagrams
    {
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            // ===== 1) Выбор путей как TrackChoice (как в Deviation_close_to_the_limit) =====
            List<ChoiseForm.TrackChoice> selectedTracks = new List<ChoiseForm.TrackChoice>();

            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();

                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;

                var propSelectedTracks = choiceForm.GetType().GetProperty("SelectedTracks");
                if (propSelectedTracks != null)
                {
                    var val = propSelectedTracks.GetValue(choiceForm, null);
                    if (val is IEnumerable<ChoiseForm.TrackChoice> list)
                        selectedTracks = list.ToList();
                }

                if (selectedTracks.Count == 0 && choiceForm.admTracksIDs != null && choiceForm.admTracksIDs.Count > 0)
                {
                    selectedTracks = choiceForm.admTracksIDs.Select(id => new ChoiseForm.TrackChoice
                    {
                        TrackId = id,
                        TrackName = AdmStructureService.GetTrackName(id)?.ToString() ?? id.ToString(),
                        DirectionName = "",
                        DirectionId = 0
                    }).ToList();
                }

                if (selectedTracks.Count == 0)
                    return;
            }

            // ===== 2) Общие справочники =====
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

            // ===== 3) XML report =====
            var xdReport = new XDocument(new XElement("report"));

            // Группировка выбранных путей по направлению (как у тебя)
            var groups = selectedTracks
                .GroupBy(s => s.DirectionId > 0 ? $"ID:{s.DirectionId}" : $"NAME:{(s.DirectionName ?? "").Trim()}")
                .ToList();

            foreach (var tripProcess in tripProcesses)
            {
                var trip = RdStructureService.GetTrip(tripProcess.Id);
                if (trip == null)
                    continue;

                long tripId = trip.Id;

                // километры поездки (1 раз)
                var allKilometers = RdStructureService.GetKilometersByTrip(trip) ?? new List<Kilometer>();

                // S3 (1 раз на проезд)
                var listS3All = RdStructureService.GetS3(tripId) as List<S3>;
                listS3All = listS3All ?? new List<S3>();

                // ===== 4) на 1 проезд делаем ОДИН <trip> =====
                string directionCode = "";
                try { directionCode = tripProcess.DirectionCode ?? ""; } catch { directionCode = ""; }

                // directionTitle: если в форме есть DirectionName — используем, иначе tripProcess.Direction
                // (но т.к. в одном проезде может быть несколько направлений в выборе, мы печатаем направление в блоке пути в XSL.
                // Здесь кладём "общее" (для шапки), а конкретику дадим в tracks-атрибутах.)
                string headerDirectionTitle = tripProcess.Direction ?? "";
                if (string.IsNullOrWhiteSpace(headerDirectionTitle))
                    headerDirectionTitle = tripProcess.Direction_Name ?? "";

                var tripElem = new XElement("trip",
                    new XAttribute("version", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                    new XAttribute("direction", headerDirectionTitle),
                    new XAttribute("directioncode", directionCode),
                    new XAttribute("check", tripProcess.GetProcessTypeName ?? ""),
                    new XAttribute("road", road ?? ""),
                    new XAttribute("distance", distance.Code ?? ""),
                    new XAttribute("periodDate", period.Period ?? ""),
                    new XAttribute("chief", tripProcess.Chief ?? ""),
                    new XAttribute("ps", tripProcess.Car ?? ""),
                    new XAttribute("tripId", tripId),
                    new XAttribute("tripDate", trip.Trip_date.ToString("dd.MM.yyyy HH:mm:ss"))
                );

                var xeDirectionsRoot = new XElement("directions");

                // ===== 5) Добавляем ВСЕ выбранные пути внутрь одного trip =====
                foreach (var dirGroup in groups)
                {
                    var first = dirGroup.First();

                    string dirTitle = (first.DirectionName ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(dirTitle))
                        dirTitle = headerDirectionTitle;

                    // Если ты хочешь именно: "Иркутск-Чита(13807)" — это в XSL соберём
                    // directioncode берём общий tripProcess.DirectionCode
                    foreach (var sel in dirGroup)
                    {
                        long trackId = sel.TrackId;

                        string trackNameForPrint = (sel.TrackName ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(trackNameForPrint))
                            trackNameForPrint = AdmStructureService.GetTrackName(trackId)?.ToString() ?? trackId.ToString();

                        var xeTracks = new XElement("tracks",
                            new XAttribute("track", trackNameForPrint),
                            new XAttribute("trackId", trackId),
                            new XAttribute("direction", dirTitle),
                            new XAttribute("directioncode", directionCode)
                        );

                        // километры только этого пути
                        var kilometers = allKilometers.Where(k => k.Track_id == trackId).ToList();

                        // если по пути вообще нет километров — печатаем пустой блок
                        if (kilometers.Count == 0)
                        {
                            xeDirectionsRoot.Add(xeTracks);
                            continue;
                        }

                        // диапазон км (FilterForm) — оставил как было (если хочешь: 1 раз на проезд — скажи, переделаю)
                        List<Kilometer> kilometerssort = null;
                        try
                        {
                            int distCode = 0;
                            int.TryParse(distance.Code, out distCode);

                            int trackNum = 0;
                            int.TryParse(trackNameForPrint, out trackNum);

                            kilometerssort = RdStructureService.GetKilometersByTripdistanceperiod(trip, distCode, trackNum);
                        }
                        catch
                        {
                            kilometerssort = null;
                        }

                        var lkm = (kilometerssort ?? kilometers).Select(o => o.Number).Distinct().ToList();
                        if (lkm.Count == 0)
                        {
                            xeDirectionsRoot.Add(xeTracks);
                            continue;
                        }

                        var filterForm = new FilterForm();
                        var filters = new List<Filter>
                        {
                            new FloatFilter { Name = "Начало (км)", Value = lkm.Min() },
                            new FloatFilter { Name = "Конец (км)",  Value = lkm.Max() }
                        };
                        filterForm.SetDataSource(filters);

                        if (filterForm.ShowDialog() == DialogResult.Cancel)
                            return;

                        float kmFrom = (float)filters[0].Value;
                        float kmTo = (float)filters[1].Value;

                        kilometers = kilometers
                            .Where(km => kmFrom <= km.Number && km.Number <= kmTo)
                            .ToList();

                        kilometers = (trip.Travel_Direction == Direction.Reverse
                            ? kilometers.OrderBy(o => o.Number)
                            : kilometers.OrderByDescending(o => o.Number))
                            .ToList();

                        if (kilometers.Count == 0)
                        {
                            xeDirectionsRoot.Add(xeTracks);
                            continue;
                        }

                        // ===== main строки =====
                        const string ISO_FLAG = "ис;";
                        var kmNums = new HashSet<int>(kilometers.Select(k => k.Number));

                        var rows = listS3All
                            .Where(s =>
                                (s.Ots == "Пр.п" || s.Ots == "Пр.л") &&
                                (s.Primech ?? "") == ISO_FLAG &&
                                kmNums.Contains(s.Km))
                            .OrderByDescending(s => s.Km)
                            .ThenBy(s => s.Meter)
                            .ToList();

                        foreach (var s3 in rows)
                        {
                            var xeMain = new XElement("main",
                                new XAttribute("pchu", s3.Pchu),
                                new XAttribute("pd", s3.Pd),
                                new XAttribute("pdb", s3.Pdb),
                                new XAttribute("km", s3.Km),
                                new XAttribute("m", s3.Meter),
                                new XAttribute("data", s3.TripDateTime.ToString("dd.MM.yyyy")),
                                new XAttribute("Ots", s3.Ots ?? ""),
                                new XAttribute("Otkl", s3.Otkl),
                                new XAttribute("len", s3.Len),
                                new XAttribute("vpz", $"{s3.Uv}/{s3.Uvg}"),
                                new XAttribute("vogr", $"{s3.Ovp}/{s3.Ogp}"),
                                new XAttribute("Primech", s3.Primech ?? "")
                            );

                            xeTracks.Add(xeMain);
                        }

                        xeDirectionsRoot.Add(xeTracks);
                    }
                }

                tripElem.Add(xeDirectionsRoot);
                xdReport.Root.Add(tripElem);
            }

            // ===== 6) XSL -> HTML =====
            var htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                var transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }

            // ===== 7) Save/Open =====
            try
            {
                var outPath = Path.Combine(Path.GetTempPath(), "report.html");
                htReport.Save(outPath);
                htReport.Save(@"G:\form\1.Основные и дополнительные параметры геометрии рельсовой колеи (ГРК)\8.Ведомость просадок в зоне изолирующих стыков.html");
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
