using System;
using ALARm.Core;
using ALARm.DataAccess;
using Autofac;
using System.IO;
using System.Collections.Generic;

namespace ALARm.Services
{
    public class ExportImportService
    {
        static readonly IContainer Container;
        static ExportImportService()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<ExportImportRepository>().As<IExportImportRepository>();
            Container = builder.Build();
        }

        public static string ExportQueryReturnString(string text)
        {
            return Container.Resolve<IExportImportRepository>().ExportQueryReturnString(text);
        }

        public static bool Execute(string text)
        {
            return Container.Resolve<IExportImportRepository>().Execute(text);
        }

        public static long ImportQueryReturnLong(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnLong(text);
        }

        public static double ImportQueryReturnDouble(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnDouble(text);
        }

        public static List<long> ImportQueryReturnListLong(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnListLong(text);
        }
        public static List<string> ImportQueryReturnListString(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnListString(text);
        }

        public static List<ExportList> ExportList(string text)
        {
            return Container.Resolve<IExportImportRepository>().ExportList(text);
        }
        public static List<ExportListPeriod> ExportListPeriod(string text)
        {
            return Container.Resolve<IExportImportRepository>().ExportListPeriod(text);
        }

        public static List<ExportListPodgr> ExportListPodgr(string text)
        {
            return Container.Resolve<IExportImportRepository>().ExportListPodgr(text);
        }
        public static List<ImportListCurveID> ImportQueryReturnListImportedCurve(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnListImportedCurve(text);
        }
        
        public static List<ImportListEkasui> ImportQueryReturnListImportedCurvedelete(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnListImportedCurvedelete(text);
        }

        public static List<ImportListElevationsix> ImportQueryReturnListImportedElevation(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnListImportedElevation(text);
        }

        public static List<ImportListDirTrackID> ImportQueryReturnListDirTrack(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnListDirTrack(text);
        }

        public static List<ImportListSTRTrackID> ImportQueryReturnListSTRTrack(string text)
        {
            return Container.Resolve<IExportImportRepository>().ImportQueryReturnListSTRTrack(text);
        }

    }
}
