# Order of Data

The suggestion to "add a field to the table that stores the order" is a good one, but not just 'any' field will do. Suppose the database holds just three items. If we query for Apple | Orange and assign them '1' and '2' for sort order and then query for Orange | Banana and assign _them_ '1' and '2' what have we gained? So what has worked for me is to have a time-based `Priority` field for the sort order.

```
[Table("records"), DebuggerDisplay("{Name}")]
class Record
{
    [PrimaryKey, Browsable(false)]
    public string Guid { get; set; } = System.Guid.NewGuid().ToString();

    public DateTime Created { get; set; } = DateTime.Now;

    [Browsable(false)]
    public DateTime Priority { get; set; } = DateTime.Now;

    [ReadOnly(true)]
    public string Name { get; set; } = string.Empty;
}
```

[![initial][1]][1]

___
For the sake of simplicity, suppose we're using `sqlite-net-pcl` instead of `System.Data.SQLite` so that `DataGridView` can use a straightforward list binding. _(This has no bearing on the actual answer, it just makes it easier to show the code.)_

```
BindingList<Record> Recordset { get; } = new BindingList<Record>();
```

The [Requery] button click handler is:

```

    buttonRequery.Click += async (sender, e) =>
    {
        Recordset.Clear();
        using (var cnx = new SQLiteConnection(SQLitePath))
        {
            foreach (var item in cnx.Query<Record>("SELECT * FROM records ORDER BY Priority"))
            {
                Recordset.Add(item);
            }
        }
    };
```

[![after drag drop][2]][2]

In response, sort the priorities in order and apply them to the items in the new order they exist now before updating the database record.

```
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
                var record = Recordset[dragRow];
                Recordset.Remove(record);
                Recordset.Insert(tgtRow, record);
                dataGridView.Refresh();
                dataGridView.Rows[tgtRow].Selected = true;
            }
        }
    }
    if (dragLabel != null)
    {
        dragLabel.Dispose();
        dragLabel = null;

        // Apply the sorted priorities to the items as they now stand in the list.
        var priorities = Recordset.Select(_ => _.Priority).OrderBy(_=>_).ToArray();
        for(int index = 0; index < priorities.Length; index++)
        {
            Recordset[index].Priority = priorities[index];
        }
        using (var cnx = new SQLiteConnection(SQLitePath))
        {
            cnx.UpdateAll(Recordset);
        }
    }
    dataGridView.CurrentCell = hit.ColumnIndex == -1 || hit.RowIndex == -1 ?
        null :
        dataGridView[hit.ColumnIndex, hit.RowIndex];
};
```
___

**Example**

Suppose we have ability to do an ephemeral sort by clicking on the column header. As a test sequence, we can (ephemeral) sort by `Created`, then (ephemeral) sort on `Name`, before hitting the [Requery] button to retrieve the database items in the actual post-drag-drop order.

```
dataGridView.CellClick += (sender, e) =>
{
    if(e.RowIndex == -1)
    {
        Record[] sorted;
        if (dataGridView.Columns[nameof(Record.Created)].Index == e.ColumnIndex)
        {
            sorted = Recordset.OrderBy(_ => _.Created).ToArray();
        }
        else if (dataGridView.Columns[nameof(Record.Name)].Index == e.ColumnIndex)
        {
            sorted = Recordset.OrderBy(_ => _.Name).ToArray();
        }
        else return;
        Recordset.Clear();
        foreach (var item in sorted)
        {
            Recordset.Add(item);
        }
    }
};
```

[![test sequence][3]][3]
___

It follows that you can insert `Grape` between Apple and Banana by setting its priority to ((Apple.Priority) + (Banana.Priority)) / 2. 

You get the idea.


  [1]: https://i.stack.imgur.com/upAoM.png
  [2]: https://i.stack.imgur.com/1fpcy.png
  [3]: https://i.stack.imgur.com/UOc2b.png