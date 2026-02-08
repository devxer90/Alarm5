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
    /// Отступление, близкие к предельным
    /// Структура:
    /// trip -> directions -> direction -> tracks -> (main/add rows)
    /// </summary>
    public class Deviation_close_to_the_limit : ALARm.Core.Report.GraphicDiagrams
    {
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            // ===== 1) Выбор путей (TrackChoice) =====
            List<ChoiseForm.TrackChoice> selectedTracks = new List<ChoiseForm.TrackChoice>();

            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();

                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;

                // Если у тебя уже добавлено свойство SelectedTracks — берём его.
                // Иначе fallback: только TrackId (без направления/имени пути)
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

            var tripProcesses = RdStructureService.GetMainParametersProcess(period, distance.Name) ?? new List<MainParametersProcess>();
            if (tripProcesses.Count == 0)
            {
                MessageBox.Show(Properties.Resources.paramDataMissing);
                return;
            }

            // ===== 3) XML =====
            var xdReport = new XDocument(new XElement("report"));

            foreach (var tripProcess in tripProcesses)
            {
                // trip объект
                var trip = RdStructureService.GetTrip(tripProcess.Id);
                if (trip == null)
                    continue;

                // километры поездки (один раз)
                var allKilometers = RdStructureService.GetKilometersByTrip(trip) ?? new List<Kilometer>();

                // S3 MAIN и ADD грузим один раз на tripId
                long tripId = trip.Id;

                var listS3All = RdStructureService.GetS3(tripId) as List<S3>;
                listS3All = listS3All ?? new List<S3>();

                var addParam = AdditionalParametersService.GetAddParam(tripId) as List<S3>;
                addParam = addParam ?? new List<S3>();

                // список “близкие к предельным”
                var closeToDanger = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    DigressionName.RampNear.Name,
                    DigressionName.IzoGapNear.Name,
                    DigressionName.SpeedUpNear.Name,
                    DigressionName.PatternRetractionNear.Name,
                    DigressionName.LevelReverse.Name,
                    DigressionName.Level150.Name,
                    DigressionName.Level75.Name,
                    DigressionName.GapSimbol.Name
                };

                // итоги по ПЧ (по этому проезду)
                int drawdownCount = 0;
                int skewnessCount = 0;
                int PMCount = 0;
                int anpCount = 0;
                int anpQuestionCount = 0;
                int IBLCount = 0;

                int countDistance = 0;

                // trip header
                var tripElem = new XElement("trip",
                    new XAttribute("version", $"{DateTime.Now} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                    new XAttribute("check", tripProcess.GetProcessTypeName ?? ""),
                    new XAttribute("road", road ?? ""),
                    new XAttribute("distance", distance.Code ?? ""),
                    new XAttribute("periodDate", period.Period ?? ""),
                    new XAttribute("chief", tripProcess.Chief ?? ""),
                    new XAttribute("ps", tripProcess.Car ?? ""),
                    new XAttribute("tripDate", trip.Trip_date.ToString("dd.MM.yyyy")),
                    new XAttribute("tripId", tripId)
                );

                // directions root
                var directionsRoot = new XElement("directions");

                // ===== 4) Группируем выбранные пути по направлению (берём из формы!) =====
                // Ключ: DirectionId (если есть) иначе DirectionName
                var groups = selectedTracks
                    .GroupBy(s => s.DirectionId > 0
                        ? $"ID:{s.DirectionId}"
                        : $"NAME:{(s.DirectionName ?? "").Trim()}")
                    .ToList();

                foreach (var dirGroup in groups)
                {
                    // Заголовок направления для печати
                    var first = dirGroup.First();
                    string directionTitle =
                        (first.DirectionName ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(directionTitle))
                        directionTitle = tripProcess.DirectionName ?? "";

                    var xeDirection = new XElement("direction",
                        new XAttribute("direction", directionTitle),
                        new XAttribute("directionId", first.DirectionId)
                    );

                    // ===== 5) Каждый выбранный путь внутри направления =====
                    foreach (var sel in dirGroup)
                    {
                        long trackId = sel.TrackId;

                        // путь печатаем строго из формы
                        string trackNameForPrint = (sel.TrackName ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(trackNameForPrint))
                            trackNameForPrint = AdmStructureService.GetTrackName(trackId)?.ToString() ?? trackId.ToString();

                        var xeTracks = new XElement("tracks",
                            new XAttribute("track", trackNameForPrint),
                            new XAttribute("trackId", trackId),
                            new XAttribute("countbyput", 0)
                        );

                        // километры только этого пути
                        var kilometers = allKilometers.Where(k => k.Track_id == trackId).ToList();

                        // даже если пусто — печатаем пустой блок
                        if (kilometers.Count == 0)
                        {
                            xeDirection.Add(xeTracks);
                            continue;
                        }

                        // ===== 5.1) диапазон км (для формы Начало/Конец) =====
                        List<Kilometer> kilometerssort = null;
                        try
                        {
                            // GetKilometersByTripdistanceperiod требует номер пути как int
                            // пытаемся распарсить TrackNameForPrint (обычно "1","2","3")
                            if (!int.TryParse(trackNameForPrint, out int trackNum))
                                trackNum = 0;

                            int distCode = 0;
                            int.TryParse(distance.Code, out distCode);

                            kilometerssort = RdStructureService.GetKilometersByTripdistanceperiod(trip, distCode, trackNum);
                        }
                        catch
                        {
                            kilometerssort = null;
                        }

                        var lkm = (kilometerssort ?? kilometers).Select(o => o.Number).ToList();
                        if (lkm.Count == 0)
                        {
                            xeDirection.Add(xeTracks);
                            continue;
                        }

                        // ===== 5.2) форма Начало/Конец км =====
                        var filterForm = new FilterForm();
                        var filters = new List<Filter>
                        {
                            new FloatFilter { Name = "Начало (км)", Value = lkm.Min() },
                            new FloatFilter { Name = "Конец (км)", Value = lkm.Max() }
                        };

                        filterForm.SetDataSource(filters);
                        if (filterForm.ShowDialog() == DialogResult.Cancel)
                            return;

                        kilometers = kilometers
                            .Where(km => (float)filters[0].Value <= km.Number && km.Number <= (float)filters[1].Value)
                            .ToList();

                        kilometers = (trip.Travel_Direction == Direction.Reverse
                            ? kilometers.OrderBy(o => o.Number)
                            : kilometers.OrderByDescending(o => o.Number))
                            .ToList();

                        if (kilometers.Count == 0)
                        {
                            xeDirection.Add(xeTracks);
                            continue;
                        }

                        // ===== 5.3) сбор строк main/add =====
                        int countByPut = 0;

                        foreach (var km in kilometers)
                        {
                            km.LoadTrackPasport(MainTrackStructureRepository, trip.Trip_date);

                            var listS3Km = listS3All.Where(o => o.Km == km.Number).ToList();

                            // MAIN
                            var pruMain = listS3Km.Where(o => closeToDanger.Contains(o.Ots)).ToList();
                            pruMain.AddRange(listS3Km.Where(o => (o.Primech ?? "").Contains("ис?")).ToList());

                            // ADD
                            var pruAdd = addParam
                                .Where(o => o.Km == km.Number &&
                                            (closeToDanger.Contains(o.Ots) ||
                                             (!string.IsNullOrWhiteSpace(o.Primech) && closeToDanger.Contains(o.Primech))))
                                .ToList();

                            // MAIN rows
                            foreach (var s3 in pruMain)
                            {
                                // счетчики
                                switch (s3.Ots)
                                {
                                    case "Пр.п":
                                    case "Пр.л":
                                        drawdownCount++;
                                        break;

                                    case "?Анп":
                                        anpQuestionCount++;
                                        break;

                                    case "Анп":
                                        anpCount++;
                                        break;
                                }

                                var otklText = (s3.Ots == "?Анп")
                                    ? (s3.Primech ?? "")
                                    : s3.Otkl.ToString("0.0");

                                var xeMain = new XElement("main",
                                    new XAttribute("km", s3.Km),
                                    new XAttribute("m", s3.Meter),
                                    new XAttribute("Data", s3.TripDateTime.ToString("dd.MM.yyyy")),
                                    new XAttribute("Ots", s3.Ots ?? ""),
                                    new XAttribute("Otkl", otklText),
                                    new XAttribute("len", s3.Len),
                                    new XAttribute("vpz", s3.Uv + "/" + s3.Uvg),
                                    new XAttribute("Primech", (s3.Primech == "м;") ? "мост" : "")
                                );

                                xeTracks.Add(xeMain);
                                countByPut += 1;
                            }

                            // ADD rows
                            foreach (var s3 in pruAdd)
                            {
                                if (s3.Ots == "Иб.л")
                                    IBLCount += s3.Kol;

                                string vpsz = "";
                                try
                                {
                                    if (km.Speeds != null && km.Speeds.Count > 0)
                                        vpsz = km.Speeds.Last().Passenger + "/" + km.Speeds.Last().Freight;
                                }
                                catch { }

                                var xeAdd = new XElement("add",
                                    new XAttribute("km", s3.Km),
                                    new XAttribute("m", s3.Meter),
                                    new XAttribute("Data", trip.Trip_date.ToString("dd.MM.yyyy")),
                                    new XAttribute("Ots", s3.Ots ?? ""),
                                    new XAttribute("Otkl", s3.Otkl.ToString("0.0")),
                                    new XAttribute("len", s3.Len),
                                    new XAttribute("vpz", vpsz),
                                    new XAttribute("Primech", "")
                                );

                                xeTracks.Add(xeAdd);

                                // по доп.парам Kol может быть > 1
                                countByPut += Math.Max(1, s3.Kol);
                            }
                        }

                        xeTracks.SetAttributeValue("countbyput", countByPut);
                        countDistance += countByPut;

                        xeDirection.Add(xeTracks);
                    }

                    directionsRoot.Add(xeDirection);
                }

                // totals (“в том числе”)
                if (drawdownCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Пр - " + drawdownCount)));
                if (skewnessCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "П - " + skewnessCount)));
                if (PMCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "П м - " + PMCount)));
                if (IBLCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Ибл - " + IBLCount)));
                if (anpCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Анп - " + anpCount)));
                if (anpQuestionCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "?Анп - " + anpQuestionCount)));

                tripElem.SetAttributeValue("countDistance", countDistance);
                tripElem.Add(directionsRoot);

                xdReport.Root.Add(tripElem);
            }

            // ===== 4) XSL -> HTML =====
            var htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                var transform = new XslCompiledTransform();

                // ВАЖНО: template.Xsl должен начинаться СРАЗУ с <?xml ... ?> без пробелов/комментов
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }

            // ===== 5) Сохранить и открыть =====
            try
            {
                var outPath = Path.Combine(Path.GetTempPath(), "report.html");
                htReport.Save(outPath);
                System.Diagnostics.Process.Start(outPath);
            }
            catch (Exception e)
            {
                MessageBox.Show("Ошибка сохранения файла: " + e);
            }
        }
    }
}
