using System;
using System.Collections.Generic;
using System.IO;

namespace ALARm.Core
{
    public interface IExportImportRepository
    {
        string ExportQueryReturnString(string text);
        bool Execute(string text);
        long ImportQueryReturnLong(string text);
        List<long> ImportQueryReturnListLong(string text);
        List<string> ImportQueryReturnListString(string text);
        List<ImportListCurveID> ImportQueryReturnListImportedCurve(string text);
        
        List<ExportList> ExportList(string text);
        List<ExportListPodgr> ExportListPodgr(string text);
        List<ExportListPeriod> ExportListPeriod(string text);


        List<ImportListElevationsix> ImportQueryReturnListImportedElevation(string text);
        List<ImportListEkasui> ImportQueryReturnListImportedCurvedelete(string text);
        List<ImportListDirTrackID> ImportQueryReturnListDirTrack(string text);
        List<ImportListSTRTrackID> ImportQueryReturnListSTRTrack(string text);

        double ImportQueryReturnDouble(string text);
    }
}
