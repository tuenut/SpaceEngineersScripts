string CONVEYOR_SORTER_IN_PREFIX = "conveyor_sorter.in.";

public IMyConveyorSorter get_conveyor(string component_name)
{
    string snake_case_component_name = component_name.ToLower().Replace(' ', '_');
    string conveyor_sorter_name = CONVEYOR_SORTER_IN_PREFIX + snake_case_component_name;

    IMyConveyorSorter conveyor_sorter = (IMyConveyorSorter)GridTerminalSystem.GetBlockWithName(conveyor_sorter_name);

    return conveyor_sorter;
}

public void setup_conveyors()
{
    foreach (string component in COMPONENTS)
    {
        IMyConveyorSorter conveyor_sorter = get_conveyor(component);
        conveyor_sorter.DrainAll = false;
    }
}

public void switch_conveyor_sorter(int amount, IMyCargoContainer container, string component_name)
{
    var suffix = component_name.ToLower().Replace(' ', '_');
    IMyConveyorSorter conveyor_sorter = get_conveyor(component_name);

    if ((amount > 0) && (!container.CustomName.Contains(suffix)))
    {
        conveyor_sorter.DrainAll = true;
    }
}
