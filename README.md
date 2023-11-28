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

[Placeholder]

