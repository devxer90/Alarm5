using System;

namespace ALARm.Core
{
    public class ImportListDirTrackID
    {
        public Int64 NewTrackID { get; set; }
        public string OldTrackID { get; set; }
        public Int64 NewDirectionID { get; set; }
        public int OldDirectionID { get; set; }
        public Int64 CurveID { get; set; }
        public string Code { get; set; }
        public int OldDirection { get; set; }
        public string Stationname { get; set; }

    }
    public class ImportListSTRTrackID
    {
        public Int64 NewTrackID { get; set; }
        public string OldTrackID { get; set; }
        public Int64 NewDirectionID { get; set; }
        public int OldDirectionID { get; set; }
        public Int64 CurveID { get; set; }
        public string Km { get; set; }
        public string M { get; set; }
        public string Up_nom { get; set; }
        public string St_kod { get; set; }
        public string Put_nom { get; set; }
        public string Type { get; set; }
        public string Otb { get; set; }
        public string PosH { get; set; }
        public string Nom { get; set; }
        public Int64 NewStationID { get; set; }

    }

    public class ImportListStationID
    {
        public string Station { get; set; }
        public string Code { get; set; }
        public Int64 NewStationID { get; set; }
        public Int64 NewDirectionID { get; set; }
        public int OldStationID { get; set; }
        public string OldTrackId { get; set; }
        public int OldDirection { get; set; }


    }
    public class ImportListStationKM
    {
        public string Station { get; set; }
        public string Code { get; set; }
        public Int64 NewStationID { get; set; }
        public int OldStationID { get; set; }
        public string OldTrackId { get; set; }
        public int OldDirectionID { get; set; }
        public int KM { get; set; }

        public int M { get; set; }


    }
    public class ExportListPodgr
    {
        public string Assetnum { get; set; }
        public Int64 NewTrackID { get; set; }
        public string OldTrackID { get; set; }
        public Int64 NewDirectionID { get; set; }
        public int OldDirectionID { get; set; }
        public string Type { get; set; }
        public string CurveID { get; set; }
        public string CurveIDs { get; set; }
        //public string OldTrackID { get; set; }
        public string StartKM { get; set; }
        public string StartM { get; set; }
        public string FinalKM { get; set; }
        public string FinalM { get; set; }
        public string SideID { get; set; }
        public string Radius { get; set; }
        public string Length { get; set; }
        public string Urov { get; set; }
        public int RIHT { get; set; }
        public string Shablon { get; set; }
        public string IZN { get; set; }
        public string SHAB { get; set; }
        public int napr { get; set; }
        public string Km { get; set; }
        public string M { get; set; }

        public Int64 DistanceID { get; set; }
        public string Distance { get; set; }

        public Int64 Ekasui_id { get; set; }
        public Int64 ID { get; set; }

        //public int Num { get; set; }
        //public string PCH { get; set; }
        public string PDB { get; set; }
        public Int64 PCHUID { get; set; }
        public string PCHU { get; set; }
        public Int64 PDID { get; set; }
        public string PD { get; set; }
        public Int64 PDBID { get; set; }




        public string FIO { get; set; }
        public int Start { get; set; }
        public int Final { get; set; }
    }
    public class ExportListPeriod
    {
        public int Id { get; set; }
        public int Adm_track_id { get; set; }
        public DateTime Start_date { get; set; }
        public DateTime Final_date { get; set; }
        public DateTime Change_date { get; set; }
        public string Comment { get; set; }
        public int Mto_type { get; set; }
    }
    public class ExportList
    {
        public string Assetnum { get; set; }
        public Int64 NewTrackID { get; set; }
        public string OldTrackID { get; set; }
        public Int64 NewDirectionID { get; set; }
        public int OldDirectionID { get; set; }
        public int Type { get; set; }
        public string CurveID { get; set; }
        public string CurveIDs { get; set; }
        //public string OldTrackID { get; set; }
        public string StartKM { get; set; }
        public string StartM { get; set; }
        public string FinalKM { get; set; }
        public string FinalM { get; set; }
        public string SideID { get; set; }
        public string Radius { get; set; }
        public string Length { get; set; }
        public int LengthLength { get; set; }
        public string Urov { get; set; }
        public int RIHT { get; set; }
        public string Shablon { get; set; }
        public string IZN { get; set; }
        public string SHAB { get; set; }
        public int napr { get; set; }
        public string Km { get; set; }
        public string M { get; set; }

        public Int64 DistanceID { get; set; }
        public string Distance { get; set; }

        public Int64 Ekasui_id { get; set; }
        public Int64 ID { get; set; }

        //public int Num { get; set; }
        //public string PCH { get; set; }
        public string PDB { get; set; }
        public Int64 PCHUID { get; set; }
        public string PCHU { get; set; }
        public Int64 PDID { get; set; }
        public string PD { get; set; }
        public Int64 PDBID { get; set; }




        public string FIO { get; set; }
        public int Start { get; set; }
        public int Final { get; set; }


    }
    public class ImportListPARK
    {
        public string ParkName { get; set; }
        public int NOMERPARKA { get; set; }
        public Int64 NewStationID { get; set; }
        public int OldStationID { get; set; }
        public string OldTrackId { get; set; }
        public int OldDirectionID { get; set; }
        public int KM { get; set; }

        public int M { get; set; }




    }
    public class ImportListEkasui
    {
        public string Length { get; set; }

        public string Assetnum { get; set; }
        public Int64 NewTrackID { get; set; }
        public string OldTrackID { get; set; }
        public Int64 NewDirectionID { get; set; }
        public int OldDirectionID { get; set; }
        public int Type { get; set; }
        public string CurveID { get; set; }
        public string CurveIDs { get; set; }
        //public string OldTrackID { get; set; }
        public string StartKM { get; set; }
        public string StartM { get; set; }
        public string FinalKM { get; set; }
        public string FinalM { get; set; }
        public string SideID { get; set; }
        public string Radius { get; set; }

        public string Urov { get; set; }
        public int RIHT { get; set; }
        public string Shablon { get; set; }
        public string IZN { get; set; }
        public string SHAB { get; set; }
        public int napr { get; set; }
        public string Km { get; set; }
        public string M { get; set; }

        public string Length_1 { get; set; }

        public string Assetnum_1 { get; set; }
        public Int64 NewTrackID_1 { get; set; }
        public string OldTrackID_1 { get; set; }
        public Int64 NewDirectionID_1 { get; set; }
        public int OldDirectionID_1 { get; set; }
        public int Type_1 { get; set; }
        public string CurveID_1 { get; set; }
        public string CurveIDs_1 { get; set; }
        //public string OldTrackID { get; set; }
        public string StartKM_1 { get; set; }
        public string StartM_1 { get; set; }
        public string FinalKM_1 { get; set; }
        public string FinalM_1 { get; set; }
        public string SideID_1 { get; set; }
        public string Radius_1 { get; set; }

        public string Urov_1 { get; set; }
        public int RIHT_1 { get; set; }
        public string Shablon_1 { get; set; }
        public string IZN_1 { get; set; }
        public string SHAB_1 { get; set; }
        public int napr_1 { get; set; }
        public string Km_1 { get; set; }
        public string M_1 { get; set; }




    }

    public class ImportListCurveID
    {
        public string Length { get; set; }

        public string Assetnum { get; set; }
        public Int64 NewTrackID { get; set; }
        public string OldTrackID { get; set; }
        public Int64 NewDirectionID { get; set; }
        public int OldDirectionID { get; set; }
        public int Type { get; set; }
        public Int64 CurveID { get; set; }
        public string CurveIDs { get; set; }
        //public string OldTrackID { get; set; }
        public string StartKM { get; set; }
        public string StartM { get; set; }
        public string FinalKM { get; set; }
        public string FinalM { get; set; }
        public string SideID { get; set; }
        public string Radius { get; set; }
        
        public string Urov { get; set; }
        public int RIHT { get; set; }
        public string Shablon { get; set; }
        public string IZN { get; set; }
        public string SHAB { get; set; }
        public int napr { get; set; }
        public string Km { get; set; }
        public string M { get; set; }


    }
    public class ImportListElevationsix
    {
        public string Assetnum { get; set; }
        public Int64 NewTrackID { get; set; }
        public string Put_nom { get; set; }
        public Int64 NewDirectionID { get; set; }
        public int Up_nom { get; set; }
        public int Type { get; set; }
        public Int64 CurveID { get; set; }
        //public string OldTrackID { get; set; }
        public string StartKM { get; set; }
        public string StartM { get; set; }
        public string FinalKM { get; set; }
        public string FinalM { get; set; }
        public string SideID { get; set; }
        public string start_km { get; set; }
        public string start_m { get; set; }
        public string final_m { get; set; }
        public string final_km { get; set; }
        public string shab { get; set; }
        public string Urov { get; set; }
        public int RIHT { get; set; }
        public string Shablon { get; set; }
        public string IZN { get; set; }
        public string SHAB { get; set; }
        public int napr { get; set; }
        public string Km { get; set; }
        public string M { get; set; }


    }

    public class ImportListStCurve
    {
        public string OldTrackID { get; set; }
        public string StartKM { get; set; }
        public string StartM { get; set; }
        public string FinalKM { get; set; }
        public string FinalM { get; set; }
        public int T1 { get; set; }
        public int T2 { get; set; }
        public string Radius { get; set; }
        public string Wear { get; set; }
        public string OldDirectionID { get; set; }

        public string Width { get; set; }
        public long CurveID { get; set; }
        public string SideID { get; set; }


    }

    public class ImportListElCurve
    {
        public string KM { get; set; }
        public string M { get; set; }
        public string OldTrackID { get; set; }
        public string OldDirectionID { get; set; }

        public string StartKM { get; set; }
        public string StartM { get; set; }
        public string FinalKM { get; set; }
        public string FinalM { get; set; }
        public double Lvl { get; set; }
        public int T1 { get; set; }
        public int T2 { get; set; }
        public long CurveID { get; set; }
        public string SideID { get; set; }


    }

    public class ImportListPDBID
    {
        public string OldTrackID { get; set; }
        public Int64 DistanceID { get; set; }
        public string Distance { get; set; }

        public Int64 Ekasui_id { get; set; }
        public Int64 ID { get; set; }
        public string Type { get; set; }
        //public int Num { get; set; }
        //public string PCH { get; set; }
        public string PDB { get; set; }
        public Int64 PCHUID { get; set; }
        public string PCHU { get; set; }
        public Int64 PDID { get; set; }
        public string PD { get; set; }
        public Int64 PDBID { get; set; }




        public string FIO { get; set; }
        public int Start { get; set; }
        public int Final { get; set; }
    }
    public class ImportListPCH
    {
        public string OldTrackID { get; set; }
        public Int64 DistanceID { get; set; }

        public Int64 Ekasui_id { get; set; }
        public Int64 ID { get; set; }

        public string Distance { get; set; }
        public Int64 PCHUID { get; set; }
        public string PCHU { get; set; }
        public Int64 PDID { get; set; }
        public string PD { get; set; }
        public Int64 PDBID { get; set; }

        public string Type { get; set; }
        //public int Num { get; set; }
        //public string PCH { get; set; }
        public string PDB { get; set; }
        public string FIO { get; set; }
        public int Start { get; set; }
        public int Final { get; set; }
    }
    public class ImportListPCHU
    {
        public string OldTrackID { get; set; }
        public Int64 DistanceID { get; set; }

        public Int64 Ekasui_id { get; set; }
        public Int64 ID { get; set; }

        public string Distance { get; set; }
        public Int64 PCHUID { get; set; }
        public string PCHU { get; set; }
        public Int64 PDID { get; set; }
        public string PD { get; set; }
        public Int64 PDBID { get; set; }

        public string Type { get; set; }
        //public int Num { get; set; }
        //public string PCH { get; set; }
        public string PDB { get; set; }
        public string FIO { get; set; }
        public int Start { get; set; }
        public int Final { get; set; }
    }

    public class ImportListStationSection
    {
        public string OldTrackID { get; set; }
        public string OldStationID { get; set; }
        public string OldDirectionID { get; set; }
        public string OldParkID { get; set; }
        
        public string Point_id { get; set; }
        public int Start_pos { get; set; }
        public int Axis_pos { get; set; }
        public int Final_pos { get; set; }
    }

    public class ImportListId
    {
        public long OldId { get; set; }
        public long NewId { get; set; }
    }
}



