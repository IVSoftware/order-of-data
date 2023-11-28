using System.ComponentModel;
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
            dataGridView.DataSource = Recordset;
            dataGridView.Columns[nameof(Record.Name)].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            using (var cnx = new SQLiteConnection(SQLitePath))
            {
                foreach (var record in cnx.Query<Record>("SELECT * FROM records"))
                {
                    Recordset.Add(record);
                }
            }
        }
        BindingList<Record> Recordset { get; } = new BindingList<Record>();
    }
}

[Table("records")]
class Record
{
    [PrimaryKey, Browsable(false)]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    [Browsable(false)]
    public long Priority { get; set; } = DateTime.UtcNow.Ticks;

    public string Name { get; set; } = string.Empty;
}
