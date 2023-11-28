using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using SQLite;

namespace order_of_data
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            if(!File.Exists(SQLitePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SQLitePath));
                using(var cnx = new SQLiteConnection(SQLitePath))
                {
                    cnx.CreateTable<Record>();
                    cnx.InsertAll(new Record[]
                    {
                        new Record{Name = "Apple"},
                        new Record{Name = "Orange"},
                        new Record{Name = "Banana"},
                    });
                }
                Process.Start("explorer.exe", Path.GetDirectoryName(SQLitePath));
            }
        }
        string SQLitePath { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Assembly.GetEntryAssembly().GetName().Name,
            "MockDatabase.db"
        );
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            dataGridView.AllowUserToAddRows = false;
            dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            using (var cnx = new SQLiteConnection(SQLitePath))
            {
                DT = cnx.Query<Record>("SELECT * FROM records").ToDataTable();
            }
            dataGridView.DataSource = DT;
            dataGridView.Columns[nameof(Record.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView.Columns[nameof(Record.Guid)].Visible = false;
            dataGridView.Columns[nameof(Record.Priority)].Visible = false;
            BeginInvoke(() =>
            {
                dataGridView.ClearSelection();
                dataGridView.Refresh();
            });

            dataGridView.CellMouseDown += (sender, e) =>
            {
                if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
                dragRow = e.RowIndex;
                if (dragLabel == null) dragLabel = new Label();
                dragLabel.Text = dataGridView[e.ColumnIndex, e.RowIndex].Value.ToString();
                dragLabel.Parent = dataGridView;
                dragLabel.Location = e.Location;
            };
            dataGridView.MouseMove += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left && dragLabel != null)
                {
                    dragLabel.Location = e.Location;
                    dataGridView.ClearSelection();
                }
            };
            dataGridView.MouseUp += (sender, e) =>
            {
                var hit = dataGridView.HitTest(e.X, e.Y);
                int dropRow = -1;
                if (hit.Type != DataGridViewHitTestType.None)
                {
                    dropRow = hit.RowIndex;
                    if (dragRow >= 0)
                    {
                        int tgtRow = dropRow + (dragRow > dropRow ? 1 : 0);
                        if (tgtRow != dragRow)
                        {
                            DataRow dtRow = DT.Rows[dragRow];
                            DataRow newRow = DT.NewRow();
                            newRow.ItemArray = DT.Rows[dragRow].ItemArray;

                            DT.Rows.Remove(dtRow);
                            DT.Rows.InsertAt(newRow, tgtRow);
                            dataGridView.Refresh();
                            dataGridView.Rows[tgtRow].Selected = true;
                        }
                    }
                }
                if (dragLabel != null)
                {
                    dragLabel.Dispose();
                    dragLabel = null;
                }
                dataGridView.CurrentCell = hit.ColumnIndex == -1 || hit.RowIndex == -1 ?
                    null :
                    dataGridView[hit.ColumnIndex, hit.RowIndex];
                foreach (var item in DT.Rows)
                {

                }
            };
        }
        BindingList<Record> Recordset { get; } = new BindingList<Record>();
        Record[] _selectedRecords = new Record[0];
        int dragRow = -1;
        Label dragLabel = null;
        DataTable DT { get; set; }
    }
}

[Table("records")]
class Record
{
    [PrimaryKey, Browsable(false)]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    [Browsable(false)]
    public long Priority { get; set; } = DateTime.UtcNow.Ticks;

    [ReadOnly(true)]
    public string Name { get; set; } = string.Empty;
}

static partial class Extensions
{
    // https://stackoverflow.com/a/18100872/5438626
    public static DataTable ToDataTable<T>(this List<T> items)
    {
        DataTable dataTable = new DataTable(typeof(T).Name);
        PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (PropertyInfo prop in Props)
        {
            var type = 
                (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>) ? 
                Nullable.GetUnderlyingType(prop.PropertyType) :
                prop.PropertyType);
            dataTable.Columns.Add(prop.Name, type);
        }
        foreach (T item in items)
        {
            var values = new object[Props.Length];
            for (int i = 0; i < Props.Length; i++)
            {
                values[i] = Props[i].GetValue(item, null);
            }
            dataTable.Rows.Add(values);
        }
        return dataTable;
    }
}
