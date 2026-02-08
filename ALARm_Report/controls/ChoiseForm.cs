using ALARm;
using ALARm.Core;
using ALARm.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ALARm_Report.controls
{
    public partial class ChoiseForm : MetroFramework.Forms.MetroForm
    {
        public class TrackChoice
        {
            public long TrackId { get; set; }
            public string DirectionName { get; set; }
            public string TrackName { get; set; }   // что отображается в колонке "Путь" (например "2" или "Главный")
            public long DirectionId { get; set; }   // если есть в AdmTrack (может быть 0 если нет)
        }
        public List<TrackChoice> SelectedTracks { get; private set; } = new List<TrackChoice>();
        private static string GetStringSafe(object obj, string propName)
        {
            try
            {
                var p = obj.GetType().GetProperty(propName);
                if (p == null) return null;
                return p.GetValue(obj)?.ToString();
            }
            catch { return null; }
        }

        private static long? GetLongSafe(object obj, string propName)
        {
            try
            {
                var p = obj.GetType().GetProperty(propName);
                if (p == null) return null;
                var v = p.GetValue(obj);
                if (v == null) return null;
                if (v is long l) return l;
                if (long.TryParse(v.ToString(), out var x)) return x;
                return null;
            }
            catch { return null; }
        }


        public DialogResult dialogResult = DialogResult.Cancel;
        public double wear = -1;
        //public List<AdmTrack> admTracks = new List<AdmTrack>();
        public List<long> admTracksIDs = new List<long>();
        public List<long> admDirectionIDs = new List<long>();
        private int Mode = 0;

        internal void SetTripsDataSource(object distanceId, ReportPeriod period)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Вид формы
        /// </summary>
        /// <param name="mode"> Виды формы: 0 - проезд и пути, 1 - износ.</param>
        public ChoiseForm(int mode)
        {
            InitializeComponent();
            switch (mode)
            {
                case 0:
                    this.Height = 237;
                    Mode = mode;
                    break;
                case 1:
                    metroPanel1.Visible = false;
                    cbTrips.Visible = false;
                    metroPanel4.Visible = true;
                    this.Height = 168;
                    Mode = mode;
                    break;
                default:
                    Mode = -1;
                    Close();
                    return;
            }
        }

        public void SetTripsDataSource(long distanceId, ReportPeriod period,long trackId = -1 )
        {
            tripsBindingSource.DataSource = RdStructureService.GetTripsOnDistance(distanceId, period);

            admTrackBindingSource.DataSource = RdStructureService.GetTracks(distanceId, period,trackId);
           
            int height = 15 + admTrackBindingSource.Count * 22 + 5 * 2;
            metroPanel1.Height = height;
            this.Height = 185 + height;
        }

        private void mbAccept_Click(object sender, EventArgs e)
        {
            switch (Mode)
            {
                case 0:
{
                admTracksIDs.Clear();
                admDirectionIDs.Clear();
                SelectedTracks.Clear();

                var list = admTrackBindingSource.DataSource as List<AdmTrack>;
                if (list == null)
                {
                    MetroFramework.MetroMessageBox.Show(this, "Нет данных по путям", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                }

                var selected = list.Where(a => a.Accept).ToList();

                if (selected.Count < 1)
                {
                    MetroFramework.MetroMessageBox.Show(this, "Выберите хотя бы один путь", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                }

                // Старое поведение оставляем (если где-то ещё используется)
                selected.ForEach(a => admTracksIDs.Add(a.Id));

                // Новое: сохраняем направление + путь (как в гриде)
                foreach (var a in selected)
                {
                    // ВНИМАНИЕ: имена свойств могут отличаться (DirectionName / TrackName и т.д.)
                    // Подгони 2 строки ниже под реальные свойства AdmTrack (как у тебя в DataGrid столбцы подписаны)

                    var dirName = GetStringSafe(a, "DirectionName")
                                  ?? GetStringSafe(a, "Direction")
                                  ?? "";

                    var trackName = GetStringSafe(a, "Code")
                                    ?? GetStringSafe(a, "TrackName")
                                    ?? GetStringSafe(a, "Track")
                                    ?? "";

                    var dirId = GetLongSafe(a, "Parent_id")
                                ?? GetLongSafe(a, "Parent_Id")
                                ?? GetLongSafe(a, "Direction_id")
                                ?? 0;

                    SelectedTracks.Add(new TrackChoice
                    {
                        TrackId = a.Id,
                        DirectionName = dirName,
                        TrackName = trackName,
                        DirectionId = dirId
                    });

                    if (dirId > 0) admDirectionIDs.Add(dirId);
                }

                dialogResult = DialogResult.OK;
                Close();
                return;
            }
                case 1:
                    if (!double.TryParse(tbWear.Text, out wear))
                    {
                        MetroFramework.MetroMessageBox.Show(this, "ErrNo5", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }

                    dialogResult = DialogResult.OK;
                    Close();
                    return;
            }
        }

        private void mbCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void cbTrips_SelectedValueChanged(object sender, EventArgs e)
        {
            if (tripsBindingSource.Count > 0 && !cbTrips.SelectedValue.Equals(null))
            {
                admTrackBindingSource.DataSource = RdStructureService.GetTracksOnTrip((long)cbTrips.SelectedValue);
                int height = 15 + admTrackBindingSource.Count * 22 + 5 * 2;
                metroPanel1.Height = height;
                this.Height = 185 + height;
            }
        }

        private void metroGrid1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
