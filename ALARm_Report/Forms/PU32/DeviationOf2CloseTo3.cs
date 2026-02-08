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
    /// Отступления 2 степени, близкие к 3 (логика как в Ф.О3):
    /// - ГЛАВНОЕ: строим отчет ОТ ВЫБОРА в форме (направление+путь),
    ///   а не от списка проездов. Тогда выбранное направление никогда не "пропадет".
    /// Структура XML:
    /// report -> trip -> directions(direction,directioncode) -> tracks(track,countbyput,nodata) -> note(...)
    /// </summary>
    public class DeviationOf2CloseTo3 : Report
    {
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            // ===== 1) Выбор путей/направлений =====
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

            // Уникализируем (чтобы не было дублей из грида)
            selectedTracks = selectedTracks
                .Where(x => x != null)
                .GroupBy(x => new
                {
                    x.TrackId,
                    x.DirectionId,
                    DirName = (x.DirectionName ?? "").Trim(),
                    TrName = (x.TrackName ?? "").Trim()
                })
                .Select(g => g.First())
                .ToList();

            // Группы выбора по направлению (ключ: CODE:13863 или NAME:Лесная-Голубичная)
            var selectedGroups = selectedTracks
                .GroupBy(x =>
                {
                    var code = NormalizeDirectionCode(ExtractDirectionCode(x.DirectionName));
                    var name = StripDirectionCode(x.DirectionName ?? "").Trim();
                    return !string.IsNullOrWhiteSpace(code) ? $"CODE:{code}" : $"NAME:{name}";
                })
                .ToList();

            // ===== 2) Справочники =====
            var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;
            if (distance == null)
            {
                MessageBox.Show("Не удалось определить ПЧ (distance).");
                return;
            }

            var roadName = AdmStructureService.GetRoadName(distance.Id, AdmStructureConst.AdmDistance, true);
            distance.Name = (distance.Name ?? "").Replace("ПЧ-", "");

            // ===== 3) Проезды по ПЧ/периоду (как PRU) =====
            var tripProcesses = RdStructureService.GetTripsOnDistance(parentId, period);
            if (tripProcesses == null)
                tripProcesses = new List<ALARm.Core.Trips>();


            // ===== 4) Формирование HTML =====
            XDocument htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                var xdReport = new XDocument(new XElement("report"));

                bool addedAnyTrip = false;

                // ===== ГЛАВНЫЙ ЦИКЛ: ИДЕМ ПО ВЫБОРУ (направлениям), а не по tripProcesses =====
                foreach (var g in selectedGroups)
                {
                    var firstSel = g.First();

                    string dirNameRaw = firstSel.DirectionName ?? "";
                    string dirCode = NormalizeDirectionCode(ExtractDirectionCode(dirNameRaw));
                    string dirName = StripDirectionCode(dirNameRaw);

                    // Находим все проезды по этому направлению
                    var tpsForDir = tripProcesses.Where(tp =>
                    {
                        // tp.DirectionCode / tp.Direction_Name / tp.Direction в разных местах — нормализуем
                        string tpCode = NormalizeDirectionCode((tp.DirectionCode ?? "").Trim());
                        if (string.IsNullOrWhiteSpace(tpCode))
                            tpCode = NormalizeDirectionCode(ExtractDirectionCode(tp.Direction_Name ?? tp.Direction ?? ""));

                        if (!string.IsNullOrWhiteSpace(dirCode))
                            return string.Equals(tpCode, dirCode, StringComparison.OrdinalIgnoreCase);

                        // fallback по имени
                        var tpName = StripDirectionCode(tp.Direction_Name ?? tp.Direction ?? "").Trim();
                        return string.Equals(tpName, (dirName ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
                    }).ToList();

                    // Если проездов нет — печатаем "пустую ведомость" по выбранным путям (направление не пропадает)
                    if (tpsForDir.Count == 0)
                    {
                        AppendEmptyTripForDirection(xdReport, g.ToList(), distance, roadName, period);
                        addedAnyTrip = true;
                        continue;
                    }

                    // Есть проезды по направлению — печатаем каждый
                    foreach (var tp in tpsForDir)
                    {
                        var trip = RdStructureService.GetTrip(tp.Id);
                        if (trip == null)
                            continue;

                        var allKilometers = RdStructureService.GetKilometersByTrip(trip) ?? new List<Kilometer>();

                        // ===== trip header (под наш XSL: trip_id / trip_date) =====
                        var tripElem = new XElement("trip",
                            new XAttribute("version", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                            new XAttribute("trip_id", trip.Id),
                            new XAttribute("trip_date", trip.Trip_date.ToString("dd.MM.yyyy HH:mm")),
                            new XAttribute("check", tp.GetProcessTypeName ?? ""),
                            new XAttribute("road", roadName ?? ""),
                            new XAttribute("distance", distance.Code ?? ""),
                            new XAttribute("periodDate", period?.Period ?? ""),
                            new XAttribute("chief", tp.Chief ?? ""),
                            new XAttribute("ps", tp.Car ?? ""),
                            new XAttribute("countDistance", 0)
                        );

                        // ===== Отступления 2 степени =====
                        var notesAll = RdStructureService.GetDigressions(trip.Id, distance.Code, new int[] { 2 }) ?? new List<Digression>();

                        // Проставим ПЧУ/ПД/ПДБ
                        var dist_section = MainTrackStructureService.GetDistSectionByDistId(distance.Id);
                        if (dist_section != null && dist_section.Count > 0)
                        {
                            foreach (var n in notesAll)
                            {
                                try
                                {
                                    var sect = dist_section.FirstOrDefault(o =>
                                        n.Km * 1000 + n.Meter >= o.Start_Km * 1000 + o.Start_M &&
                                        n.Km * 1000 + n.Meter <= o.Final_Km * 1000 + o.Final_M);

                                    if (sect != null)
                                    {
                                        n.PCHU = sect.Pchu.ToString();
                                        n.PD = sect.Pd.ToString();
                                        n.PDB = sect.Pdb.ToString();
                                    }
                                }
                                catch { }
                            }
                        }

                        // ===== directions элемент (один на направление) =====
                        var xeDirections = new XElement("directions",
                            new XAttribute("direction", dirName),
                            new XAttribute("directioncode", dirCode)
                        );

                        int countDistance = 0;

                        // "В том числе" — как у тебя
                        int drawdownCount = 0;      // Пр
                        int levelCount = 0;         // У
                        int skewnessCount = 0;      // П
                        int straighteningCount = 0; // Р

                        // Печатаем пути ровно из выбранной группы g
                        var tracksDistinct = g
                            .GroupBy(t => new { t.TrackId, Name = (t.TrackName ?? "").Trim() })
                            .Select(gr => gr.First())
                            .ToList();

                        foreach (var tr in tracksDistinct)
                        {
                            long trackId = tr.TrackId;

                            string trackName = (tr.TrackName ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(trackName))
                                trackName = (AdmStructureService.GetTrackName(trackId)?.ToString() ?? trackId.ToString()).Trim();

                            var xeTracks = new XElement("tracks",
                                new XAttribute("direction", dirName),
                                new XAttribute("directioncode", dirCode),
                                new XAttribute("track", trackName),
                                new XAttribute("countbyput", 0),
                                new XAttribute("nodata", 1)
                            );

                            // километры по пути
                            var kmsByTrack = (allKilometers ?? new List<Kilometer>())
                                .Where(k => k.Track_id == trackId)
                                .ToList();

                            if (kmsByTrack.Count == 0)
                            {
                                // нет км -> нет данных
                                xeDirections.Add(xeTracks);
                                continue;
                            }

                            var kmSet = new HashSet<int>(kmsByTrack.Select(k => k.Number));

                            var trackNotes = notesAll
                                .Where(n => kmSet.Contains(n.Km))
                                .OrderBy(n => n.Km * 1000 + n.Meter)
                                .ToList();

                            if (trackNotes.Count == 0)
                            {
                                // нет записей -> нет данных
                                xeDirections.Add(xeTracks);
                                continue;
                            }

                            xeTracks.SetAttributeValue("nodata", null);

                            int countByPut = 0;

                            foreach (var note in trackNotes)
                            {
                                // пропуски как у тебя
                                if (note.Name == "Суж" || note.Name == "Уш")
                                    continue;

                                // пересчет для "У" как у тебя
                                if (note.Name == "У")
                                    note.Count = note.Length / 10 + ((note.Length % 10 + 1 > 1) ? 1 : 0);

                                // счетчики "в том числе"
                                switch (note.Name)
                                {
                                    case "Пр.п":
                                    case "Пр.л":
                                        drawdownCount += note.Count;
                                        break;
                                    case "У":
                                        levelCount += note.Count;
                                        break;
                                    case "П":
                                        skewnessCount += note.Count;
                                        break;
                                    case "Р":
                                        straighteningCount += note.Count;
                                        break;
                                }

                                countByPut += note.Count;

                                xeTracks.Add(new XElement("note",
                                    new XAttribute("pchu", (note.PCHU ?? note.Pchu ?? "").Trim()),
                                    new XAttribute("pd", (note.PD ?? "").Trim()),
                                    new XAttribute("pdb", (note.PDB ?? "").Trim()),
                                    new XAttribute("km", note.Km),
                                    new XAttribute("m", note.Meter),
                                    new XAttribute("found_date", note.FoundDate.ToString("dd.MM.yyyy")),
                                    new XAttribute("deviation", note.Name ?? ""),
                                    new XAttribute("digression", note.Value),
                                    new XAttribute("len", note.Length),
                                    new XAttribute("count", note.Count),
                                    new XAttribute("primech", note.Primech ?? "")
                                ));
                            }

                            xeTracks.SetAttributeValue("countbyput", countByPut);
                            countDistance += countByPut;

                            xeDirections.Add(xeTracks);
                        }

                        tripElem.Add(xeDirections);

                        // totals
                        if (drawdownCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Пр - " + drawdownCount)));
                        if (levelCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "У - " + levelCount)));
                        if (skewnessCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "П - " + skewnessCount)));
                        if (straighteningCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Р - " + straighteningCount)));

                        tripElem.SetAttributeValue("countDistance", countDistance);

                        xdReport.Root.Add(tripElem);
                        addedAnyTrip = true;
                    }
                }

                // На всякий случай: если вообще ничего не добавили — печатаем полностью пустой отчет по выбору
                if (!addedAnyTrip)
                {
                    foreach (var g in selectedGroups)
                        AppendEmptyTripForDirection(xdReport, g.ToList(), distance, roadName, period);
                }

                // ===== XSL Transform =====
                var transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }

            // ===== Сохранить и открыть =====
            try
            {
                var outPath = Path.Combine(Path.GetTempPath(), "report.html");
                htReport.Save(outPath);
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

        /// <summary>
        /// Печатает пустой trip (направление+пути) с nodata=1,
        /// чтобы выбранное направление не "пропадало", даже если проездов нет.
        /// </summary>
        private static void AppendEmptyTripForDirection(
            XDocument xdReport,
            List<ChoiseForm.TrackChoice> tracks,
            AdmUnit distance,
            string roadName,
            ReportPeriod period)
        {
            if (xdReport?.Root == null || tracks == null || tracks.Count == 0) return;

            var first = tracks.First();

            string dirNameRaw = first.DirectionName ?? "";
            string dirCode = NormalizeDirectionCode(ExtractDirectionCode(dirNameRaw));
            string dirName = StripDirectionCode(dirNameRaw);

            var tripElem = new XElement("trip",
                new XAttribute("version", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss}"),
                new XAttribute("trip_id", ""),
                new XAttribute("trip_date", ""),
                new XAttribute("check", ""),
                new XAttribute("road", roadName ?? ""),
                new XAttribute("distance", distance?.Code ?? ""),
                new XAttribute("periodDate", period?.Period ?? ""),
                new XAttribute("chief", ""),
                new XAttribute("ps", ""),
                new XAttribute("countDistance", 0)
            );

            var xeDirections = new XElement("directions",
                new XAttribute("direction", dirName),
                new XAttribute("directioncode", dirCode)
            );

            var tracksDistinct = tracks
                .GroupBy(t => new { t.TrackId, Name = (t.TrackName ?? "").Trim() })
                .Select(g => g.First())
                .ToList();

            foreach (var tr in tracksDistinct)
            {
                string trackName = (tr.TrackName ?? "").Trim();
                if (string.IsNullOrWhiteSpace(trackName))
                    trackName = tr.TrackId.ToString();

                xeDirections.Add(new XElement("tracks",
                    new XAttribute("direction", dirName),
                    new XAttribute("directioncode", dirCode),
                    new XAttribute("track", trackName),
                    new XAttribute("countbyput", 0),
                    new XAttribute("nodata", 1)
                ));
            }

            tripElem.Add(xeDirections);
            xdReport.Root.Add(tripElem);
        }

        private static string ExtractDirectionCode(string directionName)
        {
            if (string.IsNullOrWhiteSpace(directionName)) return "";
            int i1 = directionName.LastIndexOf('(');
            int i2 = directionName.LastIndexOf(')');
            if (i1 >= 0 && i2 > i1)
            {
                var inside = directionName.Substring(i1 + 1, i2 - i1 - 1).Trim();
                var digits = new string(inside.Where(char.IsDigit).ToArray());
                return string.IsNullOrWhiteSpace(digits) ? inside : digits;
            }
            return "";
        }

        private static string StripDirectionCode(string directionName)
        {
            if (string.IsNullOrWhiteSpace(directionName)) return "";
            int i = directionName.LastIndexOf('(');
            if (i > 0) return directionName.Substring(0, i).Trim();
            return directionName.Trim();
        }

        /// <summary>
        /// Нормализует код направления: оставляет только цифры (на случай " 13863 " / "013863" / "13863)")
        /// </summary>
        private static string NormalizeDirectionCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return "";
            var digits = new string(code.Where(char.IsDigit).ToArray());
            return digits.TrimStart('0'); // если у вас коды могут идти с ведущими нулями
        }

        public override string ToString()
        {
            return "Отступления 2 степени, близкие к 3";
        }
    }
}
