namespace ALARm.Core
{
    public class StationSection : MainTrackObject
    {
        //public string Station { get; set; }
        //public string Dir { get; set; }
        //public string mark { get; set; }
        //public string point { get; set; }
        //public string side { get; set; }
        //public string legnht { get; set; }
        //public string id { get; set; }
        //public string point_id { get; set; }
        //public string side_id { get; set; }
        //public string mark_id { get; set; }
        //public string dir_id { get; set; }
        //public string period_id { get; set; }

        
        //public string km { get; set; }
        //public string meter { get; set; }

        //public string num { get; set; }
        //public string olddirection { get; set; }

        public long Nod_Id { get; set; }
        public string Road { get; set; }
        public string Nod { get; set; }
        public long Station_Id { get; set; }
        public string Station { get; set; }
        public string PrevStation { get; set; }
        public string NextStation { get; set; }
        public int Side_Id { get; set; }
        public string Side { get; set; }
        public int Axis_Km { get; set; }
        public int Axis_M { get; set; }

        public long Point_Id { get; set; }
        public string Point { get; set; }
        public double RealCoordinate()
        {
            return Axis_Km + Axis_M / 1000.0;
        }
    }
}