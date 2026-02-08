using ALARm.Core;
using ALARm.Core.AdditionalParameteres;
using ALARm.Core.Report;
using ALARm.Services;
using ALARm_Report.controls;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using System.Windows.Forms;

namespace ALARm_Report.Forms
{
    public class speed_limits : ALARm.Core.Report.GraphicDiagrams
    {
        public override void Process(long parentId, ReportTemplate template, ReportPeriod period, MetroProgressBar progressBar)
        {
            this.MainTrackStructureRepository = MainTrackStructureService.GetRepository();

            // ===== 1) Выбор путей (admTracksId) =====
            List<long> admTracksId = new List<long>();
            using (var choiceForm = new ChoiseForm(0))
            {
                choiceForm.SetTripsDataSource(parentId, period);
                choiceForm.ShowDialog();
                if (choiceForm.dialogResult == DialogResult.Cancel)
                    return;

                admTracksId = choiceForm.admTracksIDs;
            }

            // ===== 2) Подготовка справочников/данных =====
            var distance = AdmStructureService.GetUnit(AdmStructureConst.AdmDistance, parentId) as AdmUnit;
            var road = AdmStructureService.GetRoadName(distance.Id, AdmStructureConst.AdmDistance, true);
            distance.Name = distance.Name.Replace("ПЧ-", "");

            // процессы проездов по периоду/ПЧ
            var tripProcesses = RdStructureService.GetMainParametersProcess(period, distance.Name);
            if (tripProcesses == null || tripProcesses.Count == 0)
            {
                MessageBox.Show(Properties.Resources.paramDataMissing);
                return;
            }

            // кривые (как было)
            List<Curve> curves = (MainTrackStructureService.GetCurves(parentId, MainTrackStructureConst.MtoCurve) as List<Curve>)
                ?.Where(c => c.Radius <= 1200)
                .OrderBy(c => c.Start_Km * 1000 + c.Start_M)
                .ToList() ?? new List<Curve>();

            // ===== 3) Собираем XML отчета =====
            XDocument xdReport = new XDocument(new XElement("report"));

            foreach (var tripProcess in tripProcesses)
            {
                // ВАЖНО: tripProcess.Trip_id — это ID проезда (то что ты хочешь вывести)
                var tripId = tripProcess.Trip_id;

                // trip ID -> Trip (дата проезда)
                var trip = RdStructureService.GetTrip(tripProcess.Id);
                if (trip == null)
                    continue;

                // track из кривых (как было, но безопасно)
                string trackNameFromCurve = "";
                try
                {
                    if (curves.Count > 0)
                    {
                        var curvesAdmUnits = AdmStructureService.GetCurvesAdmUnits(curves[0].Id) as List<CurvesAdmUnits>;
                        var curvesAdmUnit = (curvesAdmUnits != null && curvesAdmUnits.Any()) ? curvesAdmUnits[0] : null;
                        trackNameFromCurve = curvesAdmUnit?.Track ?? "";
                    }
                }
                catch { /* ignore */ }

                // элемент trip
                XElement tripElem = new XElement("trip",
                    new XAttribute("version", $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} v{Assembly.GetExecutingAssembly().GetName().Version}"),
                    new XAttribute("road", road ?? ""),
                    new XAttribute("distance", distance.Code ?? ""),
                    new XAttribute("periodDate", period.Period ?? ""),
                    new XAttribute("check", tripProcess.GetProcessTypeName ?? ""),
                    new XAttribute("chief", tripProcess.Chief ?? ""),
                    new XAttribute("ps", tripProcess.Car ?? ""),
                    // ✅ добавили
                    new XAttribute("tripId", tripId),
                    new XAttribute("tripDate", trip.Trip_date.ToString("dd.MM.yyyy HH:mm:ss")),
                    // если нужно старое поле track на уровне trip — оставим как было
                    new XAttribute("track", trackNameFromCurve)
                );

                // итоги по trip (ПЧ)
                int ItogMainDistance = 0;
                int ItogAddDistance = 0;

                // для блока "В том числе"
                int constrictionCount = 0;  // Суж
                int broadeningCount = 0;    // Уш
                int levelCount = 0;         // У
                int skewnessCount = 0;      // П
                int drawdownCount = 0;      // Пр
                int straighteningCount = 0; // Р
                int straighteningRNRCount = 0; // Рнр
                int slopeCount = 0;         // Укл
                int PMCount = 0;            // П м
                int IBLCount = 0;           // Иб.л
                int outstandingaccelerationcount = 0; // Анп
                int outstandingaccelerationcountq = 0; // ?Анп
                int gapCount = 0;           // З

                // ✅ directions контейнер (1 на trip)
                XElement xeDirections = new XElement("directions");
                tripElem.Add(xeDirections);

                // ===== 4) По каждому выбранному пути =====
                foreach (var track_id in admTracksId)
                {
                    var trackName = AdmStructureService.GetTrackName(track_id)?.ToString() ?? track_id.ToString();

                    // километры по проезду/пути
                    var kilometersAll = RdStructureService.GetKilometersByTrip(trip);
                    if (kilometersAll == null || !kilometersAll.Any())
                    {
                        // ✅ даже если нет километров — печатаем пустой tracks
                        xeDirections.Add(BuildEmptyTracks(
                            trackName: trackName,
                            tripProcess: tripProcess,
                            distance: distance,
                            tripId: tripId,
                            tripDate: trip.Trip_date
                        ));
                        continue;
                    }

                    var kilometers = kilometersAll.Where(o => o.Track_id == track_id).ToList();
                    if (kilometers.Count == 0)
                    {
                        // ✅ даже если по этому пути нет км — печатаем пустой tracks
                        xeDirections.Add(BuildEmptyTracks(
                            trackName: trackName,
                            tripProcess: tripProcess,
                            distance: distance,
                            tripId: tripId,
                            tripDate: trip.Trip_date
                        ));
                        continue;
                    }

                    // выбор диапазона км (как было)
                    var lkm = kilometers.Select(o => o.Number).Distinct().ToList();
                    if (lkm.Count == 0)
                    {
                        xeDirections.Add(BuildEmptyTracks(
                            trackName: trackName,
                            tripProcess: tripProcess,
                            distance: distance,
                            tripId: tripId,
                            tripDate: trip.Trip_date
                        ));
                        continue;
                    }

                    var filterForm = new FilterForm();
                    var filters = new List<Filter>();
                    filters.Add(new FloatFilter() { Name = "Начало (км)", Value = lkm.Min() });
                    filters.Add(new FloatFilter() { Name = "Конец (км)", Value = lkm.Max() });

                    filterForm.SetDataSource(filters);
                    if (filterForm.ShowDialog() == DialogResult.Cancel)
                        return;

                    float kmStart = (float)filters[0].Value;
                    float kmEnd = (float)filters[1].Value;

                    kilometers = kilometers.Where(km => kmStart <= km.Number && km.Number <= kmEnd).ToList();
                    if (kilometers.Count == 0)
                    {
                        xeDirections.Add(BuildEmptyTracks(
                            trackName: trackName,
                            tripProcess: tripProcess,
                            distance: distance,
                            tripId: tripId,
                            tripDate: trip.Trip_date
                        ));
                        continue;
                    }

                    // ===== 5) Данные доп.параметров/ПРУ/стыков =====
                    List<Gap> check_gap_state = AdditionalParametersService.Check_gap_state(kilometers.First().Trip.Id, template.ID) ?? new List<Gap>();
                    var ListS3 = RdStructureService.GetS3(kilometers.First().Trip.Id) as List<S3> ?? new List<S3>();

                    var AddParam = AdditionalParametersService.GetAddParam(kilometers.First().Trip.Id) as List<S3>;
                    if (AddParam == null)
                    {
                        MessageBox.Show("Не удалось сформировать отчет, так как возникла ошибка во время загрузки данных по доп параметрам");
                        return;
                    }

                    // ✅ tracks элемент (всегда создаём)
                    XElement xeTracks = new XElement("tracks",
                        new XAttribute("track", trackName),
                        // ✅ теперь хотим "Иркутск-Чита (13807)"
                        new XAttribute("directionName", tripProcess.DirectionName ?? ""),
                        new XAttribute("directionCode", tripProcess.DirectionCode ?? ""),
                        // чтобы не ломать старое — можно оставить и старый direction, если где-то используется
                        new XAttribute("direction", tripProcess.DirectionCode ?? ""),
                        new XAttribute("distance", distance.Code ?? ""),
                        // ✅ продублируем для строки "Проезд/Дата" внутри tracks (по желанию)
                        new XAttribute("tripId", tripId),
                        new XAttribute("tripDate", trip.Trip_date.ToString("dd.MM.yyyy HH:mm:ss")),
                        // заполнится ниже
                        new XAttribute("countbyput", 0)
                    );

                    xeDirections.Add(xeTracks);

                    int itogMainByTrack = 0;
                    int itogAddByTrack = 0;

                    // ===== 6) Проход по километрам =====
                    foreach (var km in kilometers)
                    {
                        // основные отметки
                        km.Digressions = RdStructureService.GetDigressionMarks(km.Trip.Id, km.Track_id, km.Number) ?? new List<DigressionMark>();

                        // боковой износ (как было)
                        var DBcrossRailProfile = AdditionalParametersService.GetCrossRailProfileFromDBbyKm(km.Number, km.Trip.Id);
                        if (DBcrossRailProfile == null) continue;

                        var sortedData = DBcrossRailProfile.OrderByDescending(d => d.Meter).ToList();
                        var crossRailProfile = AdditionalParametersService.GetCrossRailProfileFromDBParse(sortedData);
                        List<Digression> addDigressions = crossRailProfile.GetDigressions();

                        var dignatur = new List<DigressionMark>();
                        foreach (var dig in addDigressions)
                        {
                            int count = dig.Length / 4;
                            count += dig.Length % 4 > 0 ? 1 : 0;

                            if (dig.DigName == DigressionName.SideWearLeft || dig.DigName == DigressionName.SideWearRight)
                            {
                                dignatur.Add(new DigressionMark()
                                {
                                    Digression = dig.DigName,
                                    NotMoveAlert = false,
                                    Meter = dig.Meter,
                                    finish_meter = dig.Kmetr,
                                    Degree = (int)dig.Typ,
                                    Length = dig.Length,
                                    Value = dig.Value,
                                    Count = count,
                                    DigName = dig.GetName(),
                                    PassengerSpeedLimit = -1,
                                    FreightSpeedLimit = -1,
                                    Comment = "",
                                    Diagram_type = "Iznos_relsov",
                                    Digtype = DigressionType.Additional
                                });
                            }
                        }

                        // скорости по бок. износу (как было)
                        int pas = 999, gruz = 999;
                        foreach (DigressionMark item in dignatur)
                        {
                            if (item.Digression == DigressionName.SideWearLeft || item.Digression == DigressionName.SideWearRight)
                            {
                                if (km.Curves == null) continue;
                                var c = km.Curves.Where(o => o.RealStartCoordinate <= km.Number + item.Meter / 10000.0 &&
                                                             o.RealFinalCoordinate >= km.Number + item.Meter / 10000.0).ToList();
                                if (c.Any())
                                {
                                    if (km.Speeds == null) continue;
                                    item.GetAllowSpeedAddParam(km.Speeds.First(), c.First().Straightenings[0].Radius, item.Value);

                                    if (item.PassengerSpeedLimit != -1 && item.PassengerSpeedLimit < pas) pas = item.PassengerSpeedLimit;
                                    if (item.FreightSpeedLimit != -1 && item.FreightSpeedLimit < gruz) gruz = item.FreightSpeedLimit;
                                }
                            }
                            else if (item.Digression == DigressionName.Gap)
                            {
                                if (item.PassengerSpeedLimit != -1 && item.PassengerSpeedLimit < pas) pas = item.PassengerSpeedLimit;
                                if (item.FreightSpeedLimit != -1 && item.FreightSpeedLimit < gruz) gruz = item.FreightSpeedLimit;
                            }
                        }

                        // доп. отметки из AddParam по километру (как было)
                        var gap_dig = AddParam.Where(o => o.Km == km.Number).ToList();
                        foreach (var item in gap_dig)
                        {
                            km.Digressions.Add(new DigressionMark
                            {
                                Km = item.Km,
                                Meter = item.Meter,
                                Length = item.Len,
                                DigName = item.Ots,
                                Count = item.Kol,
                                Value = item.Otkl,
                                PassengerSpeedLimit = item.Ogp,
                                FreightSpeedLimit = item.Ogp,
                                Digtype = DigressionType.Additional
                            });
                        }

                        // PRU по основным (как было)
                        var PRUbyKmMAIN = ListS3.Where(o =>
                                ((o.Ovp != -1 && o.Ogp != -1) || (o.Primech == "гр")) &&
                                o.Km == km.Number).ToList();
                        PRUbyKmMAIN = PRUbyKmMAIN.Where(o =>
                                (o.Ovp != 0 && o.Ogp != 0) || (o.Ovp == 0 && o.Ogp == 0 && o.Typ == 4))
                            .ToList();

                        // PRU по доп (стыки "З") (как было)
                        var PRUbyKmADD = check_gap_state.Where(o => o.Otst == "З" && o.Km == km.Number).ToList();

                        km.LoadTrackPasport(MainTrackStructureRepository, trip.Trip_date);

                        // ===== MAIN -> <main> =====
                        foreach (var s3 in PRUbyKmMAIN)
                        {
                            if (s3.Put == null) continue;
                            if (!string.IsNullOrEmpty(s3.Primech) && s3.Primech.Contains("Натурная кривая")) continue;

                            switch (s3.Ots)
                            {
                                case "Пр.п":
                                case "Пр.л":
                                    drawdownCount += 1; break;
                                case "Анп":
                                    outstandingaccelerationcount += 1; break;
                                case "?Анп":
                                    outstandingaccelerationcountq += 1; break;
                                case "Суж":
                                    constrictionCount += 1; break;
                                case "Уш":
                                    broadeningCount += 1; break;
                                case "У":
                                    levelCount += 1; break;
                                case "П":
                                    skewnessCount += 1; break;
                                case "Р":
                                    straighteningCount += 1; break;
                                case "Укл":
                                    slopeCount += 1; break;
                                case "П м":
                                    PMCount += 1; break;
                                case "Иб.л":
                                    IBLCount += 1; break;
                                case "Рнр":
                                    straighteningRNRCount += 1; break;
                            }

                            string otklStr;
                            if (s3.Ots == "Аг" || s3.Ots == "Анп" || s3.Ots == "Пси" || s3.Ots == "ОШК" || s3.Ots == "ОШП" || s3.Ots == "Укл")
                                otklStr = s3.Otkl.ToString("0.00");
                            else
                                otklStr = s3.Otkl.ToString();

                            string stepStr = (s3.Typ.ToString() == "5") ? "-" : s3.Typ.ToString();

                            // примечания (как было)
                            string primech = "";
                            if (s3.Primech == "м;") primech = "мост";
                            else if (s3.Primech == "гр") primech = "гр";

                            XElement xeMain = new XElement("main",
                                new XAttribute("track", s3.Put),
                                new XAttribute("km", km.Number),
                                new XAttribute("m", s3.Meter),
                                new XAttribute("Data", s3.TripDateTime.ToString("dd.MM.yyyy")),
                                new XAttribute("Ots", s3.Ots),
                                new XAttribute("Otkl", otklStr),
                                new XAttribute("len", s3.Len),
                                new XAttribute("Stepen", stepStr),
                                new XAttribute("vpz", (s3.Uv.ToString() == "-1" ? "-" : s3.Uv.ToString()) + "/" + (s3.Uvg.ToString() == "-1" ? "-" : s3.Uvg.ToString())),
                                new XAttribute("vogr", (s3.Ovp.ToString() == "-1" ? "-" : s3.Ovp.ToString()) + "/" + (s3.Ogp.ToString() == "-1" ? "-" : s3.Ogp.ToString())),
                                new XAttribute("Primech", primech)
                            );

                            xeTracks.Add(xeMain);
                        }

                        itogMainByTrack += PRUbyKmMAIN.Count;
                        ItogMainDistance += PRUbyKmMAIN.Count;

                        // ===== ADD -> <add> =====
                        foreach (var s3 in PRUbyKmADD)
                        {
                            switch (s3.Otst)
                            {
                                case "З":
                                    gapCount += 1; break;
                                case "Анп":
                                    outstandingaccelerationcount += 1; break;
                                case "?Анп":
                                    outstandingaccelerationcountq += 1; break;
                            }

                            XElement xeAdd = new XElement("add",
                                new XAttribute("track", s3.track_id),
                                new XAttribute("km", s3.Km),
                                new XAttribute("m", s3.Meter),
                                new XAttribute("Data", trip.Trip_date.ToString("dd.MM.yyyy")),
                                new XAttribute("Ots", s3.Otst),
                                new XAttribute("Otkl", Math.Max(s3.Zazor, s3.R_zazor)),
                                new XAttribute("len", "-"),
                                new XAttribute("vpz", s3.Vpz),
                                new XAttribute("vogr", s3.Vdop),
                                new XAttribute("Primech", "")
                            );

                            xeTracks.Add(xeAdd);
                        }

                        itogAddByTrack += PRUbyKmADD.Count;
                        ItogAddDistance += PRUbyKmADD.Count;
                    }

                    // ✅ Итог по пути
                    xeTracks.SetAttributeValue("countbyput", itogMainByTrack + itogAddByTrack);
                }

                // ✅ Итог по ПЧ
                tripElem.SetAttributeValue("countDistance", ItogMainDistance + ItogAddDistance);

                // ✅ блок "В том числе" — добавляем как было (только если >0)
                if (drawdownCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Пр - " + drawdownCount)));
                if (outstandingaccelerationcount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Анп - " + outstandingaccelerationcount)));
                if (outstandingaccelerationcountq > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "?Анп - " + outstandingaccelerationcountq)));
                if (constrictionCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Суж - " + constrictionCount)));
                if (broadeningCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Уш - " + broadeningCount)));
                if (levelCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "У - " + levelCount)));
                if (skewnessCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "П - " + skewnessCount)));
                if (straighteningCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Р - " + straighteningCount)));
                if (slopeCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Укл - " + slopeCount)));
                if (PMCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "П м - " + PMCount)));
                if (IBLCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Иб.л - " + IBLCount)));
                if (gapCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "З - " + gapCount)));
                if (straighteningRNRCount > 0) tripElem.Add(new XElement("total", new XAttribute("totalinfo", "Рнр - " + straighteningRNRCount)));

                // ✅ добавляем trip всегда (даже если пусто), чтобы печатались пустые таблицы по путям
                xdReport.Root.Add(tripElem);
            }

            // ===== 7) Transform XSL -> HTML =====
            XDocument htReport = new XDocument();
            using (XmlWriter writer = htReport.CreateWriter())
            {
                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }

            // ===== 8) Save/Open =====
            try
            {
                var tempHtml = Path.Combine(Path.GetTempPath(), "report.html");
                htReport.Save(tempHtml);

                // твой путь сохранения (как было)
                htReport.Save($@"G:\form\1.Основные и дополнительные параметры геометрии рельсовой колеи (ГРК)\6.Ведомость неисправностей, требующих ограничения скорости  по основным и дополнительным параметрам.html");
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

        /// <summary>
        /// ✅ Пустой блок tracks, чтобы даже без данных печатались таблицы по путям.
        /// </summary>
        private static XElement BuildEmptyTracks(string trackName, dynamic tripProcess, AdmUnit distance, long tripId, DateTime tripDate)
        {
            return new XElement("tracks",
                new XAttribute("track", trackName),
                new XAttribute("directionName", tripProcess.DirectionName ?? ""),
                new XAttribute("directionCode", tripProcess.DirectionCode ?? ""),
                new XAttribute("direction", tripProcess.DirectionCode ?? ""),
                new XAttribute("distance", distance.Code ?? ""),
                new XAttribute("tripId", tripId),
                new XAttribute("tripDate", tripDate.ToString("dd.MM.yyyy HH:mm:ss")),
                new XAttribute("countbyput", 0)
            );
        }
    }
}
