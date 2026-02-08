using Svg;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;

namespace ALARm.Core.Report
{
    public class MainParameters : GraphicDiagrams
    {
        private readonly float LevelPosition = 32.5f;
        private readonly float LevelStep = 7.5f;
        private readonly float LevelKoef = 0.25f;

        private readonly float StraighRighttPosition = 62f;
        private readonly float StrightLeftPosition = 71f;
        private readonly float StrightStep = 15f;
        private readonly float StrightKoef = 0.5f;

        private readonly float GaugePosition = 100.5f;
        private readonly float GaugeKoef = 0.5f;

        private readonly float ProsRightPosition = 138.5f;
        private readonly float ProsLeftPosition = 124.5f;
        private readonly float ProsKoef = 0.5f;

        public MainParameters(IRdStructureRepository rdStructureRepository, IMainTrackStructureRepository mainTrackStructureRepository, IAdmStructureRepository admStructureRepository)
        {
            this.RdStructureRepository = rdStructureRepository;
            this.MainTrackStructureRepository = mainTrackStructureRepository;
            this.AdmStructureRepository = admStructureRepository;
        }
        public void Process(ReportTemplate template, Kilometer kilometer, Trips trip, bool autoprint, Kilometer previous, Kilometer next)
        {

            XDocument htReport = new XDocument();
            var svgLength = 0;
            using (XmlWriter writer = htReport.CreateWriter())
            {
                XDocument xdReport = new XDocument();
                XElement report = new XElement("report");

                int svgIndex = template.Xsl.IndexOf("</svg>");
                template.Xsl = template.Xsl.Insert(svgIndex, RighstSideXslt());
                if (kilometer.Number == 6109)
                {

                }
                var direction = AdmStructureRepository.GetDirectionByTrack(kilometer.Track_id);


                //ПРУ
                var PasSpeed = kilometer.Speeds.Any() ? kilometer.Speeds.First().Passenger : -1;
                var pru_dig_list = new List<DigressionMark> { };

                var curve_bpd_list = new List<DigressionMark> { };

                var Curve_nature_value = new List<DigressionMark>();
                int prevIndex = kilometer.Number + 1;
                //var rezulat = new List<> ;
                //var rezulat_next = new List<> ;
                //ezulat_next=
                var result = new List<Curve>();

                //if (previous != null)
                // result.AddRange(previous.Curve.s);

                if (kilometer.Number == 6609)
                {

                }

                result.AddRange(kilometer.Curves.GroupBy(p => p.Id).Select(g => g.First()).ToList());
                if (next != null)
                {
                    // result.AddRange(next.Curves); nb     
                    // var intersect = kilometer.Curves.Intersect(next.Curves);
                    //result.AddRange(intersect);
                }

                foreach (var bpd_curve in result)
                {
                    if (kilometer.Number == 6609)
                    {

                    }
                    if (kilometer.Number == 6610)
                    {

                    }
                    List<RDCurve> rdcs = RdStructureRepository.GetRDCurves(bpd_curve.Id, trip.Id);
                    //var LevelPoins = rdcs.Where(o => o.Point_level > 0).ToList();
                    //var StrPoins = rdcs.Where(o => o.Point_str > 0).ToList();
                    var nn = kilometer.Number;
                    var curve_center_ind = rdcs.Count / 2;
                    var rightCurve = new List<RDCurve>();
                    var leftCurve = new List<RDCurve>();

                    //басын аяғын тауып алу Рихтовка
                    //for (int cInd = curve_center_ind; cInd < rdcs.Count; cInd++)
                    //{
                    //    rightCurve.Add(rdcs[cInd]);
                    //    if (Math.Abs(rdcs[cInd].Trapez_str) < 0.1)
                    //        break;
                    //}
                    //for (int cInd = curve_center_ind; cInd > 0; cInd--)
                    //{
                    //    leftCurve.Add(rdcs[cInd]);
                    //    if (Math.Abs(rdcs[cInd].Trapez_str) < 0.1)
                    //        break;
                    //}

                    //var strData = rdcs.Where(o => leftCurve.Last().X <= o.X && o.X <= rightCurve.Last().X).ToList();

                    //var strData = rdcs.Where(o => (o.Km * 10000 + o.M) >= (bpd_curve.Start_Km * 10000 + bpd_curve.Start_M) && (o.Km * 10000 + o.M) <= (bpd_curve.Final_Km * 10000 + bpd_curve.Final_M)).ToList();
                    var curveStrStartCoord = bpd_curve.Straightenings.First().Start_Km * 10000 + bpd_curve.Straightenings.First().Start_M;
                    var curveStrFinalCoord = bpd_curve.Straightenings.Last().Final_Km * 10000 + bpd_curve.Straightenings.Last().Final_M;
                    var strData = rdcs.Where(o => (o.Km * 10000 + o.M) >= (curveStrStartCoord) && (o.Km * 10000 + o.M) <= (curveStrFinalCoord)).ToList();
                    // трапециядан туынды алу
                    //for (int fi = 0; fi < strData.Count - 4; fi++)
                    //{
                    //    var temp = Math.Abs(strData[fi + 4].Trapez_str - strData[fi].Trapez_str);
                    //    strData[fi].FiList = temp;
                    //}
                    if (strData.Count() < 10)
                    {
                        continue;
                    }

                    for (int fi = 4; fi < strData.Count; fi++)

                    {

                        var temp = (fi < strData.Count - 4) ?
                            Math.Abs(strData[fi + 4].Trapez_str - strData[fi].Trapez_str) : Math.Abs(strData[fi - 4].Trapez_str - strData[fi].Trapez_str);
                        strData[fi].FiList = temp;

                    }
                    //накты вершиналарды табу
                    var vershList = new List<List<RDCurve>>();
                    var perehod = new List<RDCurve>();
                    var krug = new List<RDCurve>();

                    var flagPerehod = true;
                    var flagKrug = false;


                    //for (int versh = 3; versh < strData.Count - 4; versh++)
                    for (int versh = 0; versh < strData.Count; versh++)
                    {
                        if (strData[versh].FiList >= 0.1 && flagPerehod)
                        {
                            perehod.Add(strData[versh]);
                        }
                        else if (strData[versh].FiList < 0.1)
                        {
                            if (perehod.Any() && perehod.Count() >= 10)
                            {
                                vershList.Add(perehod);
                                perehod = new List<RDCurve>();
                                krug = new List<RDCurve>();

                                flagPerehod = false;
                                flagKrug = true;
                            }
                        }

                        if (strData[versh].FiList < 0.1 && flagKrug)
                        {
                            krug.Add(strData[versh]);
                        }
                        else if (strData[versh].FiList >= 0.1)
                        {
                            if (krug.Any() && krug.Count() >= 10)
                            {
                                vershList.Add(krug);
                                perehod = new List<RDCurve>();
                                krug = new List<RDCurve>();

                                flagPerehod = true;
                                flagKrug = false;
                            }
                        }
                    }
                    if (perehod.Any() && perehod.Count() >= 10)
                    {
                        vershList.Add(perehod);
                    }
                    //if (bpd_curve.Start_Km != bpd_curve.Final_Km && krug.Any())
                    if (krug.Any() && krug.Count() >= 10)
                    {
                        vershList.Add(krug);
                    }

                    var StrPoins = new List<RDCurve>();

                    foreach (var item in vershList)
                    {
                        StrPoins.Add(item.First());
                    }
                    if (strData.Count == 0) continue;
                    StrPoins.Add(strData.Last());
                    //if (strData.Count() == 0) continue;
                    //if (vershList.Count() > 0)
                    //{
                    //    if (StrPoins.Last().FiList != 0)
                    //    {
                    //        StrPoins.Add(vershList.Last().Last());
                    //    }
                    //}


                    curve_center_ind = rdcs.Count / 2;
                    var rightCurveLvl = new List<RDCurve>();
                    var leftCurveLvl = new List<RDCurve>();
                    //var LvlData = rdcs.Where(o => (o.Km * 10000 + o.M) >= (bpd_curve.Start_Km * 10000 + bpd_curve.Start_M) && (o.Km * 10000 + o.M) <= (bpd_curve.Final_Km * 10000 + bpd_curve.Final_M)).ToList();
                    var curveElevStartCoord = 0;
                    var curveElevFinalCoord = 0;
                    if (bpd_curve.Elevations.Count == 0 && bpd_curve.Straightenings.Count == 0) continue;
                    if (bpd_curve.Elevations.Count == 0)
                    {
                        curveElevStartCoord = bpd_curve.Straightenings.First().Start_Km * 10000 + bpd_curve.Straightenings.First().Start_M;
                        curveElevFinalCoord = bpd_curve.Straightenings.Last().Final_Km * 10000 + bpd_curve.Straightenings.Last().Final_M;
                    }
                    if (bpd_curve.Elevations.Count > 0)
                    {
                        curveElevStartCoord = bpd_curve.Elevations.First().Start_Km * 10000 + bpd_curve.Elevations.First().Start_M;
                        curveElevFinalCoord = bpd_curve.Elevations.Last().Final_Km * 10000 + bpd_curve.Elevations.Last().Final_M;
                    }

                    var LvlData = rdcs.Where(o => (o.Km * 10000 + o.M) >= (curveElevStartCoord) && (o.Km * 10000 + o.M) <= (curveElevFinalCoord)).ToList();

                    // трапециядан туынды алу
                    //for (int fi = 0; fi < LvlData.Count - 4; fi++)
                    //{
                    //    var temp = Math.Abs(LvlData[fi + 4].Trapez_level - LvlData[fi].Trapez_level);
                    //    LvlData[fi].FiList2 = temp;
                    //}
                    for (int fi = 4; fi < LvlData.Count; fi++)
                    {
                        var temp = (fi < LvlData.Count - 4) ? Math.Abs(LvlData[fi + 4].Trapez_level - LvlData[fi].Trapez_level) : Math.Abs(LvlData[fi - 4].Trapez_level - LvlData[fi].Trapez_level);
                        LvlData[fi].FiList2 = temp;
                    }
                    //накты вершиналарды табу
                    var vershListLVL = new List<List<RDCurve>>();
                    var perehodLVL = new List<RDCurve>();
                    var krugLVL = new List<RDCurve>();

                    var flagPerehodLVL = true;
                    var flagKrugLVL = false;
                    if (kilometer.Number == 6937)
                    {

                    }
                    for (int versh = 0; versh < LvlData.Count; versh++)
                    {
                        if (LvlData[versh].FiList2 > 0.1 && flagPerehodLVL)
                        {
                            perehodLVL.Add(LvlData[versh]);
                        }
                        else
                        {
                            if (perehodLVL.Any())
                            {
                                vershListLVL.Add(perehodLVL);
                                perehodLVL = new List<RDCurve>();
                                krugLVL = new List<RDCurve>();

                                flagPerehodLVL = false;
                                flagKrugLVL = true;
                            }
                        }

                        if (LvlData[versh].FiList2 < 0.1 && flagKrugLVL && (versh < LvlData.Count() - 1))
                        {
                            krugLVL.Add(LvlData[versh]);
                        }
                        else
                        {
                            if (krugLVL.Any())
                            {
                                vershListLVL.Add(krugLVL);
                                perehodLVL = new List<RDCurve>();
                                krugLVL = new List<RDCurve>();

                                flagPerehodLVL = true;
                                flagKrugLVL = false;
                            }
                        }

                    }
                    if (perehodLVL.Any())
                    {
                        vershListLVL.Add(perehodLVL);
                    }

                    var LevelPoins = new List<RDCurve>();

                    foreach (var item in vershListLVL)
                    {
                        LevelPoins.Add(item.First());
                    }


                    if (LvlData.Count == 0) break;
                    LevelPoins.Add(LvlData.Last());



                    bool flaglvl = false;
                    //  if (StrPoins.Count < 4 && LevelPoins.Count < 4    )                  continue;
                    if (StrPoins.Count < 3 && LevelPoins.Count < 3) continue;
                    if (StrPoins.Count < 2 && LevelPoins.Count >= 4)
                        flaglvl = true;


                    int lvl = -1, str = -1, lenPerKrivlv = -1;
                    int lenPru = -1;

                    var minH = new List<RDCurve>();
                    var minOb = 9000.0;

                    var maxAnp = new List<RDCurve>();
                    var max = -1.0;


                    if (kilometer.Number == 6913)
                    {

                    }


                    if (kilometer.Number == 6937)
                    {

                    }

                    DigressionMark curvelistitem = new DigressionMark();

                    //foreach (var straight in bpd_curve.Straightenings)
                    //{
                    //    var startm = (int)(10000 * (straight.FirstTransitionEnd % 1));


                    //    foreach (var elev in bpd_curve.Elevations)
                    //    {

                    //        // if (kilometer.Number == item.First().Km || (item.First().Km != item.Last().Km > 0))
                    //        //   if (elev.RealStartCoordinate <= straight.RealFinalCoordinate + 0.001 && elev.RealFinalCoordinate + 0.001 >= straight.RealFinalCoordinate)
                    //        if (elev.RealStartCoordinate <= straight.RealFinalCoordinate && elev.RealFinalCoordinate >= straight.RealStartCoordinate


                    //                               && (elev.Final_Km == kilometer.Number || elev.Final_Km != elev.Start_Km)
                    //                               )
                    //        {
                    //            if (elev.Lvl == 51)
                    //            {

                    //            }
                    //            if (straight.Radius == 0) continue;
                    //            curvelistitem = new DigressionMark()
                    //            {
                    //                Km = (int)Math.Floor(straight.FirstTransitionEnd),
                    //                lvl = (int)elev.Lvl,
                    //                Radius = straight.Radius,
                    //                Meter = startm,
                    //                Alert = $"{startm} R:{straight.Radius} h:{elev.Lvl} Ш:{straight.Width} И:{straight.Wear} "

                    //            };



                    //            //if ((int)Math.Floor(straight.RealStartCoordinate) <= kilometer.Number && (int)Math.Floor(straight.RealFinalCoordinate) >= kilometer.Number
                    //            //    && curve_bpd_list.Where(o => o.Meter == curvelistitem.Meter).Count() == 0)
                    //            //curve_bpd_list.Where(o => o.Meter == curvelistitem.Meter).Count() == 0 && 
                    //            if (curvelistitem.Km == kilometer.Number && curve_bpd_list.Where(o => o.Alert == curvelistitem.Alert).Count() == 0)
                    //            {
                    //                curve_bpd_list.Add(curvelistitem);
                    //            }

                    //        }


                    //        //if (curvelistitem.Km == kilometer.Number && curve_bpd_list.Where(o => o.Alert == curvelistitem.Alert).Count() == 0)

                    //    }

                    //}
                    if (kilometer.Number == 6936)
                    {

                    }

                    if (kilometer.Number == 6653)
                    {

                    }
                    foreach (var straight in bpd_curve.Straightenings)
                    {

                        var startm = (int)(10000 * (straight.FirstTransitionEnd % 1));

                        if (bpd_curve.Elevations.Count() == 0)
                        {
                            if (straight.Radius == 0) continue;
                            curvelistitem = new DigressionMark()
                            {
                                Km = (int)Math.Floor(straight.FirstTransitionEnd),
                                lvl = 0,
                                Radius = straight.Radius,
                                Meter = startm,
                                Alert = $"{startm} R:{straight.Radius} h:{0} Ш:{straight.Width} И:{straight.Wear} "

                            };

                            if (curvelistitem.Km == kilometer.Number && curve_bpd_list.Where(o => o.Alert == curvelistitem.Alert).Count() == 0)
                            {
                                curve_bpd_list.Add(curvelistitem);
                            }
                        }

                        foreach (var elev in bpd_curve.Elevations)
                        {

                            // if (kilometer.Number == item.First().Km || (item.First().Km != item.Last().Km > 0))
                            //   if (elev.RealStartCoordinate <= straight.RealFinalCoordinate + 0.001 && elev.RealFinalCoordinate + 0.001 >= straight.RealFinalCoordinate)
                            if (elev.FirstTransitionEnd <= straight.SecondTransitionStart && elev.SecondTransitionStart >= straight.FirstTransitionEnd
                                && (elev.Final_Km == kilometer.Number || elev.Final_Km != elev.Start_Km))
                            {
                                if (straight.Radius == 0) continue;
                                curvelistitem = new DigressionMark()
                                {
                                    Km = (int)Math.Floor(straight.FirstTransitionEnd),
                                    lvl = (int)elev.Lvl,
                                    Radius = straight.Radius,
                                    Meter = startm,
                                    Alert = $"{startm} R:{straight.Radius} h:{elev.Lvl} Ш:{straight.Width} И:{straight.Wear} "

                                };

                                if (curvelistitem.Km == kilometer.Number && curve_bpd_list.Where(o => o.Alert == curvelistitem.Alert).Count() == 0)
                                {
                                    curve_bpd_list.Add(curvelistitem);
                                }
                            }
                        }

                        if (bpd_curve.Elevations.Count == 0 && bpd_curve.Straightenings.Count > 0)
                        {

                            // if (kilometer.Number == item.First().Km || (item.First().Km != item.Last().Km > 0))
                            //   if (elev.RealStartCoordinate <= straight.RealFinalCoordinate + 0.001 && elev.RealFinalCoordinate + 0.001 >= straight.RealFinalCoordinate)

                            {
                                if (straight.Radius == 0) continue;
                                curvelistitem = new DigressionMark()
                                {
                                    Km = (int)Math.Floor(straight.FirstTransitionEnd),
                                    lvl = 0,
                                    Radius = straight.Radius,
                                    Meter = startm,
                                    Alert = $"{startm} R:{straight.Radius} h:{0} Ш:{straight.Width} И:{straight.Wear} "

                                };

                                if (curvelistitem.Km == kilometer.Number && curve_bpd_list.Where(o => o.Alert == curvelistitem.Alert).Count() == 0)
                                {
                                    curve_bpd_list.Add(curvelistitem);
                                }
                            }
                        }




                    }




                    //if (bpd_curve.Elevations.Count == 0 && bpd_curve.Straightenings.Count > 0)
                    //{ vershListLVL= vershListSTR}
                    List<List<RDCurve>> vershl = flaglvl ? vershListLVL : vershList;
                    for (int i = 1; i < vershl.Count; i = i + 2)
                    {
                        var item = vershl[i];
                        var pru_number = false;
                        if (item.Count() <= 10) continue;
                        var r = item.Select(o => Math.Abs(o.Trapez_str)).Max();
                        var h = item.Select(o => Math.Abs(o.Trapez_level)).Max();

                        if (item.First().Km != item.Last().Km)
                        {
                            item = item.Where(o => o.Km == kilometer.Number).ToList();
                        }
                        if (item.Count() <= 10) continue;
                        var avgmeterbyItem = item[item.Count() / 2].M;

                        h = (int)item.Select(o => Math.Abs(o.Trapez_level)).Max();
                        lvl = (int)h;
                        // var metr= item.Select(o => o.Trapez_level).;
                        if (Math.Abs(h) < minOb)
                        {
                            minOb = Math.Abs(h);
                            minH = item;
                        }

                        //макс ыздеу анп га

                        for (int ii = 1; ii < vershl.Count; ii = ii + 2)
                        {
                            var itemL = vershl[ii];
                            var tempM = itemL.Select(o => o.PassBoost).Max();
                            if (tempM > max)
                            {
                                max = tempM;
                                maxAnp = itemL;
                            }
                        }

                        //if (Math.Abs(h) < 4)
                        //continue;

                        var passportcurves = kilometer.CurvesBPD.Where(o => o.Id == bpd_curve.Id).ToList();
                        if (passportcurves.Count() > 1 && bpd_curve.Start_Km != bpd_curve.Final_Km /*&& kilometer.Direction == Direction.Reverse*/)
                            passportcurves = passportcurves.Where(o => o.Start_Km == kilometer.Number).ToList();


                        for (int k = 0; k < passportcurves.Count(); k++)
                        {
                            passportcurves[k].Elevations = passportcurves[k].Elevations.Where(o => o.Start_Km == kilometer.Number || o.Final_Km == kilometer.Number).ToList();
                        }
                        if (passportcurves.Count() == 0) break;

                        Curve curvepass = curvepass = passportcurves.Count() > i / 2 ? passportcurves[i / 2] : passportcurves.First();



                        //if (kilometer.Number == item.First().Km)
                        //{

                        //    if (curvestrindex > curvepass.Elevations.Count() || curvepass.Elevations.Count() == 0) continue;
                        //    DigressionMark curvelistitem = new DigressionMark();
                        //    if (straightpass != new StCurve())
                        //    {
                        //        startm = (int)curvepass.Start_M + (int)straightpass.Transition_1;

                        //    }
                        //    else
                        //    {
                        //        curvelistitem = new DigressionMark()
                        //        {
                        //            Km = item.First().Km,
                        //            lvl = (int)bpd_curve.Elevations[0].Lvl,
                        //            Radius = bpd_curve.Radius,
                        //            Meter = (item.First().Km == item.Last().Km) ? (int)avgmeterbyItem - 30 : (startm + kilometer.Final_m) / 2 - 30,
                        //            Alert = $"{startm} R:{curvepass.Radius} h:{curvepass.Elevations[curvestrindex].Lvl} Ш:{curvepass.Straightenings[wearind].Width} И:{curvepass.Straightenings[wearind].Wear} "

                        //        };
                        //    }

                        if ((kilometer.Number == item.First().Km || (item.First().Km != item.Last().Km) && passportcurves.Count() > 0))
                        {
                            if (Math.Abs(r) < 4) continue;
                            var kfivfact = new DigressionMark()
                            {
                                Km = item.First().Km,
                                Meter = (item.First().Km == item.Last().Km) ? (int)avgmeterbyItem : (item.First().M + kilometer.Final_m) / 2,
                                lvl = (int)Math.Abs(h),
                                Alert = $"кривая факт. R:{ (17860 / Math.Abs(r)):0} H:{ Math.Abs(h):0}"
                            };
                            if (pru_dig_list.Where(o => o.Alert == kfivfact.Alert).Count() == 0)
                            {
                                pru_dig_list.Add(kfivfact);
                            }

                        }


                        //}
                        try
                        {
                            //Passenger
                            var PassBoostAbs = item.Select(o => Math.Abs(o.PassBoost_anp)).ToList();
                            var PassboostMax = PassBoostAbs.Max();

                            var MaxPassboostIndex = PassBoostAbs.IndexOf(PassboostMax);
                            var AnpPassMax = PassboostMax * Math.Sign(item[MaxPassboostIndex].PassBoost_anp);
                            //Freight
                            var FreightBoostAbs = item.Select(o => Math.Abs(o.FreightBoost_anp)).ToList();
                            var FreightboostMax = FreightBoostAbs.Max();
                            var MaxFreightboostIndex = FreightBoostAbs.IndexOf(FreightboostMax);
                            var AnpFreigMax = FreightboostMax * Math.Sign(item[MaxFreightboostIndex].FreightBoost_anp);
                            //



                            var itemData = new Data { };
                            //  var Vkr = RoundNumToFive(itemData.GetKRSpeedPass(item));
                            //
                            var KR = new List<int> { };


                            var index = item.Count() / 2;

                            var R = (17860.0 / (Math.Abs(item[index].Trapez_str) != 0 ? Math.Abs(item[index].Trapez_str) : 0.0001));
                            var Vkr = (int)(Math.Sqrt(13.0 * R * (0.7 + 0.0061 * Math.Abs(item[index].Trapez_level))));
                            KR.Add(RoundNumToFive(Vkr));
                            var AnpPass = 0.0;
                            var AnpPassFirst = 0.0;//'П'

                            var AnpPassLast = 0.0;


                            var Ogr = -1;

                            var item_center = (item.First().Km * 1000 + item.First().M) + ((item.Last().Km * 1000 + item.Last().M) - (item.First().Km * 1000 + item.First().M)) / 2;
                            var itempkm = item_center / 1000;
                            var item_center_m = (item.First().M + item.Last().M) / 2;

                            AnpPassFirst = kilometer.Speeds.First().Passenger * kilometer.Speeds.First().Passenger / 13.0 / R - 0.0061 * Math.Abs(item[index].Trapez_level);
                            AnpPassLast = kilometer.Speeds.Last().Passenger * kilometer.Speeds.Last().Passenger / 13.0 / R - 0.0061 * Math.Abs(item[index].Trapez_level);




                            var sp1 = kilometer.Speeds.First().Start_M;
                            var sp2 = kilometer.Speeds.First().Final_M;
                            if (item_center_m > kilometer.Speeds.First().Start_M && item_center_m < kilometer.Speeds.First().Final_M)

                            {
                                AnpPass = AnpPassFirst;
                                if (kilometer.Speeds.First().Passenger > Vkr) Ogr = Vkr;
                            }

                            if (item_center_m > kilometer.Speeds.Last().Start_M && item_center_m < kilometer.Speeds.Last().Final_M)

                            {
                                AnpPass = AnpPassLast;
                                if (kilometer.Speeds.Last().Passenger > Vkr) Ogr = Vkr;

                            }

                            var Dname = "";

                            if (AnpPass >= 0.65 && kilometer.Number == itempkm && Ogr > -1)
                            {
                                Dname = DigressionName.SpeedUp.Name;
                                Ogr = RoundNumToFive(Ogr);
                            }



                            if (AnpPass >= 0.65 && kilometer.Number == itempkm && Ogr > -1)
                            {
                                Dname = DigressionName.SpeedUp.Name;
                                Ogr = RoundNumToFive(Ogr);
                            }
                            else if (0.65 <= AnpPass && kilometer.Number == itempkm && Ogr == -1)
                            {
                                Dname = DigressionName.SpeedUpNear.Name;
                                Ogr = -1;
                            }

                            if (Dname != "")
                            {
                                pru_dig_list.Add(new DigressionMark
                                {
                                    Km = kilometer.Number,
                                    //Meter = item[item.Count / 2].M,
                                    Meter = maxAnp[maxAnp.Count / 2].M,// new  07.06/2024
                                                                       //Meter = maxAnp[maxAnp.Count / 2].M - 20,  07.06/2024
                                                                       // Length = item.Count, // длина круговой
                                    Length = maxAnp.Count(),
                                    DigName = Dname,
                                    //Comment = $"П:{AnpPassMax:0.00}      Г:{AnpFreigMax:0.00}",
                                    Comment = $"{ AnpPass:0.00}",
                                    PassengerSpeedAllow = kilometer.Speeds.First().Passenger,
                                    PassengerSpeedLimit = kilometer.Speeds.First().Passenger > Ogr ? Ogr : -1,
                                    FreightSpeedAllow = kilometer.Speeds.First().Freight,
                                    FreightSpeedLimit = kilometer.Speeds.First().Freight > Ogr ? Ogr : -1,
                                    //Pch = kilometer.PdbSection[0].Distance,
                                    DirectionName = direction.Name,
                                    TrackName = kilometer.Track_name
                                });
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"АНП write s3 error {e.Message}");
                        }


                    }

                    var gotPruCurve = false;
                    //  for (int i = 1; i < vershListLVL.Count; i = i + 2)

                    if (kilometer.Number == 6937)
                    {

                    }
                    for (int i = 1; i < vershListLVL.Count; i = i + 2)
                    {
                        var item = vershListLVL[i];

                        var r = item.Select(o => Math.Abs(o.Trapez_str)).Max();
                        //var h = vershListLVL.Select(it => it.Select(o => Math.Abs(o.Trapez_level)).Max()).Max();
                        var h = item.Select(o => Math.Abs(o.Trapez_level)).Max();

                        if (item.First().Km != item.Last().Km)
                        {
                            item = item.Where(o => o.Km == kilometer.Number).ToList();
                        }
                        if (item.Count() < 10) continue;
                        var avgmeterbyItem = item[item.Count() / 2].M;
                        var kmrealCurveCenter = item[item.Count / 2].Km.ToDoubleCoordinate((int)avgmeterbyItem);

                        var kmrealCurveCenterSdvig = (int)item[item.Count / 2].Km.ToDoubleCoordinate((int)avgmeterbyItem - 100);


                        lvl = (int)h;

                        if (kilometer.Number == item.First().Km || (item.First().Km != item.Last().Km))
                        //if (kilometer.Number == item.First().Km))
                        {



                            var kfivfact = new DigressionMark()
                            {
                                Km = item.First().Km,
                                Meter = (item.First().Km == item.Last().Km) ? (int)avgmeterbyItem : (item.First().M + kilometer.Final_m) / 2,
                                lvl = (int)Math.Abs(h),
                                Alert = $"кривая факт. R:{ (17860 / Math.Abs(r)):0} H:{ Math.Abs(h):0}",
                                Radiusr = (17860 / Math.Abs(r)).ToString()
                            };
                            var rs = pru_dig_list.Where(b => Math.Abs(b.lvl - lvl) < 10).Count();
                            if (pru_dig_list.Where(o => o.Alert == kfivfact.Alert).Count() > 0 && rs == 0)
                            {
                               

                                pru_dig_list.Add(kfivfact);

                             

                            }

                        }



                        if (kilometer.Number == 7242)
                        {

                        }

                        //------------------------------------------------------------------------------------------
                        //----ПРУ-----------------------------------------------------------------------------------
                        //------------------------------------------------------------------------------------------
                        var x = rdcs[curve_center_ind].Km;



                        if (kmrealCurveCenterSdvig != kilometer.Number) continue;

                        var realCurveCenter = item[item.Count / 2].Km.ToDoubleCoordinate((int)item[item.Count / 2].M);
                        //if (kilometer.Number != item.First().Km) continue; 

                        //var realCurveCenter = item[item.Count / 2].Km.ToDoubleCoordinate(item[item.Count / 2].M);
                        // var pmeter = item[item.Count / 2].M;
                        var pmeter = (int)avgmeterbyItem;
                        pmeter = kilometer.LevelAvgTrapezoid.Count - 1 < pmeter ? kilometer.LevelAvgTrapezoid.Count - 1 : pmeter;


                        PasSpeed = kilometer.Speeds.Any() ? kilometer.Speeds.First().Passenger : -1;
                        var sp1 = kilometer.Speeds.First().RealStartCoordinate;
                        var sp2 = kilometer.Speeds.First().RealFinalCoordinate;
                        var sp11 = kilometer.Speeds.Last().RealStartCoordinate;
                        var sp22 = kilometer.Speeds.Last().RealFinalCoordinate;

                        if ((kilometer.Number + pmeter / 10000.0 < kilometer.Speeds.Last().RealFinalCoordinate) && (kilometer.Number + pmeter / 10000.0 > kilometer.Speeds.Last().RealStartCoordinate))

                        {
                            PasSpeed = kilometer.Speeds.Last().Passenger;

                        }
                        if ((kilometer.Number + pmeter / 10000.0 < kilometer.Speeds.First().RealFinalCoordinate) && (kilometer.Number + pmeter / 10000.0 > kilometer.Speeds.First().RealStartCoordinate))

                        {
                            PasSpeed = kilometer.Speeds.First().Passenger;

                        }





                        var passportLvlData = bpd_curve.Elevations.Where(o => realCurveCenter.Between(o.RealStartCoordinate, o.RealFinalCoordinate)).ToList();


                        var pLvl = lvl + 0.0;

                        if (kilometer.Number == 6907 || kilometer.Number == 6999)
                        {

                        }
                        if (lvl == 0) continue;
                        foreach (var p in passportLvlData)
                        {

                            var localdiff = Math.Abs(Math.Abs(lvl) - Math.Abs(p.Lvl));
                            if (localdiff > Math.Abs(Math.Abs(lvl) - (int)Math.Abs(pLvl)))
                            {
                                pLvl = p.Lvl;
                            }
                        }

                        var diff = Math.Abs(Math.Abs(lvl) - (int)Math.Abs(pLvl));
                        //if (bpd_curve.Elevations.Count == 0 && bpd_curve.Straightenings.Count > 0)
                        //{
                        //    diff = (int)Math.Abs(lvl);
                        //}
                        var rdcsData = new Data { };

                        var Vkr = RoundNumToFive(rdcsData.GetKRSpeedPass(rdcs));
                        var Ogr = -1;
                        if (kilometer.Speeds.First().Passenger > Vkr)
                            Ogr = Vkr;
                        //балл
                        var ball = -1;
                        var razn = -1;
                        if (140 < PasSpeed && 10 < diff)
                        {
                            ball = 50;
                            razn = diff - 10;
                        }
                        else if ((101 <= PasSpeed && PasSpeed <= 140) && 15 < diff)
                        {
                            ball = 50;
                            razn = diff - 15;
                        }
                        else if ((61 <= PasSpeed && PasSpeed <= 100) && 20 < diff)
                        {
                            ball = 50;
                            razn = diff - 20;
                        }
                        else if (PasSpeed <= 60 && 25 < diff)
                        {
                            ball = 50;
                            razn = diff - 25;
                        }

                        if (ball != -1 && gotPruCurve == false)
                        {
                            try
                            {
                                //if (pru_dig_list.Last().DigName == DigressionName.Pru.Name && pmeter== ru_dig_list
                                //{

                                //}

                                //var rs = pru_dig_list.Where(b =>b.DigName == DigressionName.Pru.Name && Math.Abs(b.Meter - pmeter)<1).Count();

                                //if (rs > 0) continue;

                                //if (!ru_dig_list.Contains(DigressionMark.Equals)                                //{ }
                                if (item.Count < 21) continue;
                                pru_dig_list.Add(new DigressionMark
                                {
                                    Km = kilometer.Number,
                                    Meter = pmeter,
                                    Value = diff, //высота
                                    Length = item.Count, // длина круговой
                                    Count = ball,
                                    DigName = DigressionName.Pru.Name,
                                    //Pch = kilometer.PdbSection[0].Distance,
                                    DirectionName = direction.Name,
                                    TrackName = kilometer.Track_name,
                                    PassengerSpeedAllow = kilometer.Speeds.First().Passenger,
                                    PassengerSpeedLimit = kilometer.Speeds.First().Passenger > Ogr ? Ogr : -1,
                                    FreightSpeedAllow = kilometer.Speeds.First().Freight,
                                    FreightSpeedLimit = kilometer.Speeds.First().Freight > Ogr ? Ogr : -1
                                });



                              
                                
                                gotPruCurve = true;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"ПрУ write s3 error {e.Message}");
                            }
                        }




                    }


                }
                if (kilometer.Number == 6653)
                {

                }
                if (pru_dig_list.Any())
                    MainTrackStructureRepository.Pru_write(kilometer.Track_id, kilometer, pru_dig_list);

                if (curve_bpd_list.Any())
                    MainTrackStructureRepository.Bpd_write(kilometer.Track_id, kilometer, curve_bpd_list);

                // добавление ПрУ и натурные значения кривой
                kilometer.Digressions = Curve_nature_value;

                //контрольные участки КУ МО СКО

                var ku = new List<DigressionMark> { };
                try
                {
                    if (kilometer.Number == 6653)
                    {

                    }
                    //Console.WriteLine("Заходим в бд ищем ку.Any()");
                    var MtoCheckSection = MainTrackStructureRepository.GetMtoObjectsByCoord(
                        trip.Trip_date,
                        kilometer.Number,
                        MainTrackStructureConst.MtoCheckSection,
                        trip.Direction_id,
                        kilometer.Track_name) as List<CheckSection>;
                    //Console.WriteLine("Заходим в бд ищем ку.Any()"+ MtoCheckSection.Last().Start_M);

                    foreach (var sect in MtoCheckSection)
                    {
                        //    Console.WriteLine("Зашли дальше если  в бд есть ку.Any()" + MtoCheckSection.Last().Start_M);
                        var CheckSection_center = (sect.Start_Km * 1000 + sect.Start_M) + ((sect.Final_Km * 1000 + sect.Final_M) - (sect.Start_Km * 1000 + sect.Start_M)) / 2;
                        var pkm = CheckSection_center / 1000;
                        var first_pmeter = CheckSection_center % 1000;
                        if (kilometer.Number <= 6653)
                        {

                        }
                        if (pkm == kilometer.Number)
                        {
                            if (kilometer.Number <= 6653)
                            {

                            }
                            // var CheckVerifyKm = RdStructureRepository.CheckVerify(
                            //     kilometer.Trip.Id,
                            //    sect.Start_Km * 1000 + sect.Start_M,
                            //    sect.Final_Km * 1000 + sect.Final_M);

                            var CheckVerifyKm = RdStructureRepository.CheckVerify(
                                                            kilometer.Trip.Id,
                                                            sect.Start_Km * 1000 + sect.Start_M,
                                                            sect.Final_Km * 1000 + sect.Final_M);

                            //    Console.WriteLine("проверка на КУ CheckVerifyKm.Any()"+ CheckVerifyKm.Last().Start_M);
                            if (kilometer.Number <= 6653)
                            {

                            }
                            if (CheckVerifyKm.Any())
                            {

                                var curve_msg = $"КУ: пар. ур. в н._(МО: {sect.Avg_level:0.0}/{CheckVerifyKm.Last().Trip_mo_level:0.0})";

                                // Console.WriteLine("КУ: пар. ур. в н._(МО: CheckVerifyKm.Any()" + curve_msg);
                                //curve
                                //curve
                                //var curve_msg = $"КУ: параметр уровень в норме_" +
                                //                $"(МО: {sect.Avg_level:0.0}/{CheckVerifyKm.First().Trip_mo_level:0.0})";
                                //var diff_curve = Math.Abs(sect.Avg_level - CheckVerifyKm.First().Trip_mo_level);
                                //if (diff_curve >= 2)
                                //{
                                //    curve_msg = $"КУ: превыш. допуска смещения_" +
                                //          $"({diff_curve:0.0}) пар. уровень (МО: {sect.Avg_level:0.0}/{CheckVerifyKm.First().Trip_mo_level:0.0})!";
                                //}
                                ////gauge
                                //var gauge_msg = $"КУ: параметр шаблон в норме_" +
                                //                $"(МО: {sect.Avg_width:0.0}/{CheckVerifyKm.First().Trip_mo_gauge:0.0})";
                                //var diff_gauge = Math.Abs(sect.Avg_width - CheckVerifyKm.First().Trip_mo_gauge);
                                //if (diff_gauge > 2)
                                //{
                                //    gauge_msg = $"КУ: превыш. допуска смещения_" +
                                //          $"({diff_gauge:0.0}) пар. шаблон (МО: {sect.Avg_width:0.0}/{CheckVerifyKm.First().Trip_mo_gauge:0.0})!";
                                //}
                                //var curve_msg = $"КУ: пар. ур. в н._(МО: {Math.Abs(sect.Avg_level) * Math.Sign(CheckVerifyKm.First().Trip_mo_level):0.0}/{CheckVerifyKm.First().Trip_mo_level:0.0})";




                                var diff_curve = Math.Abs(sect.Avg_level - CheckVerifyKm.Last().Trip_mo_level);
                                if (diff_curve >= 2)
                                {

                                    curve_msg = $"КУ: прев. доп-ка. смещ._({diff_curve:0.0}) пар. ур. (МО:  { sect.Avg_level:0.0}/{CheckVerifyKm.Last().Trip_mo_level:0.0})!";

                                }
                                //gauge
                                var gauge_msg = $"КУ: пар. шаб. в н._(МО: { sect.Avg_width:0.0}/{ CheckVerifyKm.Last().Trip_mo_gauge:0.0})";

                                var diff_gauge = Math.Abs(sect.Avg_width - CheckVerifyKm.First().Trip_mo_gauge);
                                if (diff_gauge > 2)
                                {

                                    gauge_msg = $"КУ: прев. доп-ка смещ._({diff_gauge:0.0}) пар. шаб. (МО: {sect.Avg_width:0.0}/{CheckVerifyKm.First().Trip_mo_gauge:0.0})!";

                                }
                                ku.Add(new DigressionMark()
                                {
                                    Meter = first_pmeter,

                                    Alert = $"{curve_msg}"
                                });

                                ku.Add(new DigressionMark()
                                {
                                    Meter = first_pmeter,
                                    //NotMoveAlert = true,
                                    Alert = $"{gauge_msg}"
                                });
                            }
                            //   Console.WriteLine("Зашло в процедуру CheckVerifyKm1.Any()" );
                        }
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine("контрольные участки КУ МО СКО error " + e.Message);
                }

                kilometer.Digressions.AddRange(ku);
                kilometer.LoadDigresions(RdStructureRepository, MainTrackStructureRepository, trip);


                //foreach (var item in kilometer.Digressions)
                //{
                //    if (item.Digression.Name == DigressionName.DrawdownLeft.Name || item.Digression.Name == DigressionName.DownhillRight.Name)
                //    {
                //        foreach (var IsoJoints in kilometer.IsoJoints)
                //        {
                //            if (IsoJoints.Meter.Between(item.Meter - item.Length / 2, item.Meter + item.Length / 2) && item.Comment == "")
                //                item.Comment = "ис";
                //        }
                //    }
                //}

                string drawdownRight = string.Empty, drawdownLeft = string.Empty, gauge = string.Empty, zeroGauge = string.Empty,
                        zeroStraightening = string.Empty, averageStraighteningRight = string.Empty, straighteningRight = string.Empty,
                        zeroStraighteningLeft = string.Empty, averageStraighteningLeft = string.Empty, straighteningLeft = string.Empty,
                        level = string.Empty, averageLevel = string.Empty, zeroLevel = string.Empty, avglevel = string.Empty;

                int fourStepOgrCoun = 0, otherfourStepOgrCoun = 0;

                var trackclasses = (List<TrackClass>)MainTrackStructureRepository.GetMtoObjectsByCoord(trip.Trip_date, kilometer.Number, MainTrackStructureConst.MtoTrackClass, kilometer.Track_id);

                svgLength = kilometer.GetLength() < 1000 ? 1000 : kilometer.GetLength();
                var xp = (-kilometer.Start_m - svgLength - 50) + (svgLength + 105) - 52;
                var runnin = "";
                if (kilometer.Runninin.Count > 0)
                {
                    if (kilometer.Runninin[0].Start_Km == kilometer.Number)
                    {
                        runnin = kilometer.Runninin[0].Reason + "пк" + (((kilometer.Runninin[0].Start_M + 1) / 100) + 1) + "-" + (((kilometer.Runninin[0].Final_M + 1) / 100) + 1) + "V:" + kilometer.Runninin[0].Passenger;
                    }

                }



                XElement addParam = new XElement("addparam",

                    new XAttribute("top-title", (runnin != "" ? runnin : "") +
                           (direction != null ? $"{direction.Name} ({direction.Code})" : "Неизвестный") + " Путь: " +
                        kilometer.Track_name + $" Класс: {(!trackclasses.Any() || trackclasses.First().Class_Id.ToString() == "6" ? "-" : trackclasses.First().Class_Id.ToString())} Км:" + kilometer.Number + " " +
                        (kilometer.PdbSection.Count > 0 ? $" ПЧ-{kilometer.PdbSection[0].Distance}" : " ПЧ-") + " Уст: " + " " +
                        (kilometer.Speeds.Count > 0 ? $"{kilometer.Speeds.First().Passenger}/{kilometer.Speeds.First().Freight}" : "-/-"))
                    {

                    },
                    //new XAttribute("top-title",
                    //    (direction != null ? $" {(kilometer.Runninin[0].Start_Km == kilometer.Number ? kilometer.Runninin[0].Reason + "V:" + kilometer.Runninin[0].Passenger + "Км:" + kilometer.Runninin[0].Start_Km + "ПК:" + (((kilometer.Runninin[0].Start_M + 1) / 100) + 1) : "")} {direction.Name} ({direction.Code})" : "Неизвестный") + " Путь: " +
                    // kilometer.Track_name + $" Класс: {(trackclasses.Any() ? trackclasses.First().Class_Id.ToString() : "-")} Км:" + kilometer.Number + " " +
                    // (kilometer.PdbSection.Count > 0 ? $" ПЧ-{kilometer.PdbSection[0].Distance}" : " ПЧ-") + " Уст: " + " " +
                    // (kilometer.Speeds.Count > 0 ? $"{kilometer.Speeds.First().Passenger}/{kilometer.Speeds.First().Freight}" : "-/-")),


                    new XAttribute("right-title",
                        //copyright + ": " + "ПО " + softVersion + "  " +
                        //systemName + ":" + trip.Car +
                        //"(" + trip.Chief.Trim() + ")" +
                        " (БПД от " + MainTrackStructureRepository.GetModificationDate() + ") <" + (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].RoadAbbr : "НЕИЗВ") + ">" + "<" + kilometer.Passage_time.ToString("dd.MM.yyyy  HH:mm") + ">" +
                        "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(trip.Travel_Direction.ToString())) + ">" +
                        "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(trip.Car_Position.ToString())) + ">" +
                        "<" + trip.Trip_date.Month + "-" + trip.Trip_date.Year + " " + (trip.Trip_Type == TripType.Control ? "контр." : trip.Trip_Type == TripType.Work ? "раб." : "доп.") + " Проезд:" + trip.Trip_date.ToString("dd.MM.yyyy  HH:mm") + " " + diagramName + ">"
                        ),
                    new XAttribute("pre", xp + 30),
                    new XAttribute("prer", xp + 21),
                    new XAttribute("topr", -kilometer.Start_m - svgLength - 45),
                    new XAttribute("topf", xp + 10),
                    new XAttribute("topx", -kilometer.Start_m - svgLength),
                    new XAttribute("topx1", -kilometer.Start_m - svgLength - 30),
                    new XAttribute("topx2", -kilometer.Start_m - svgLength - 15),
                    new XAttribute("fragment", (kilometer.StationSection != null && kilometer.StationSection.Count > 0 ? "Станция: " + kilometer.StationSection[0].Station : (kilometer.Sector != null ? kilometer.Sector.ToString() : "")) + " Км:" + kilometer.Number),
                    new XAttribute("viewbox", $"-20 {-kilometer.Start_m - svgLength - 50} 830 {svgLength + 105}"),
                    new XAttribute("minY", -kilometer.Start_m),
                    new XAttribute("maxY", -kilometer.Final_m),
                    new XAttribute("minYround", -(kilometer.Start_m - kilometer.Start_m % 100)),

                    RightSideChart(trip.Trip_date, kilometer, kilometer.Track_id, new float[] { 151f, 146f, 152.5f, 155f }),

                    new XElement("xgrid",
                        new XElement("x", MMToPixelChartString(LevelPosition - LevelStep),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "  –30"),
                        new XAttribute("y", MMToPixelChartString(LevelPosition - LevelStep - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(LevelPosition),
                        new XAttribute("dasharray", "3,3"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "      0"),
                        new XAttribute("y", MMToPixelChartString(LevelPosition - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(LevelPosition + LevelStep),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "    30"),
                        new XAttribute("y", MMToPixelChartString(LevelPosition + LevelStep - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(StraighRighttPosition - StrightStep),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "  –30"),
                        new XAttribute("y", MMToPixelChartString(StraighRighttPosition - StrightStep - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(StraighRighttPosition),
                        new XAttribute("dasharray", "3,3"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "      0"),
                        new XAttribute("y", MMToPixelChartString(StraighRighttPosition - 1f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(StraighRighttPosition + StrightStep / 10f),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "      3"),
                        new XAttribute("y", MMToPixelChartString(StraighRighttPosition + StrightStep / 10f + 0.2f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(StrightLeftPosition - StrightStep / 10f),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "    –3"),
                        new XAttribute("y", MMToPixelChartString(StrightLeftPosition - StrightStep / 10f - 1f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(StrightLeftPosition),
                        new XAttribute("dasharray", "3,3"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "      0"),
                        new XAttribute("y", MMToPixelChartString(StrightLeftPosition + 0.2f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(StrightLeftPosition + StrightStep),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "    30"),
                        new XAttribute("y", MMToPixelChartString(StrightLeftPosition + StrightStep - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(GaugePosition - 10 * GaugeKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black")),

                        new XElement("x", MMToPixelChartString(GaugePosition - 8 * GaugeKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "1512"),
                        new XAttribute("y", MMToPixelChartString(GaugePosition - 8 * GaugeKoef - 0.5f)),

                        new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(GaugePosition - 4 * GaugeKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "grey")),

                        new XElement("x", MMToPixelChartString(GaugePosition),
                        new XAttribute("dasharray", "3,3"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "1520"),
                        new XAttribute("y", MMToPixelChartString(GaugePosition - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(GaugePosition + 8 * GaugeKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "1528"),
                        new XAttribute("y", MMToPixelChartString(GaugePosition + 8 * GaugeKoef - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(GaugePosition + 16 * GaugeKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "1536"),
                        new XAttribute("y", MMToPixelChartString(GaugePosition + 16 * GaugeKoef - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(GaugePosition + 22 * GaugeKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "1542"),
                        new XAttribute("y", MMToPixelChartString(GaugePosition + 22 * GaugeKoef - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(GaugePosition + 26 * GaugeKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black")),

                        new XElement("x", MMToPixelChartString(GaugePosition + 28 * GaugeKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "1548"),
                        new XAttribute("y", MMToPixelChartString(GaugePosition + 28 * GaugeKoef - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(ProsRightPosition - 10 * ProsKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "  –10"),
                        new XAttribute("y", MMToPixelChartString(ProsRightPosition - 10 * ProsKoef - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(ProsRightPosition),
                        new XAttribute("dasharray", "3,3"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "      0"),
                        new XAttribute("y", MMToPixelChartString(ProsRightPosition - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(ProsRightPosition + 10 * ProsKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "    10"),
                        new XAttribute("y", MMToPixelChartString(ProsRightPosition + 10 * ProsKoef - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(ProsLeftPosition - 10 * ProsKoef),
                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "  –10"),
                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition - 10 * ProsKoef - 0.5f)),
                        new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(ProsLeftPosition),
                        new XAttribute("dasharray", "3,3"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "      0"),
                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition - 0.5f)),
                        new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(ProsLeftPosition + 10 * ProsKoef),

                        new XAttribute("dasharray", "0.5,2"),
                        new XAttribute("stroke", "black"),
                        new XAttribute("label", "    10"),
                        new XAttribute("y", MMToPixelChartString(ProsLeftPosition + 10 * ProsKoef - 0.5f)),
                        new XAttribute("x", xp + 15))
                        ));

                //kil
                for (int index = 0; index < kilometer.meter.Count - 1; index++)
                {
                    try
                    {
                        int metre = -kilometer.meter[index];
                        drawdownRight += MMToPixelChartString(kilometer.DrawdownLeft[index] * ProsKoef + ProsRightPosition) + "," + metre + " ";
                        drawdownLeft += MMToPixelChartString(kilometer.DrawdownRight[index] * ProsKoef + ProsLeftPosition) + "," + metre + " ";
                        gauge += MMToPixelChartString((kilometer.Gauge[index] - 1520) * GaugeKoef + GaugePosition) + "," + metre + " ";
                        zeroGauge += MMToPixelChartString((kilometer.fsh0[index] - 1520) * GaugeKoef + GaugePosition) + "," + metre + " ";

                        //пасспорт рихт
                        zeroStraightening += MMToPixelChartString(kilometer.fZeroStright[index] * StrightKoef + StraighRighttPosition) + "," + metre + " ";
                        zeroStraighteningLeft += MMToPixelChartString(kilometer.fZeroStright[index] * StrightKoef + StrightLeftPosition) + "," + metre + " ";
                        //Паспорт уровень
                        zeroLevel += MMToPixelChartString(kilometer.flvl0[index] * LevelKoef + LevelPosition) + "," + metre + " ";

                        //ср линия рихт
                        averageStraighteningRight += MMToPixelChartString(kilometer.StrightAvgTrapezoid[index] * StrightKoef + StraighRighttPosition) + "," + metre + " ";
                        averageStraighteningLeft += MMToPixelChartString(kilometer.StrightAvgTrapezoid[index] * StrightKoef + StrightLeftPosition) + "," + metre + " ";

                        //трапеция рихт
                        var drh = kilometer.StrightAvgTrapezoid[index] + (kilometer.StrightRight[index] - kilometer.StrightAvgTrapezoid[index]);
                        straighteningRight += MMToPixelChartString(drh * StrightKoef + StraighRighttPosition) + "," + metre + " ";

                        var drh1 = kilometer.StrightAvgTrapezoid[index] + (kilometer.StrightLeft[index] - kilometer.StrightAvgTrapezoid[index]);
                        straighteningLeft += MMToPixelChartString(drh1 * StrightKoef + StrightLeftPosition) + "," + metre + " ";



                        level += MMToPixelChartString(kilometer.Level[index] * LevelKoef + LevelPosition) + "," + metre + " ";
                        averageLevel += MMToPixelChartString(kilometer.LevelAvgTrapezoid[index] * LevelKoef + LevelPosition) + "," + metre + " ";

                        avglevel += MMToPixelChartString(kilometer.LevelAvg[index] * LevelKoef + LevelPosition) + "," + metre + " ";
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Рисование линий А4 ошибка " + e.Message);
                    }
                }

                var styletest = "fill:none;stroke:green;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:3";
                var style = "fill:none;stroke:black;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:1";
                var styleAverage = "fill:none;stroke:black;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:1; stroke-dasharray:0.7 0.6;";
                var styleAvg = "fill:none;stroke:dimgray;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:0.5; stroke-dasharray:4 2;stroke:dodgerblue";

                addParam.Add(new XElement("polyline", new XAttribute("points", drawdownRight), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", drawdownLeft), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", gauge), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", zeroGauge), new XAttribute("style", style)));

                addParam.Add(new XElement("polyline", new XAttribute("points", zeroStraightening), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", averageStraighteningRight), new XAttribute("style", styleAverage)));
                addParam.Add(new XElement("polyline", new XAttribute("points", straighteningRight), new XAttribute("style", style)));

                addParam.Add(new XElement("polyline", new XAttribute("points", zeroStraighteningLeft), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", averageStraighteningLeft), new XAttribute("style", styleAverage)));
                addParam.Add(new XElement("polyline", new XAttribute("points", straighteningLeft), new XAttribute("style", style)));

                addParam.Add(new XElement("polyline", new XAttribute("points", level), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", averageLevel), new XAttribute("style", styleAverage)));
                addParam.Add(new XElement("polyline", new XAttribute("points", avglevel), new XAttribute("style", styleAvg)));
                addParam.Add(new XElement("polyline", new XAttribute("points", zeroLevel), new XAttribute("style", style)));


                char separator = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];

                var digElemenets = new XElement("digressions");
                List<int> usedTops = new List<int>();
                List<int> speedmetres = new List<int>();

                var gmeter = kilometer.Start_m.RoundTo10() + 10;


                foreach (var picket in kilometer.Pickets)
                {
                    picket.WriteNotesToReport(
                        kilometer,
                        speedmetres,
                        addParam,
                        digElemenets,
                        ProsRightPosition,
                        ProsLeftPosition,
                        StraighRighttPosition,
                        StrightLeftPosition,
                        GaugePosition,
                        LevelPosition,
                        this,
                        ref fourStepOgrCoun,
                        ref otherfourStepOgrCoun);
                }

                //addParam.Add(new XAttribute("common", kilometer.GetdigressionsCount));

                var GetBedomost = new List<Bedemost> { };
                try
                {
                    // 1степень санау
                    GetBedomost = ((List<Bedemost>)RdStructureRepository.GetBedemost(kilometer.Trip.Id)).Where(o => o.Km == kilometer.Number).ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine("GetBedomost " + e.Message);
                }
                addParam.Add(
                    new XAttribute("speedlimit",
                    kilometer.CalcMainPoint() + " " + $"Кол.ст.- 1:{(GetBedomost.Any() ? GetBedomost.First().Type1 : 0)}; " + kilometer.GetdigressionsCount +
                    "  Кол.огр." + fourStepOgrCoun + "/" + otherfourStepOgrCoun + " Огр. " + kilometer.SpeedLimit + $" Скор.{(int)kilometer.Speed.Average()}")
                    );

                addParam.Add(digElemenets);
                report.Add(addParam);

                xdReport.Add(report);

                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);

            }
            try
            {
                htReport.Save($@"G:\form\{kilometer.Number}_{kilometer.Track_id}.html");

                //htReport.Save($@"\\192.168.1.200\form\{kilometer.Number}_{kilometer.Track_id}.html");
                var svg = htReport.Element("html").Element("body").Element("div").Element("div").Element("svg");
                var svgDoc = SvgDocument.FromSvg<SvgDocument>(svg.ToString());
                svgDoc.Width = 800 * 3;
                svgDoc.Height = (svgLength + 95) * 3;

                if (autoprint)
                {

                    PrintDocument pd = new PrintDocument();
                    //pd.PrinterSettings.PrinterName = "EPSON5EEE22";
                    Console.WriteLine(pd.PrinterSettings.PrinterName);

                    pd.PrintPage += (sender, args) =>
                    {
                        var i = svgDoc.Draw();
                        //i.Save($@"g:\form\{kilometer.Number}_{kilometer.Track_id}.png");
                        htReport.Save($@"G:\form\{kilometer.Number}_{kilometer.Track_id}.html");
                        pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                        pd.OriginAtMargins = true;
                        pd.DefaultPageSettings.Landscape = true;
                        Point p = new Point(0, 0);
                        args.Graphics.DrawImage(i, 0, 0, i.Width / 3, i.Height / 3 - 15);
                    };
                    pd.Print();
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Ошибка при сохранении отчетной формы: " + e.Message);
            }
            finally
            {
                // System.Diagnostics.Process.Start(Path.GetTempPath() + "/report.html");
            }
        }

        private void pd_print(object sender, PrintPageEventArgs e)
        {
            Graphics gr = e.Graphics;
            gr.DrawString("Sales", new Font(FontFamily.GenericMonospace, 12, FontStyle.Bold), new SolidBrush(Color.Black), new Point(40, 40));
        }
        public void ProcessRegion(ReportTemplate template, Kilometer kilometer, Trips trip, bool autoprint, int currentKmMeter)
        {
            XDocument htReport = new XDocument();
            var svgLength = 0;
            using (XmlWriter writer = htReport.CreateWriter())
            {
                XDocument xdReport = new XDocument();
                XElement report = new XElement("report");

                int svgIndex = template.Xsl.IndexOf("</svg>");
                template.Xsl = template.Xsl.Insert(svgIndex, RighstSideXslt());

                //ПРУ
                var PasSpeed = kilometer.Speeds.Any() ? kilometer.Speeds.First().Passenger : -1;
                var pru_dig_list = new List<DigressionMark> { };
                var curve_bpd_list = new List<DigressionMark> { };
                var Curve_nature_value = new List<DigressionMark>();

                foreach (var bpd_curve in kilometer.Curves)
                {
                    List<RDCurve> rdcs = RdStructureRepository.GetRDCurves(bpd_curve.Id, trip.Id);
                    var LevelPoins = rdcs.Where(o => o.Point_level > 0).ToList();
                    var StrPoins = rdcs.Where(o => o.Point_str > 0).ToList();

                    int lvl = -1, str = -1, lenPerKrivlv = -1;

                    try
                    {
                        var lenPerKriv10000 = ((StrPoins[1].Km + StrPoins[1].M / 10000.0) - (StrPoins[2].Km + StrPoins[2].M / 10000.0)) * 10000;
                        var lenPerKriv = Math.Abs((int)lenPerKriv10000 % 1000);

                        var lenKriv10000 = ((StrPoins[0].Km + StrPoins[0].M / 10000.0) - (StrPoins[3].Km + StrPoins[3].M / 10000.0)) * 10000;
                        var lenKriv = Math.Abs((int)lenKriv10000 % 1000);

                        var lenPerKriv10000lv = ((LevelPoins[1].Km + LevelPoins[1].M / 10000.0) - (LevelPoins[2].Km + LevelPoins[2].M / 10000.0)) * 10000;
                        lenPerKrivlv = Math.Abs((int)lenPerKriv10000lv % 1000);

                        var lenKriv10000lv = ((LevelPoins[0].Km + LevelPoins[0].M / 10000.0) - (LevelPoins[3].Km + LevelPoins[3].M / 10000.0)) * 10000;
                        var lenKrivlv = Math.Abs((int)lenKriv10000lv % 1000);

                        var d = false;
                        if ((StrPoins[0].Km + StrPoins[0].M / 10000.0) > (StrPoins[3].Km + StrPoins[3].M / 10000.0))
                            d = true;

                        //нижние 2 точки трапеции
                        var start_km = d ? StrPoins.Last().Km : StrPoins.First().Km;
                        var start_m = d ? StrPoins.Last().M : StrPoins.First().M;
                        var final_km = d ? StrPoins.First().Km : StrPoins.Last().Km;
                        var final_m = d ? StrPoins.First().M : StrPoins.Last().M;

                        var start_lvl_km = d ? LevelPoins.Last().Km : LevelPoins.First().Km;
                        var start_lvl_m = d ? LevelPoins.Last().M : LevelPoins.First().M;
                        var final_lvl_km = d ? LevelPoins.First().Km : LevelPoins.Last().Km;
                        var final_lvl_m = d ? LevelPoins.First().M : LevelPoins.Last().M;

                        var razn1 = (int)(((start_km + start_m / 10000.0) - (start_lvl_km + start_lvl_m / 10000.0)) * 10000) % 1000; // start
                        var razn2 = (int)(((final_km + final_m / 10000.0) - (final_lvl_km + final_lvl_m / 10000.0)) * 10000) % 1000; // final
                        var razn3 = lenKriv - lenKrivlv; // общая длина нижних

                        //верхние 2 точки трапеции
                        var start_kmc = d ? StrPoins[2].Km : StrPoins[1].Km;
                        var start_mc = d ? StrPoins[2].M : StrPoins[1].M;
                        var final_kmc = d ? StrPoins[1].Km : StrPoins[2].Km;
                        var final_mc = d ? StrPoins[1].M : StrPoins[2].M;

                        var start_lvl_kmc = d ? LevelPoins[2].Km : LevelPoins[1].Km;
                        var start_lvl_mc = d ? LevelPoins[2].M : LevelPoins[1].M;
                        var final_lvl_kmc = d ? LevelPoins[1].Km : LevelPoins[2].Km;
                        var final_lvl_mc = d ? LevelPoins[1].M : LevelPoins[2].M;

                        var razn1c = (int)(((start_kmc + start_mc / 10000.0) - (start_lvl_kmc + start_lvl_mc / 10000.0)) * 10000) % 1000; // start
                        var razn2c = (int)(((final_kmc + final_mc / 10000.0) - (final_lvl_kmc + final_lvl_mc / 10000.0)) * 10000) % 1000; // final

                        //Переходные 
                        //1-й
                        var tap_len1 = Math.Round(((start_km + start_m / 10000.0) - (start_kmc + start_mc / 10000.0)) * 10000) % 1000;
                        var tap_len1_lvl = Math.Round(((start_lvl_km + start_lvl_m / 10000.0) - (start_lvl_kmc + start_lvl_mc / 10000.0)) * 10000) % 1000;
                        //2-й
                        var tap_len2 = Math.Round(((final_km + final_m / 10000.0) - (final_kmc + final_mc / 10000.0)) * 10000) % 1000;
                        var tap_len2_lvl = Math.Round(((final_lvl_km + final_lvl_m / 10000.0) - (final_lvl_kmc + final_lvl_mc / 10000.0)) * 10000) % 1000;

                        //Радиус/Уровень (для мин макс сред)
                        var temp_data = rdcs.GetRange((int)Math.Abs(tap_len1_lvl) + 40, Math.Abs(lenPerKrivlv));
                        var temp_data_str = rdcs.Where(o => (start_kmc + start_mc / 10000.0) <= (o.Km + o.M / 10000.0) && (o.Km + o.M / 10000.0) <= (final_kmc + final_mc / 10000.0)).ToList();
                        var temp_data_lvl = rdcs.Where(o => (start_lvl_kmc + start_lvl_mc / 10000.0) <= (o.Km + o.M / 10000.0) && (o.Km + o.M / 10000.0) <= (final_lvl_kmc + final_lvl_mc / 10000.0)).ToList();

                        //Переходные (для макс сред)
                        var transitional_lvl_data = rdcs.GetRange(40, Math.Abs((int)tap_len1_lvl));
                        var transitional_str_data = rdcs.GetRange(40, Math.Abs((int)tap_len1));

                        var transitional_lvl_data2 = rdcs.GetRange((int)Math.Abs(tap_len1_lvl) + 40 + Math.Abs(lenPerKrivlv), Math.Abs((int)tap_len2_lvl));
                        var transitional_str_data2 = rdcs.GetRange((int)Math.Abs(tap_len1) + 40 + Math.Abs(lenPerKriv), Math.Abs((int)tap_len2));

                        lvl = (int)(temp_data_lvl.Select(o => o.Level).Average());
                        str = (int)(17860 / Math.Abs(temp_data_str.Select(o => o.Radius).Average()));
                    }
                    catch
                    {
                        Console.WriteLine("Ошибка при расчете натурной кривой");
                    }

                    var curve_center = (bpd_curve.Start_Km * 1000 + bpd_curve.Start_M) + ((bpd_curve.Final_Km * 1000 + bpd_curve.Final_M) - (bpd_curve.Start_Km * 1000 + bpd_curve.Start_M)) / 2;
                    var pkm = curve_center / 1000;
                    var first_pmeter = curve_center % 1000;

                    int curve_krug = -1;
                    if (lenPerKrivlv != -1)
                    {
                        curve_krug = lenPerKrivlv;
                    }
                    else
                        curve_krug = Math.Abs((bpd_curve.Elevations.First().Start_Km * 1000 + bpd_curve.Elevations.First().Start_M) -
                                                   (bpd_curve.Elevations.First().Final_Km * 1000 + bpd_curve.Elevations.First().Final_M)) -
                                                   (bpd_curve.Elevations.First().Transition_1 + bpd_curve.Elevations.First().Transition_2);

                    if (pkm == kilometer.Number)
                    {
                        var rdcsData = new Data { };
                        var Vkr = RoundNumToFive(rdcsData.GetKRSpeedPass(rdcs));
                        var Ogr = -1;
                        if (kilometer.Speeds.First().Passenger > Vkr)
                            Ogr = Vkr;
                        var pmeter = kilometer.Length - first_pmeter;
                        pmeter = kilometer.LevelAvgTrapezoid.Count - 1 < pmeter ? kilometer.LevelAvgTrapezoid.Count - 1 : pmeter;


                        var diff = Math.Abs(Math.Abs((int)kilometer.LevelAvgTrapezoid[pmeter]) - Math.Abs((int)bpd_curve.Elevations.First().Lvl));

                        //балл
                        var ball = -1;
                        var razn = -1;
                        if (140 < PasSpeed && 10 < diff)
                        {
                            ball = 50;
                            razn = diff - 10;
                        }
                        else if ((101 <= PasSpeed && PasSpeed <= 140) && 15 < diff)
                        {
                            ball = 50;
                            razn = diff - 15;
                        }
                        else if ((61 <= PasSpeed && PasSpeed <= 100) && 20 < diff)
                        {
                            ball = 50;
                            razn = diff - 20;
                        }
                        else if (PasSpeed <= 60 && 25 < diff)
                        {
                            ball = 50;
                            razn = diff - 25;
                        }

                        if (ball != -1 && !pru_dig_list.Any())
                        {
                            try
                            {
                                pru_dig_list.Add(new DigressionMark
                                {
                                    Km = kilometer.Number,
                                    Meter = first_pmeter,
                                    Value = diff, //высота
                                    Length = curve_krug, // длина круговой
                                    Count = ball,
                                    DigName = DigressionName.Pru.Name,
                                    PassengerSpeedAllow = kilometer.Speeds.First().Passenger,
                                    PassengerSpeedLimit = kilometer.Speeds.First().Passenger > Ogr ? Ogr : -1,

                                    FreightSpeedAllow = kilometer.Speeds.First().Freight,
                                    FreightSpeedLimit = kilometer.Speeds.First().Freight > Ogr ? Ogr : -1,
                                });
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"ПрУ write s3 error {e.Message}");
                            }
                        }


                        if (lvl != -1)
                        {
                            Curve_nature_value.Add(new DigressionMark()
                            {
                                Meter = first_pmeter,
                                Alert = $"{first_pmeter} Крив. факт R:{ str } h:{ lvl }"
                            });
                        }
                    }

                    //if (!curve_bpd_list.Any())
                    {
                        curve_bpd_list.Add(new DigressionMark()
                        {
                            Km = -999,
                            Meter = first_pmeter,

                            Alert = $"{first_pmeter - 30}  Паспорт R:{ str} h:{lvl}"
                        });
                    }

                    //if (curve_bpd_list.Any())
                    // curve_bpd_list.Select()



                }

                if (pru_dig_list.Any())
                    MainTrackStructureRepository.Pru_write(kilometer.Track_id, kilometer, pru_dig_list);
                if (curve_bpd_list.Any())
                    MainTrackStructureRepository.Bpd_write(kilometer.Track_id, kilometer, curve_bpd_list);

                // добавление ПрУ и натурные значения кривой
                kilometer.Digressions = Curve_nature_value;

                //контрольные участки КУ МО СКО
                var ku = new List<DigressionMark> { };
                try
                {
                    var MtoCheckSection = MainTrackStructureRepository.GetMtoObjectsByCoord(
                        trip.Trip_date,
                        kilometer.Number,
                        MainTrackStructureConst.MtoCheckSection,
                        trip.Direction_Name,
                        kilometer.Track_name) as List<CheckSection>;

                    foreach (var sect in MtoCheckSection)
                    {
                        var CheckSection_center = (sect.Start_Km * 1000 + sect.Start_M) + ((sect.Final_Km * 1000 + sect.Final_M) - (sect.Start_Km * 1000 + sect.Start_M)) / 2;
                        var pkm = CheckSection_center / 1000;
                        var first_pmeter = CheckSection_center % 1000;

                        if (pkm == kilometer.Number)
                        {
                            var CheckVerifyKm = RdStructureRepository.CheckVerify(
                                                            kilometer.Trip.Id,
                                                            sect.Start_Km * 1000 + sect.Start_M,
                                                            sect.Final_Km * 1000 + sect.Final_M);
                            if (kilometer.Number == 6624)
                            {

                            }
                            if (CheckVerifyKm.Any())
                            {
                                //curve
                                //var curve_msg = $"КУ: параметр уровень в норме_" +
                                //                $"(МО: {sect.Avg_level:0.0}/{CheckVerifyKm.First().Trip_mo_level:0.0})";
                                //var diff_curve = Math.Abs(Math.Abs(sect.Avg_level) *Math.Sign( CheckVerifyKm.First().Trip_mo_level) - CheckVerifyKm.First().Trip_mo_level);
                                //if (diff_curve >= 2)
                                //{
                                //    curve_msg = $"КУ: превыш. допуска смещения_" +
                                //          $"({diff_curve:0.0}) пар. уровень (МО: {Math.Abs(sect.Avg_level) * Math.Sign(CheckVerifyKm.First().Trip_mo_level):0.0}/{CheckVerifyKm.First().Trip_mo_level:0.0})!";
                                //}
                                ////gauge
                                //var gauge_msg = $"КУ: параметр шаблон в норме_" +
                                //                $"(МО: {sect.Avg_width:0.0}/{CheckVerifyKm.First().Trip_mo_gauge:0.0})";
                                //var diff_gauge = Math.Abs(sect.Avg_width - CheckVerifyKm.First().Trip_mo_gauge);
                                //if (diff_gauge > 2)
                                //{
                                //    gauge_msg = $"КУ: превыш. допуска смещения_" +
                                //          $"({diff_gauge:0.0}) пар. шаблон (МО: {sect.Avg_width:0.0}/{CheckVerifyKm.First().Trip_mo_gauge:0.0})!";
                                //}
                                var curve_msg = $"КУ: пар. ур. в н._(МО: { sect.Avg_level:0.0}/{ CheckVerifyKm.Last().Trip_mo_level:0.0})";

                                //  var diff_curve = Math.Abs(Math.Abs(sect.Avg_level) * Math.Sign(CheckVerifyKm.First().Trip_mo_level) - CheckVerifyKm.First().Trip_mo_level);
                                var diff_curve = sect.Avg_level - CheckVerifyKm.Last().Trip_mo_level;

                                if (Math.Abs(diff_curve) >= 2)
                                {
                                    //sect.Avg_level = sect.Avg_level * (-1);
                                    curve_msg = $"КУ: прев. доп-ка смещ._({diff_curve:0.0}) пар. ур. (МО: {sect.Avg_level:0.0}/{CheckVerifyKm.Last().Trip_mo_level:0.0})!";
                                }
                                //gauge
                                var gauge_msg = $"КУ: пар. шаб. в н._(МО: {sect.Avg_width:0.0}/{CheckVerifyKm.First().Trip_mo_gauge:0.0})";
                                var diff_gauge = Math.Abs(sect.Avg_width - CheckVerifyKm.First().Trip_mo_gauge);
                                if (diff_gauge > 2)
                                {


                                    gauge_msg = $"КУ: прев. доп-ка смещ._({diff_gauge:0.0}) пар. шаб. (МО: {sect.Avg_width:0.0}/{CheckVerifyKm.First().Trip_mo_gauge:0.0})!";
                                }

                                ku.Add(new DigressionMark()
                                {
                                    Meter = first_pmeter,
                                    //NotMoveAlert = true,
                                    Alert = $"{curve_msg}"
                                });

                                ku.Add(new DigressionMark()
                                {
                                    Meter = first_pmeter,
                                    //NotMoveAlert = true,
                                    Alert = $"{gauge_msg}"
                                });
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("контрольные участки КУ МО СКО error " + e.Message);
                }

                kilometer.Digressions.AddRange(ku);
                kilometer.LoadDigresions(RdStructureRepository, MainTrackStructureRepository, trip);

                string drawdownRight = string.Empty, drawdownLeft = string.Empty, gauge = string.Empty, zeroGauge = string.Empty,
                        zeroStraightening = string.Empty, averageStraighteningRight = string.Empty, straighteningRight = string.Empty,
                        zeroStraighteningLeft = string.Empty, averageStraighteningLeft = string.Empty, straighteningLeft = string.Empty,
                        level = string.Empty, averageLevel = string.Empty, zeroLevel = string.Empty;

                int fourStepOgrCoun = 0, otherfourStepOgrCoun = 0;

                svgLength = kilometer.GetLength() < 1000 ? 1000 : kilometer.GetLength();
                var xp = (-kilometer.Start_m - svgLength - 50) + (svgLength + 105) - 52;
                var direction = AdmStructureRepository.GetDirectionByTrack(kilometer.Track_id);

                XElement addParam = new XElement("addparam",
                    new XAttribute("top-title",
                        (direction != null ? $"{direction.Name} ({direction.Code} )" : "Неизвестный") + " Путь: " + kilometer.Track_name + " Км:" +
                        kilometer.Number + " " + (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].ToString() : " ПЧ-/ПЧУ-/ПД-/ПДБ-") + " Уст: " + " " +
                        (kilometer.Speeds.Count > 0 ? $"{kilometer.Speeds.First().Passenger}/{kilometer.Speeds.First().Freight}" : "-/-") +
                        $" Скор:{(int)kilometer.Speed.Average()}"),

                    new XAttribute("right-title",
                        copyright + ": " + "ПО " + softVersion + "  " +
                        systemName + ":" + trip.Car + "(" + trip.Chief.Trim() + ") (БПД от " + MainTrackStructureRepository.GetModificationDate() + ") <" + (kilometer.PdbSection.Count > 0 ? kilometer.PdbSection[0].RoadAbbr : "НЕИЗВ") + ">" + "<" + kilometer.Passage_time.ToString("dd.MM.yyyy  HH:mm") + ">" +
                        "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(trip.Travel_Direction.ToString())) + ">" +
                        "<" + Helper.GetShortFormInNormalString(Helper.GetResourceName(trip.Car_Position.ToString())) + ">" +
                        "<" + trip.Trip_date.Month + "-" + trip.Trip_date.Year + " " + (trip.Trip_Type == TripType.Control ? "контр." : trip.Trip_Type == TripType.Work ? "раб." : "доп.") + " Проезд:" + trip.Trip_date.ToString("dd.MM.yyyy  HH:mm") + " " + diagramName + ">"
                        ),
                    new XAttribute("pre", xp + 30),
                    new XAttribute("prer", xp + 21),
                    new XAttribute("topr", -kilometer.Start_m - svgLength - 45),
                    new XAttribute("topf", xp + 10),
                    new XAttribute("topx", -kilometer.Start_m - svgLength),
                    new XAttribute("topx1", -kilometer.Start_m - svgLength - 30),
                    new XAttribute("topx2", -kilometer.Start_m - svgLength - 15),
                    new XAttribute("fragment", (kilometer.StationSection != null && kilometer.StationSection.Count > 0 ? "Станция: " + kilometer.StationSection[0].Station : (kilometer.Sector != null ? kilometer.Sector.ToString() : "")) + " Км:" + kilometer.Number),
                    new XAttribute("viewbox", $"-20 {-kilometer.Start_m - svgLength - 50} 830 {svgLength + 105}"),
                    new XAttribute("minY", -kilometer.Start_m),
                    new XAttribute("maxY", -kilometer.Final_m),
                    new XAttribute("minYround", -(kilometer.Start_m - kilometer.Start_m % 100)),

                    RightSideChart(trip.Trip_date, kilometer, kilometer.Track_id, new float[] { 151f, 146f, 152.5f, 155f }),

                    new XElement("xgrid",
                        new XElement("x", MMToPixelChartString(LevelPosition - LevelStep), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "  –30"), new XAttribute("y", MMToPixelChartString(LevelPosition - LevelStep - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(LevelPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black"), new XAttribute("label", "      0"), new XAttribute("y", MMToPixelChartString(LevelPosition - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(LevelPosition + LevelStep), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "    30"), new XAttribute("y", MMToPixelChartString(LevelPosition + LevelStep - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(StraighRighttPosition - StrightStep), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "  –30"), new XAttribute("y", MMToPixelChartString(StraighRighttPosition - StrightStep - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(StraighRighttPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black"), new XAttribute("label", "      0"), new XAttribute("y", MMToPixelChartString(StraighRighttPosition - 1f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(StraighRighttPosition + StrightStep / 10f), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "      3"), new XAttribute("y", MMToPixelChartString(StraighRighttPosition + StrightStep / 10f + 0.2f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(StrightLeftPosition - StrightStep / 10f), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "    –3"), new XAttribute("y", MMToPixelChartString(StrightLeftPosition - StrightStep / 10f - 1f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(StrightLeftPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black"), new XAttribute("label", "      0"), new XAttribute("y", MMToPixelChartString(StrightLeftPosition + 0.2f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(StrightLeftPosition + StrightStep), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "    30"), new XAttribute("y", MMToPixelChartString(StrightLeftPosition + StrightStep - 0.5f)), new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(GaugePosition - 10 * GaugeKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey")),
                        new XElement("x", MMToPixelChartString(GaugePosition - 8 * GaugeKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "1512"), new XAttribute("y", MMToPixelChartString(GaugePosition - 8 * GaugeKoef - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(GaugePosition - 4 * GaugeKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey")),
                        new XElement("x", MMToPixelChartString(GaugePosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black"), new XAttribute("label", "1520"), new XAttribute("y", MMToPixelChartString(GaugePosition - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(GaugePosition + 8 * GaugeKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "1528"), new XAttribute("y", MMToPixelChartString(GaugePosition + 8 * GaugeKoef - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(GaugePosition + 16 * GaugeKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "1536"), new XAttribute("y", MMToPixelChartString(GaugePosition + 16 * GaugeKoef - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(GaugePosition + 22 * GaugeKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "1542"), new XAttribute("y", MMToPixelChartString(GaugePosition + 22 * GaugeKoef - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(GaugePosition + 26 * GaugeKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey")),
                        new XElement("x", MMToPixelChartString(GaugePosition + 28 * GaugeKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "1548"), new XAttribute("y", MMToPixelChartString(GaugePosition + 28 * GaugeKoef - 0.5f)), new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(ProsRightPosition - 10 * ProsKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "  –10"), new XAttribute("y", MMToPixelChartString(ProsRightPosition - 10 * ProsKoef - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(ProsRightPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black"), new XAttribute("label", "      0"), new XAttribute("y", MMToPixelChartString(ProsRightPosition - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(ProsRightPosition + 10 * ProsKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "    10"), new XAttribute("y", MMToPixelChartString(ProsRightPosition + 10 * ProsKoef - 0.5f)), new XAttribute("x", xp + 15)),

                        new XElement("x", MMToPixelChartString(ProsLeftPosition - 10 * ProsKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "  –10"), new XAttribute("y", MMToPixelChartString(ProsLeftPosition - 10 * ProsKoef - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(ProsLeftPosition), new XAttribute("dasharray", "3,3"), new XAttribute("stroke", "black"), new XAttribute("label", "      0"), new XAttribute("y", MMToPixelChartString(ProsLeftPosition - 0.5f)), new XAttribute("x", xp + 15)),
                        new XElement("x", MMToPixelChartString(ProsLeftPosition + 10 * ProsKoef), new XAttribute("dasharray", "0.5,2"), new XAttribute("stroke", "grey"), new XAttribute("label", "    10"), new XAttribute("y", MMToPixelChartString(ProsLeftPosition + 10 * ProsKoef - 0.5f)), new XAttribute("x", xp + 15))
                        ));
                //kil
                var meterCount = (currentKmMeter + 500) > kilometer.meter.Count() ? kilometer.meter.Count() : currentKmMeter + 500;
                var startIndex = currentKmMeter - 250;
                for (int index = currentKmMeter; index < meterCount - 1; index++)
                {
                    try
                    {
                        if (kilometer.Number == 6339)
                        {
                            meterCount = meterCount;
                        }
                        int metre = -kilometer.meter[index + 1];
                        drawdownRight += MMToPixelChartString(kilometer.DrawdownLeft[index] * ProsKoef + ProsRightPosition) + "," + metre + " ";
                        drawdownLeft += MMToPixelChartString(kilometer.DrawdownRight[index] * ProsKoef + ProsLeftPosition) + "," + metre + " ";
                        gauge += MMToPixelChartString((kilometer.Gauge[index] - 1520) * GaugeKoef + GaugePosition) + "," + metre + " ";
                        zeroGauge += MMToPixelChartString((kilometer.fsh0[index] - 1520) * GaugeKoef + GaugePosition) + "," + metre + " ";

                        zeroStraightening += MMToPixelChartString(Math.Abs(kilometer.fZeroStright[index]) * Math.Sign(kilometer.StrightRight[index]) * StrightKoef + StraighRighttPosition) + "," + metre + " ";
                        averageStraighteningRight += MMToPixelChartString(kilometer.StrightAvgTrapezoid[index] * StrightKoef + StraighRighttPosition) + "," + metre + " ";
                        var drh = kilometer.StrightAvgTrapezoid[index] + (kilometer.StrightRight[index] - kilometer.StrightAvgTrapezoid[index]);
                        straighteningRight += MMToPixelChartString(drh * StrightKoef + StraighRighttPosition) + "," + metre + " ";

                        zeroStraighteningLeft += MMToPixelChartString(Math.Abs(kilometer.fZeroStright[index]) * Math.Sign(kilometer.StrightRight[index]) * StrightKoef + StrightLeftPosition) + "," + metre + " ";


                        averageStraighteningLeft += MMToPixelChartString(kilometer.StrightAvgTrapezoid[index] * StrightKoef + StrightLeftPosition) + "," + metre + " ";
                        drh = kilometer.StrightAvgTrapezoid[index] + (kilometer.StrightLeft[index] - kilometer.StrightAvgTrapezoid[index]);
                        straighteningLeft += MMToPixelChartString(drh * StrightKoef + StrightLeftPosition) + "," + metre + " ";


                        level += MMToPixelChartString(kilometer.Level[index] * LevelKoef + LevelPosition) + "," + metre + " ";
                        averageLevel += MMToPixelChartString(kilometer.LevelAvgTrapezoid[index] * LevelKoef + LevelPosition) + "," + metre + " ";
                        zeroLevel += MMToPixelChartString(Math.Abs(kilometer.flvl0[index]) * Math.Sign(kilometer.StrightRight[index]) * LevelKoef + LevelPosition) + "," + metre + " ";
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Рисование линий А4 ошибка " + e.Message);
                    }
                }

                var style = "fill:none;stroke:black;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:1";
                var styleAverage = "fill:none;stroke:black;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:1; stroke-dasharray:0.7 0.6;";
                var styleAvg = "fill:none;stroke:dimgray;vector-effect:non-scaling-stroke;stroke-linejoin:round;stroke-width:0.5; stroke-dasharray:4 2;stroke:dodgerblue";

                addParam.Add(new XElement("polyline", new XAttribute("points", drawdownRight), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", drawdownLeft), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", gauge), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", zeroGauge), new XAttribute("style", style)));

                addParam.Add(new XElement("polyline", new XAttribute("points", zeroStraightening), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", averageStraighteningRight), new XAttribute("style", styleAverage)));
                addParam.Add(new XElement("polyline", new XAttribute("points", straighteningRight), new XAttribute("style", style)));

                addParam.Add(new XElement("polyline", new XAttribute("points", zeroStraighteningLeft), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", averageStraighteningLeft), new XAttribute("style", styleAverage)));
                addParam.Add(new XElement("polyline", new XAttribute("points", straighteningLeft), new XAttribute("style", style)));

                addParam.Add(new XElement("polyline", new XAttribute("points", level), new XAttribute("style", style)));
                addParam.Add(new XElement("polyline", new XAttribute("points", averageLevel), new XAttribute("style", styleAverage)));
                addParam.Add(new XElement("polyline", new XAttribute("points", zeroLevel), new XAttribute("style", style)));


                char separator = CultureInfo.CurrentCulture.NumberFormat.CurrencyDecimalSeparator[0];

                var digElemenets = new XElement("digressions");
                List<int> usedTops = new List<int>();
                List<int> speedmetres = new List<int>();

                var gmeter = kilometer.Start_m.RoundTo10() + 10;


                foreach (var picket in kilometer.Pickets)
                {
                    picket.WriteNotesToReport(
                        kilometer,
                        speedmetres,
                        addParam,
                        digElemenets,
                        ProsRightPosition,
                        ProsLeftPosition,
                        StraighRighttPosition,
                        StrightLeftPosition,
                        GaugePosition,
                        LevelPosition,
                        this,
                        ref fourStepOgrCoun,
                        ref otherfourStepOgrCoun);
                }

                //addParam.Add(new XAttribute("common", kilometer.GetdigressionsCount));

                var GetBedomost = new List<Bedemost> { };
                try
                {
                    // 1степень санау
                    GetBedomost = ((List<Bedemost>)RdStructureRepository.GetBedemost(kilometer.Trip.Id)).Where(o => o.Km == kilometer.Number).ToList();
                }
                catch (Exception e)
                {
                    Console.WriteLine("GetBedomost " + e.Message);
                }
                addParam.Add(
                    new XAttribute("speedlimit",
                    kilometer.CalcMainPoint() + " " +
                    $"Кол.ст.- 1:{(GetBedomost.Any() ? GetBedomost.First().Type1 : 0)}; " + kilometer.GetdigressionsCount +
                    "  Кол.огр.:" + fourStepOgrCoun + "/" + otherfourStepOgrCoun + " Огр: " + kilometer.SpeedLimit + " Пред:- ")
                    );

                addParam.Add(digElemenets);
                report.Add(addParam);

                xdReport.Add(report);

                XslCompiledTransform transform = new XslCompiledTransform();
                transform.Load(XmlReader.Create(new StringReader(template.Xsl)));
                transform.Transform(xdReport.CreateReader(), writer);
            }
            try
            {
                htReport.Save($@"G:\work_shifrovka\{kilometer.Number}_{kilometer.Track_id}.html");

                //htReport.Save($@"\\192.168.1.200\form\{kilometer.Number}_{kilometer.Track_id}.html");
                var svg = htReport.Element("html").Element("body").Element("div").Element("div").Element("svg");
                var svgDoc = SvgDocument.FromSvg<SvgDocument>(svg.ToString());
                svgDoc.Width = 830 * 3;
                svgDoc.Height = (svgLength + 115) * 3;

                if (autoprint)
                {

                    PrintDocument pd = new PrintDocument();
                    //pd.PrinterSettings.PrinterName = "EPSON5EEE22";
                    Console.WriteLine(pd.PrinterSettings.PrinterName);

                    pd.PrintPage += (sender, args) =>
                    {
                        var i = svgDoc.Draw();
                        //i.Save($@"g:\form\{kilometer.Number}_{kilometer.Track_id}.png");
                        pd.DefaultPageSettings.Margins = new Margins(0, 0, 0, 0);
                        pd.OriginAtMargins = false;
                        pd.DefaultPageSettings.Landscape = true;
                        Point p = new Point(0, 0);
                        args.Graphics.DrawImage(i, 0, 0, i.Width / 3, i.Height / 3 - 15);
                    };
                    pd.Print();

                }



            }
            //sl12
            catch (Exception e)
            {
                System.Console.WriteLine("Ошибка при сохранении отчетной формы: " + e.Message);
            }
            finally
            {
                // System.Diagnostics.Process.Start(Path.GetTempPath() + "/report.html");
            }
        }
    }
}


