using ALARm.Core;
using ALARm.Core.Report;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace ALARm_Report.controls
{
    public partial class FilterForm : Form
    {
        public ReportPeriod ReportPeriod { get; set; }
        public string ReportClasssName { get; set; }
        public List<Filter> Filters { get; set; }
        private string previousValue { get; set; } = "";

        public FilterForm()
        {
            InitializeComponent();

            // Важно: подписки, если не сделано в дизайнере
            dataGridView1.CellValidating += DataGridView1_CellValidating;
            dataGridView1.EditingControlShowing += dataGridView1_EditingControlShowing;
            this.Shown += FilterForm_Shown;
        }

        public void SetDataSource(List<Filter> filter)
        {
            Filters = filter;
            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = Filters;

            dataGridView1.Columns.Clear();
            dataGridView1.Columns.Add("Name", "Имя");
            dataGridView1.Columns.Add("Value", "Значение");

            dataGridView1.Columns[0].DataPropertyName = "Name";
            dataGridView1.Columns[0].Width = 250;
            dataGridView1.Columns[0].DefaultCellStyle.BackColor = System.Drawing.Color.LightBlue;
            dataGridView1.Columns[0].ReadOnly = true;

            dataGridView1.Columns[1].DataPropertyName = "Value";

            dataGridView1.Columns[0].Resizable = DataGridViewTriState.False;
            dataGridView1.Columns[1].Resizable = DataGridViewTriState.False;
        }

        private void DataGridView1_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            // Валидация нужна только если редактируем обычную текстовую ячейку.
            // Для ComboBox это не обязательно, но пусть будет мягко.
            if (e.ColumnIndex != 1) return;

            var cell = dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex];
            if (cell is DataGridViewComboBoxCell) return;

            // Если вдруг кто-то вводит дату текстом — не даём сохранить мусор
            if (cell?.OwningRow?.DataBoundItem is DateFilter)
            {
                var ru = CultureInfo.GetCultureInfo("ru-RU");
                var raw = (e.FormattedValue?.ToString() ?? "").Trim();

                string[] dateFormats =
                {
                    "dd.MM.yyyy",
                    "d.M.yyyy",
                    "dd.MM.yyyy HH:mm:ss",
                    "d.M.yyyy HH:mm:ss",
                    "dd.MM.yyyy H:mm:ss",
                    "d.M.yyyy H:mm:ss"
                };

                if (!DateTime.TryParseExact(raw, dateFormats, ru, DateTimeStyles.AllowWhiteSpaces, out _))
                {
                    MessageBox.Show($"Некорректная дата: '{raw}'. Формат: dd.MM.yyyy");
                    e.Cancel = true;
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            var filters = (List<Filter>)dataGridView1.DataSource;
            foreach (var f in filters)
            {
                if (f.Value == null)
                {
                    MessageBox.Show("Укажите все входные параметры для формирования отчета");
                    return;
                }
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void FilterForm_Shown(object sender, EventArgs e)
        {
            var filters = dataGridView1.DataSource as List<Filter>;
            if (filters == null) return;

            var ru = CultureInfo.GetCultureInfo("ru-RU");
            string[] dateFormats =
            {
                "dd.MM.yyyy",
                "d.M.yyyy",
                "dd.MM.yyyy HH:mm:ss",
                "d.M.yyyy HH:mm:ss",
                "dd.MM.yyyy H:mm:ss",
                "d.M.yyyy H:mm:ss"
            };

            // ВАЖНО: идём по индексам, а не через filters.IndexOf(f)
            for (int rowIndex = 0; rowIndex < filters.Count; rowIndex++)
            {
                var f = filters[rowIndex];

                // -------------------- DateFilter (теперь на ВЕСЬ ГОД) --------------------
                if (f is DateFilter df)
                {
                    var raw = (df.Value ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        MessageBox.Show("DateFilter.Value пустой");
                        continue;
                    }

                    // Парсим независимо от региональных настроек Windows
                    if (!DateTime.TryParseExact(raw, dateFormats, ru, DateTimeStyles.AllowWhiteSpaces, out var dt))
                    {
                        MessageBox.Show($"Некорректная дата: '{raw}'. Ожидается dd.MM.yyyy");
                        continue;
                    }

                    // Нормализуем, чтобы точно совпадало со списком
                    df.Value = dt.ToString("dd.MM.yyyy", ru);
                    raw = df.Value;

                    // Список дат НА ВЕСЬ ГОД
                    var ds = new List<string>(366);
                    var current = new DateTime(dt.Year, 1, 1);
                    var end = new DateTime(dt.Year, 12, 31);

                    while (current <= end)
                    {
                        ds.Add(current.ToString("dd.MM.yyyy", ru));
                        current = current.AddDays(1);
                    }

                    var cb = new DataGridViewComboBoxCell
                    {
                        DataSource = ds,
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                    };

                    dataGridView1.Rows[rowIndex].Cells[1] = cb;

                    // Ставим значение только из списка, иначе будет "value is not valid"
                    cb.Value = ds.Contains(raw) ? raw : ds[0];

                    // Синхронизируем обратно в модель, чтобы биндинг не подсовывал другое
                    df.Value = cb.Value?.ToString();

                    continue;
                }

                // -------------------- TripTypeFilter --------------------
                if (f is TripTypeFilter tf)
                {
                    var ds = new List<string> { "", "контрольная", "рабочая", "дополнительная" };

                    var cb = new DataGridViewComboBoxCell
                    {
                        DataSource = ds,
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                    };

                    dataGridView1.Rows[rowIndex].Cells[1] = cb;

                    var raw = (tf.Value ?? "").Trim();
                    cb.Value = ds.Contains(raw) ? raw : ds[0];
                    tf.Value = cb.Value?.ToString();

                    continue;
                }

                // -------------------- PU32TypeFilter --------------------
                if (f is PU32TypeFilter pf)
                {
                    var ds = new List<string> { "", "по ПЧ", "по направлению", "cравнительная" };

                    var cb = new DataGridViewComboBoxCell
                    {
                        DataSource = ds,
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                    };

                    dataGridView1.Rows[rowIndex].Cells[1] = cb;

                    var raw = (pf.Value ?? "").Trim();
                    cb.Value = ds.Contains(raw) ? raw : ds[0];
                    pf.Value = cb.Value?.ToString();

                    continue;
                }

                // -------------------- PeriodFilter --------------------
                if (f is PeriodFilter pef)
                {
                    var currentYear = DateTime.Now.Year;
                    var ds = new List<string>();

                    for (int y = currentYear - 1; y <= currentYear + 1; y++)
                        for (int m = 1; m <= 12; m++)
                            ds.Add(new ReportPeriod { PeriodMonth = m, PeriodYear = y }.ToString());

                    var cb = new DataGridViewComboBoxCell
                    {
                        DataSource = ds,
                        DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                    };

                    dataGridView1.Rows[rowIndex].Cells[1] = cb;

                    var raw = (pef.Value ?? "").Trim();
                    if (ds.Count > 0)
                    {
                        cb.Value = ds.Contains(raw) ? raw : ds[0];
                        pef.Value = cb.Value?.ToString();
                    }
                }
            }
        }

        private void dataGridView1_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView1.CurrentCell.ColumnIndex == 1 && e.Control is ComboBox && dataGridView1.CurrentCell.RowIndex == 3)
            {
                ComboBox comboBox = e.Control as ComboBox;
                comboBox.SelectedIndexChanged -= LastColumnComboSelectionChanged;
                comboBox.SelectedIndexChanged += LastColumnComboSelectionChanged;
            }
        }

        private void LastColumnComboSelectionChanged(object sender, EventArgs e)
        {
            var value = (sender as ComboBox)?.SelectedValue;
            var rowIndex = dataGridView1.CurrentRow?.Index ?? -1;
            if (rowIndex != 3) return;

            if (value != null && !value.ToString().Equals(""))
            {
                var tripType = dataGridView1.Rows[2].Cells[1].Value;

                if (ReportClasssName.Equals("PU32")
                    && rowIndex == 3
                    && !value.ToString().Equals("cравнительная")
                    && !previousValue.Equals(value))
                {
                    if (Filters.Count > 4)
                        Filters.RemoveRange(4, Filters.Count - 4);

                    Filters.Add(new DateFilter() { Name = "Дата проверки с:", Value = ReportPeriod.StartDate.ToString("dd.MM.yyyy") });
                    Filters.Add(new DateFilter() { Name = "                         по:", Value = ReportPeriod.FinishDate.ToString("dd.MM.yyyy") });

                    dataGridView1.DataSource = null;
                    dataGridView1.DataSource = Filters;

                    FilterForm_Shown(sender, e);

                    (dataGridView1.Rows[3].Cells[1] as DataGridViewComboBoxCell).Value = value;
                    dataGridView1.Rows[2].Cells[1].Value = tripType;
                    previousValue = value.ToString();
                }

                if (ReportClasssName.Equals("PU32")
                    && rowIndex == 3
                    && value.ToString().Equals("cравнительная")
                    && !previousValue.Equals(value))
                {
                    if (Filters.Count > 4)
                        Filters.RemoveRange(4, Filters.Count - 4);

                    Filters.Add(new PeriodFilter() { Name = "Сравниваемый период" });
                    Filters.Add(new TripTypeFilter() { Name = "Тип сравниваемой поездки" });

                    dataGridView1.DataSource = null;
                    dataGridView1.DataSource = Filters;

                    FilterForm_Shown(sender, e);

                    (dataGridView1.Rows[3].Cells[1] as DataGridViewComboBoxCell).Value = value;
                    dataGridView1.Rows[2].Cells[1].Value = tripType;
                    previousValue = value.ToString();
                }
            }
        }
    }
}
