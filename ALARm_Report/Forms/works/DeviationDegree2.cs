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
    /// Отступления 2 степени
    /// XML: report/trip/directions/tracks/note + total
    /// Рабочая логика:
    /// - directions всегда содержит хотя бы один tracks (если нет данных -> tracks nodata=1 + строка "нет данных" в XSL)
    /// - выбор идет по SelectedTracks (направление+путь), поэтому "Путь 2" в разных направлениях не теряется
    /// </summary>
    public class DeviationDegree2 : Report
    {
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            // =========================
            // 1) Выбор путей + направлений (как в рабочих вариантах)
            // =========================
            List<ChoiseForm.TrackChoice> selectedTracks;
            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();

                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;

                selectedTracks = choiceForm.SelectedTracks ?? new List<ChoiseForm.TrackChoice>();
            }

            if (selectedTracks == null || selectedTracks.Count == 0)
                return;

            // Уникализируем выбранные строки
            selectedTracks = selectedTracks
                .Where(x => x != null)
                .GroupBy(x => new
                {
                    x.TrackId,
                    Dir = (x.DirectionName ?? "").Trim(),
                    Tr = (x.TrackName ?? "").Trim()
                })
                .Select(g => g.First())
                .ToList();

            // Группы выбора по направлению (код из скобок)
            var selectedGroups = selectedTracks
                .GroupBy(x =>
                {
                    var code = NormalizeDirectionCode(ExtractDirectionCode(x.DirectionName));
                    var name = StripDirectionCode(x.DirectionName ?? "").Trim();
                    return !string.IsNullOrWhiteSpace(code) ? $"CODE:{code}" : $"NAME:{name}";
                })
                .ToList();

            // =========================
            // 2) Справочники / проезды
            // =========================
            var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;
            if (distance == null)
            {
                MessageBox.Show("Не удалось определить ПЧ (distance).");
                return;
            }

            var road = AdmStructureService.GetRoadName(distance.Id, AdmStructureConst.AdmDistance, true);
            distance.Name = (distance.Name ?? "").Replace("ПЧ-", "");

            var tripProcesses = RdStructureService.GetMainParametersProcess(period, distance.Name);
            if (tripProcesses == null || tripProcesses.Count == 0)
            {
                MessageBox.Show(Properties.Resources.paramDataMissing);
                return;
            }

            // Для PCHU/PD/PDB
            var distSections = MainTrackStructureService.GetDistSectionByDistId(distance.Id) ?? new List<DistSection>();

            // =========================
            // 3) XML + Transform
            // =========================
            XDocument htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                var xdReport = new XDocument(new XElement("report"));

                foreach (var tripProcess in tripProcesses)
                {
                    // trip для даты
                    var tripObj = RdStructureService.GetTrip(tripProcess.Id);
                    string tripDateStr = GetTripDateString(tripObj);

                    // S3 степени 2 грузим один раз (как и надо)
                    var s3All = RdStructureService.GetS3(tripProcess.Trip_id, 2, distance.Name) as List<S3>;
                    s3All = s3All ?? new List<S3>();

                    // Проставим PCHU/PD/PDB
                    if (distSections.Count > 0 && s3All.Count > 0)
                    {
                        foreach (var s in s3All)
                        {
                            try
                            {
                                var sect = distSections.FirstOrDefault(o =>
                                    s.Km * 1000 + s.Meter >= o.Start_Km * 1000 + o.Start_M &&
                                    s.Km * 1000 + s.Meter <= o.Final_Km * 1000 + o.Final_M);

                                if (sect != null)
                                {
                                    s.Pchu = sect.Pchu;
                                    s.Pd = sect.Pd;
                                    s.Pdb = sect.Pdb;
                                }
                            }
                            catch { }
                        }
                    }

                    // trip header (добавляем всегда)
                    var tripElem = new XElement("trip",
                        new XAttribute("version", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                        new XAttribute("date_statement", DateTime.Now.Date.ToShortDateString()),

                        new XAttribute("trip_id", tripProcess.Trip_id),
                        new XAttribute("trip_date", tripDateStr),

                        new XAttribute("check", tripProcess.GetProcessTypeName ?? ""),
                        new XAttribute("distance", distance.Code ?? ""),
                        new XAttribute("periodDate", period?.Period ?? ""),
                        new XAttribute("road", road ?? ""),
                        new XAttribute("chief", tripProcess.Chief ?? ""),
                        new XAttribute("ps", tripProcess.Car ?? ""),
                        new XAttribute("countDistance", 0)
                    );

                    // totals “в том числе”
                    int constrictionCount = 0;   // Суж
                    int broadeningCount = 0;     // Уш
                    int levelCount = 0;          // У
                    int skewnessCount = 0;       // П
                    int drawdownCount = 0;       // Пр
                    int straighteningCount = 0;  // Р
                    int RSTCount = 0;

                    int countDistance = 0;

                    // ===== ПЕЧАТАЕМ РОВНО ТО, ЧТО ВЫБРАЛИ (как в рабочих отчётах) =====
                    foreach (var dirGroup in selectedGroups)
                    {
                        var first = dirGroup.First();

                        string dirNameRaw = first.DirectionName ?? "";
                        string dirCode = NormalizeDirectionCode(ExtractDirectionCode(dirNameRaw));
                        string dirName = StripDirectionCode(dirNameRaw);

                        // directions (направление)
                        var xeDirection = new XElement("directions",
                            new XAttribute("direction", dirName),
                            new XAttribute("directioncode", dirCode)
                        );

                        bool hasAnyTracks = false;

                        // пути внутри направления (уникально)
                        var tracksDistinct = dirGroup
                            .GroupBy(t => new { t.TrackId, Name = (t.TrackName ?? "").Trim() })
                            .Select(g => g.First())
                            .ToList();

                        foreach (var tr in tracksDistinct)
                        {
                            long trackId = tr.TrackId;

                            string trackName = (tr.TrackName ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(trackName))
                                trackName = (AdmStructureService.GetTrackName(trackId)?.ToString() ?? trackId.ToString()).Trim();

                            // tracks создаем всегда (как в рабочих вариантах)
                            var xeTracks = new XElement("tracks",
                                new XAttribute("direction", dirName),
                                new XAttribute("directioncode", dirCode),
                                new XAttribute("track", trackName),
                                new XAttribute("countbyput", 0),
                                new XAttribute("nodata", 1)
                            );

                            // 1) Берём по пути
                            var byTrack = s3All
                                .Where(s => s.track_id == trackId)
                                .OrderBy(s => s.Km * 1000 + s.Meter)
                                .ToList();

                            // 2) Если можем — фильтруем по коду направления (НО безопасно!)
                            //    Частая проблема: Directcode пустой/другого формата => тогда всё "исчезает".
                            //    Поэтому: сперва пробуем с кодом, если пусто — берём без кода.
                            List<S3> rows = byTrack;

                            if (!string.IsNullOrWhiteSpace(dirCode))
                            {
                                var byCode = byTrack.Where(s =>
                                    string.Equals(NormalizeDirectionCode(Convert.ToString(s.Directcode)), dirCode, StringComparison.OrdinalIgnoreCase)
                                ).ToList();

                                if (byCode.Count > 0)
                                    rows = byCode;
                            }

                            // 3) Фильтры как у тебя
                            rows = rows.Where(s => s.Ots != "Анп" && s.Ots != "ПрУ" && s.Ots != "?Анп").ToList();

                            // Если после фильтра нет строк — оставляем nodata=1 (но tracks всё равно будет)
                            if (rows.Count == 0)
                            {
                                xeDirection.Add(xeTracks);
                                hasAnyTracks = true;
                                continue;
                            }

                            // Добавляем NOTE
                            int countByPut = 0;
                            bool anyNoteAdded = false;

                            foreach (var s in rows)
                            {
                                // Пересчеты и totals
                                switch (s.Ots)
                                {
                                    case "Пр.п":
                                    case "Пр.л":
                                        drawdownCount += s.Kol;
                                        break;

                                    case "Суж":
                                        s.Kol = s.Len / 4 + ((s.Len % 4 + 1 > 1) ? 1 : 0);
                                        constrictionCount += s.Kol;
                                        break;

                                    case "Уш":
                                        s.Kol = s.Len / 4 + ((s.Len % 4 + 1 > 1) ? 1 : 0);
                                        broadeningCount += s.Kol;
                                        break;

                                    case "У":
                                        // Оставил твою формулу как в присланном коде
                                        s.Kol = s.Len / 20 + ((s.Len % 10 + 1 > 1) ? 1 : 0);
                                        levelCount += s.Kol;
                                        break;

                                    case "П":
                                        skewnessCount += s.Kol;
                                        break;

                                    case "Р":
                                        straighteningCount += s.Kol;
                                        break;

                                    case "Рст":
                                        RSTCount += s.Kol;
                                        break;
                                }

                                countByPut += s.Kol;

                                xeTracks.Add(new XElement("note",
                                    new XAttribute("pchu", s.Pchu),
                                    new XAttribute("pd", s.Pd),
                                    new XAttribute("pdb", s.Pdb),
                                    new XAttribute("km", s.Km),
                                    new XAttribute("m", s.Meter),
                                    new XAttribute("found_date", SafeFoundDate(s.TripDateTime)),
                                    new XAttribute("deviation", s.Ots ?? ""),
                                    new XAttribute("digression", s.Otkl),
                                    new XAttribute("len", s.Len),
                                    new XAttribute("count", s.Kol),
                                    new XAttribute("primech", NormalizePrimech(s.Primech))
                                ));

                                anyNoteAdded = true;
                            }

                            if (anyNoteAdded)
                            {
                                xeTracks.SetAttributeValue("nodata", null);
                                xeTracks.SetAttributeValue("countbyput", countByPut);
                                countDistance += countByPut;
                            }

                            xeDirection.Add(xeTracks);
                            hasAnyTracks = true;
                        }

                        // ===== КАК В РАБОЧИХ ВАРИАНТАХ: directions НЕ ДОЛЖЕН БЫТЬ ПУСТЫМ =====
                        // Если вдруг ничего не добавилось (редко), добавим заглушку, чтобы не было пустого листа
                        if (!hasAnyTracks)
                        {
                            xeDirection.Add(new XElement("tracks",
                                new XAttribute("direction", dirName),
                                new XAttribute("directioncode", dirCode),
                                new XAttribute("track", "—"),
                                new XAttribute("countbyput", 0),
                                new XAttribute("nodata", 1)
                            ));
                        }

                        tripElem.Add(xeDirection);
                    }

                    // totals
                    if (drawdownCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Пр - " + drawdownCount)));
                    if (constrictionCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Суж - " + constrictionCount)));
                    if (broadeningCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Уш - " + broadeningCount)));
                    if (levelCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "У - " + levelCount)));
                    if (skewnessCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "П - " + skewnessCount)));
                    if (straighteningCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Р - " + straighteningCount)));
                    if (RSTCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Рст - " + RSTCount)));

                    tripElem.SetAttributeValue("countDistance", countDistance);

                    xdReport.Root.Add(tripElem);
                }

                // XSL -> HTML
                var transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }

            // =========================
            // 4) Сохранить и открыть
            // =========================
            try
            {
                var outPath = Path.Combine(Path.GetTempPath(), "report.html");
                htReport.Save(outPath);
            }
            catch
            {
                MessageBox.Show("Ошибка сохранения файла");
            }
            finally
            {
                System.Diagnostics.Process.Start(Path.Combine(Path.GetTempPath(), "report.html"));
            }
        }

        // -------------------------
        // Helpers
        // -------------------------

        private static string ExtractDirectionCode(string directionName)
        {
            if (string.IsNullOrWhiteSpace(directionName)) return "";
            int i1 = directionName.LastIndexOf('(');
            int i2 = directionName.LastIndexOf(')');
            if (i1 >= 0 && i2 > i1)
                return directionName.Substring(i1 + 1, i2 - i1 - 1).Trim();
            return "";
        }

        private static string StripDirectionCode(string directionName)
        {
            if (string.IsNullOrWhiteSpace(directionName)) return "";
            int i = directionName.LastIndexOf('(');
            if (i > 0) return directionName.Substring(0, i).Trim();
            return directionName.Trim();
        }

        private static string NormalizeDirectionCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "";
            var digits = new string(code.Where(char.IsDigit).ToArray());
            return digits.TrimStart('0'); // "013863" -> "13863"
        }

        private static string GetTripDateString(object trip)
        {
            if (trip == null) return "";
            try
            {
                var t = trip.GetType();
                var p =
                    t.GetProperty("Trip_date") ??
                    t.GetProperty("trip_date") ??
                    t.GetProperty("TripDate") ??
                    t.GetProperty("tripDate") ??
                    t.GetProperty("Trip_Date");

                if (p == null) return "";
                var v = p.GetValue(trip, null);
                if (v == null) return "";

                if (v is DateTime dt)
                    return dt.ToString("dd.MM.yyyy HH:mm:ss");

                return v.ToString();
            }
            catch
            {
                return "";
            }
        }

        private static string SafeFoundDate(DateTime dt)
        {
            try
            {
                if (dt.Year < 1900) return "";
                return dt.ToString("dd.MM.yyyy");
            }
            catch
            {
                return "";
            }
        }

        private static string NormalizePrimech(string primech)
        {
            if (string.IsNullOrWhiteSpace(primech)) return "";
            if (primech.Contains("м;")) return "мост";
            if (primech.Contains("Стр")) return "Стр";
            if (primech.Contains("гр-/60")) return "гр";
            return primech.Replace(";", "");
        }
    }
}
