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
    public class DeviationDegree3 : Report
    {
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            // ===== 1) Выбор путей (как в Deviation_close_to_the_limit) =====
            List<ChoiseForm.TrackChoice> selectedTracks = new List<ChoiseForm.TrackChoice>();

            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();

                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;

                // Берём SelectedTracks если есть (через reflection, чтобы не падать на старой версии)
                var propSelectedTracks = choiceForm.GetType().GetProperty("SelectedTracks");
                if (propSelectedTracks != null)
                {
                    var val = propSelectedTracks.GetValue(choiceForm, null);
                    if (val is IEnumerable<ChoiseForm.TrackChoice> list)
                        selectedTracks = list.ToList();
                }

                // fallback: старый вариант только TrackId
                if ((selectedTracks == null || selectedTracks.Count == 0) &&
                    choiceForm.admTracksIDs != null && choiceForm.admTracksIDs.Count > 0)
                {
                    selectedTracks = choiceForm.admTracksIDs.Select(id => new ChoiseForm.TrackChoice
                    {
                        TrackId = id,
                        TrackName = AdmStructureService.GetTrackName(id)?.ToString() ?? id.ToString(),
                        DirectionName = "",
                        DirectionId = 0
                    }).ToList();
                }

                if (selectedTracks == null || selectedTracks.Count == 0)
                    return;
            }

            // ===== 2) Подготовка справочников =====
            var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;
            if (distance == null)
            {
                MessageBox.Show("Не удалось определить ПЧ (distance).");
                return;
            }

            var road = AdmStructureService.GetRoadName(distance.Id, AdmStructureConst.AdmDistance, true);
            distance.Name = (distance.Name ?? "").Replace("ПЧ-", "");

            var curves = (MainTrackStructureService.GetCurves(parentId, MainTrackStructureConst.MtoCurve) as List<Curve>)
                ?.Where(c => c.Radius <= 1200)
                ?.OrderBy(c => c.Start_Km * 1000 + c.Start_M)
                ?.ToList() ?? new List<Curve>();

            if (!curves.Any())
            {
                MessageBox.Show("Нет данных по кривым (curves) для выбранного участка.");
                return;
            }

            var tripProcesses = RdStructureService.GetMainParametersProcess(period, distance.Name) ?? new List<MainParametersProcess>();
            if (tripProcesses.Count == 0)
            {
                MessageBox.Show(Properties.Resources.paramDataMissing);
                return;
            }

            // ===== 3) Строим XML отчёта =====
            XDocument htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                XDocument xdReport = new XDocument(new XElement("report"));

                foreach (var tripProcess in tripProcesses)
                {
                    // --- Счётчики итогов по ПЧ для этого tripProcess
                    int constrictionCount = 0;    // Суж
                    int broadeningCount = 0;      // Уш
                    int levelCount = 0;           // У
                    int skewnessCount = 0;        // П
                    int drawdownCount = 0;        // Пр
                    int straighteningCount = 0;   // Р / Рст
                    int RSTCount = 0;             // если нужно — добавишь логику

                    // Дата проезда и ID проезда
                    // В твоём исходнике было tripProcess.Trip_id для S3.
                    // Trip entity берём, если есть, чтобы взять Trip_date.
                    var tripEntity = RdStructureService.GetTrip(tripProcess.Id);
                    string tripDateHeader = tripEntity != null ? tripEntity.Trip_date.ToString("dd.MM.yyyy") : "нет данных";

                    // CurvesAdmUnits для шапки (как было)
                    var curvesAdmUnits = AdmStructureService.GetCurvesAdmUnits(curves[0].Id) as List<CurvesAdmUnits>;
                    var curvesAdmUnit = (curvesAdmUnits != null && curvesAdmUnits.Any()) ? curvesAdmUnits[0] : null;

                    XElement tripElem = new XElement("trip",
                        new XAttribute("version", $"{DateTime.Now} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                        new XAttribute("date_statement", DateTime.Now.Date.ToShortDateString()),
                        new XAttribute("direction", curvesAdmUnit?.Direction ?? ""),
                        new XAttribute("check", tripProcess.GetProcessTypeName ?? ""),
                        new XAttribute("track", curvesAdmUnit?.Track ?? ""),
                        new XAttribute("road", road ?? ""),
                        new XAttribute("distance", distance.Code ?? ""),
                        new XAttribute("periodDate", period.Period ?? ""),
                        new XAttribute("chief", tripProcess.Chief ?? ""),
                        new XAttribute("ps", tripProcess.Car ?? ""),
                        // новые поля (ты просил)
                        new XAttribute("trip_id", tripProcess.Trip_id),
                        new XAttribute("trip_date", tripDateHeader),
                        // общий итог по ПЧ
                        new XAttribute("countDistance", 0)
                    );

                    // Коррекция участков ПЧ (для Pchu/Pd/Pdb)
                    var dist_section = MainTrackStructureService.GetDistSectionByDistId(distance.Id);

                    // ===== 4) Группируем выбранные пути по направлению (КАК В close_to_the_limit) =====
                    var groups = selectedTracks
                        .GroupBy(s => s.DirectionId > 0
                            ? $"ID:{s.DirectionId}"
                            : $"NAME:{(s.DirectionName ?? "").Trim()}")
                        .ToList();

                    // В нашем XSL структура: trip -> directions -> tracks
                    // Поэтому для каждой группы делаем <directions ...>, а внутри пути -> <tracks ...>
                    foreach (var dirGroup in groups)
                    {
                        var first = dirGroup.First();

                        // Заголовок направления
                        string directionTitle = (first.DirectionName ?? "").Trim();
                        if (string.IsNullOrWhiteSpace(directionTitle))
                            directionTitle = tripProcess.DirectionName ?? "";

                        // direction code: пытаемся достать из имени "(13807)" иначе берём DirectionId, иначе tripProcess.DirectionCode
                        string directionCode = ExtractDirectionCode(directionTitle);
                        if (string.IsNullOrWhiteSpace(directionCode) && first.DirectionId > 0)
                            directionCode = first.DirectionId.ToString();
                        if (string.IsNullOrWhiteSpace(directionCode))
                            directionCode = tripProcess.DirectionCode ?? "";

                        var xeDirections = new XElement("directions",
                            new XAttribute("direction", directionTitle),
                            new XAttribute("directioncode", directionCode)
                        );

                        // ===== 5) Каждый выбранный путь внутри направления =====
                        // На всякий случай убираем дубли путей внутри одного направления
                        var tracksDistinct = dirGroup
                            .GroupBy(t => new { t.TrackId, Name = (t.TrackName ?? "").Trim() })
                            .Select(g => g.First())
                            .ToList();

                        foreach (var sel in tracksDistinct)
                        {
                            long trackId = sel.TrackId;

                            string trackNameForPrint = (sel.TrackName ?? "").Trim();
                            if (string.IsNullOrWhiteSpace(trackNameForPrint))
                                trackNameForPrint = AdmStructureService.GetTrackName(trackId)?.ToString() ?? trackId.ToString();

                            // Создаём <tracks> всегда (чтобы пустая форма была)
                            var xeTracks = new XElement("tracks",
                                new XAttribute("direction", directionTitle),
                                new XAttribute("directioncode", directionCode),
                                new XAttribute("track", trackNameForPrint),
                                new XAttribute("countbyput", 0),
                                new XAttribute("nodata", 1) // по умолчанию нет данных
                            );

                            // Получаем S3 (как в исходном DeviationDegree3) и фильтруем по track_id
                            var listS3 = (RdStructureService.GetS3(tripProcess.Trip_id, 3, distance.Name) as List<S3>) ?? new List<S3>();
                            listS3 = listS3.Where(o => o.track_id == trackId).ToList();

                            if (listS3.Count == 0)
                            {
                                // нет данных — оставляем nodata=1
                                xeDirections.Add(xeTracks);
                                continue;
                            }

                            // есть данные — снимаем nodata
                            xeTracks.SetAttributeValue("nodata", null);

                            // Коррекция участков
                            if (dist_section != null)
                            {
                                foreach (var item in listS3)
                                {
                                    var sect = dist_section.FirstOrDefault(o =>
                                        item.Km * 1000 + item.Meter >= o.Start_Km * 1000 + o.Start_M &&
                                        item.Km * 1000 + item.Meter <= o.Final_Km * 1000 + o.Final_M);

                                    if (sect != null)
                                    {
                                        item.Pchu = sect.Pchu;
                                        item.Pd = sect.Pd;
                                        item.Pdb = sect.Pdb;
                                    }
                                }
                            }

                            int countByPut = 0;

                            foreach (var s3 in listS3)
                            {
                                // как у тебя было
                                if (s3.Ots == "Рнр" || s3.Ots == "Рнрст")
                                    continue;

                                // Пересчёт Kol и итоги по ПЧ
                                switch (s3.Ots)
                                {
                                    case "Пр.п":
                                    case "Пр.л":
                                        drawdownCount += s3.Kol;
                                        break;

                                    case "Суж":
                                        s3.Kol = s3.Len / 4 + ((s3.Len % 4 + 1 > 1) ? 1 : 0);
                                        constrictionCount += s3.Kol;
                                        break;

                                    case "Уш":
                                        s3.Kol = s3.Len / 4 + ((s3.Len % 4 + 1 > 1) ? 1 : 0);
                                        broadeningCount += s3.Kol;
                                        break;

                                    case "У":
                                        s3.Kol = s3.Len / 10 + ((s3.Len % 10 + 1 > 1) ? 1 : 0);
                                        levelCount += s3.Kol;
                                        break;

                                    case "П":
                                        skewnessCount += s3.Kol;
                                        break;

                                    case "Р":
                                    case "Рст":
                                        straighteningCount += s3.Kol;
                                        break;
                                }

                                countByPut += s3.Kol;

                                // note
                                var xeNote = new XElement("note",
                                    new XAttribute("pchu", s3.Pchu),
                                    new XAttribute("pd", s3.Pd),
                                    new XAttribute("pdb", s3.Pdb),
                                    new XAttribute("km", s3.Km),
                                    new XAttribute("m", s3.Meter),
                                    new XAttribute("deviation", s3.Ots ?? ""),
                                    new XAttribute("digression", s3.Otkl),
                                    new XAttribute("len", s3.Len),
                                    new XAttribute("count", s3.Kol),
                                    new XAttribute("found_date", s3.TripDateTime.ToString("dd.MM.yyyy")),
                                    new XAttribute("primech",
                                        (s3.Primech ?? "").Contains("м;") ? "мост" :
                                        (s3.Primech ?? "").Contains("Стр") ? "Стр" :
                                        (s3.Primech ?? "").Contains("гр-/60") ? "гр" :
                                        (s3.Primech ?? "").Replace(";", ""))
                                );

                                xeTracks.Add(xeNote);
                            }

                            xeTracks.SetAttributeValue("countbyput", countByPut);

                            xeDirections.Add(xeTracks);
                        }

                        tripElem.Add(xeDirections);
                    }

                    // ===== 6) Итоги по ПЧ =====
                    if (drawdownCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Пр - " + drawdownCount)));
                    if (constrictionCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Суж - " + constrictionCount)));
                    if (broadeningCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Уш - " + broadeningCount)));
                    if (levelCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "У - " + levelCount)));
                    if (skewnessCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "П - " + skewnessCount)));
                    if (straighteningCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Р - " + straighteningCount)));
                    if (RSTCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Рст - " + RSTCount)));

                    tripElem.SetAttributeValue("countDistance",
                        drawdownCount + constrictionCount + broadeningCount + levelCount + skewnessCount + straighteningCount + RSTCount);

                    // ===== 7) Добавляем trip всегда (даже если всё пусто) =====
                    xdReport.Root.Add(tripElem);
                }

                // ===== 8) XSL -> HTML =====
                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }

            // ===== 9) Сохранить и открыть =====
            try
            {
                var outTemp = Path.Combine(Path.GetTempPath(), "report.html");
                htReport.Save(outTemp);
                htReport.Save(@"G:\form\6.Выходные формы Основные параметры\Отступления 3-ой степени (Ф.О3).html");
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
        /// Вытаскивает код направления из строки вида "Иркутск-Чита (13807)" -> "13807"
        /// Если не нашёл — вернёт пустую строку.
        /// </summary>
        private static string ExtractDirectionCode(string directionName)
        {
            if (string.IsNullOrWhiteSpace(directionName))
                return "";

            int open = directionName.LastIndexOf('(');
            int close = directionName.LastIndexOf(')');

            if (open >= 0 && close > open)
            {
                var inside = directionName.Substring(open + 1, close - open - 1).Trim();
                // оставим только цифры
                var digits = new string(inside.Where(char.IsDigit).ToArray());
                return digits;
            }

            return "";
        }
    }
}
