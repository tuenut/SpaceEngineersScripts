List<string> COMPONENTS = new List<string> { "Motor", "Steel Plate" };
string LCD_NAMES_PANEL = "lcd.inventory_items_names";
float FONT_SIZE = 0.5f;
string FONT_FAMILY = "Monospace";

int counter = 0;

Dictionary<string, int> items_in_stock;

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save()
{

}

public void Main(string argument, UpdateType updateSource)
{
    Echo(counter.ToString());

    SetupLCD();
    items_in_stock = new Dictionary<string, int>();

    foreach (string component in COMPONENTS)
    {
        InspectInventory(component);
    }

    PrintInventory();

    counter++;
}

public void InspectInventory(string component_name)
{
    MyItemType component_type = new MyItemType("MyObjectBuilder_Component", component_name.Replace(" ", ""));
    var containers = GetContainers();

    foreach (IMyCargoContainer container in containers)
    {
        IMyInventory inv = container.GetInventory();
        var items = new List<MyInventoryItem>();
        inv.GetItems(items);
        foreach (MyInventoryItem item in items)
        {
            HandleItem(item);
        }
    }
}

public void HandleItem(MyInventoryItem item)
{
    string[] item_type_split = item.Type.ToString().Split('/');
    string item_name = item_type_split[1];
    try
    {
        items_in_stock.Add(item_name, item.Amount.ToIntSafe());
    }
    catch (ArgumentException)
    {
        items_in_stock[item_name] += item.Amount.ToIntSafe();
    }
}

public List<IMyTerminalBlock> GetContainers()
{
    var containers = new List<IMyTerminalBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyCargoContainer>(containers);

    return containers;
}

public void SetupLCD()
{
    IMyTextPanel lcd = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(LCD_NAMES_PANEL);
    
    lcd.WriteText("", false);
    lcd.Font = FONT_FAMILY;
    lcd.FontColor = Color.Green;
    lcd.FontSize = FONT_SIZE;
}

public void PrintInventory()
{
    foreach( KeyValuePair<string, int> item in items_in_stock )
    {
        PrintItemToLCD(item.Key, item.Value);
    }
}

public void PrintItemToLCD(string item_name, int amount)
{
    string text = item_name + amount.ToString().PadLeft(50 - item_name.Length) + "\n";

    IMyTextPanel lcd_names = (IMyTextPanel)GridTerminalSystem.GetBlockWithName(LCD_NAMES_PANEL);
    lcd_names.WriteText(text, true);
}
