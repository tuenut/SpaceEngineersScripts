public class ProgramKlass { // для вставки в программируемый блок в игре, убрать эту строку и последнюю

public const string OWNER_PREFIX = "tu"; // just prefix in block name
public const string GRID_NAME_PREFIX = "ms-01"; // set to your own name (not real grid name for now)
public const string OUTPUT_LCD = "display"; // suffix of output lsd name
public const int COCKPIT_DISPLAY = 0; // set to your own display index if use cockpit displays

public static string CURRENT_PREFIX = $"{OWNER_PREFIX}.{GRID_NAME_PREFIX}"; // use grid prefix while it not resolving dynamically


public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save() {}

public void Main(string argument, UpdateType updateSource) {
    Echo("Run!");

    Dictionary<string, List<float>> powerSourcesData = CollectPowerSourcesData();
    DisplayData(powerSourcesData);
}

public Dictionary<string, List<float>> CollectPowerSourcesData()  {
    // Collect data from power sources. Just all, not filter by name prefixes

    Echo("Collect Data");

    Dictionary<string, List<float>> energyProducers = new Dictionary<string, List<float>> {};
    
    List<IMyPowerProducer> powerProducers = new List<IMyPowerProducer>();

    GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(powerProducers);

    foreach (IMyPowerProducer powerProducerBlock in powerProducers) {
        if (powerProducerBlock.IsWorking) {
            string key = powerProducerBlock.BlockDefinition.TypeId.ToString().Split('_')[1];

            try{
                energyProducers[key][0] = energyProducers[key][0] + powerProducerBlock.CurrentOutput;
                energyProducers[key][1] = energyProducers[key][1] + powerProducerBlock.MaxOutput;
            } catch (KeyNotFoundException) {
                energyProducers[key] = new List<float> {powerProducerBlock.CurrentOutput, powerProducerBlock.MaxOutput};
            }
        }
    }

    return energyProducers;
}

public void DisplayData(Dictionary<string, List<float>> data) {
    // prepare data to output and print on panel

    Echo("Display Data");

    IMyTextSurface outputLCD = (IMyTextSurface) GetPanel();
    if (outputLCD == null) {
        outputLCD = GetSurfaceFromCockpit();
    }
    if (outputLCD == null) {
        Echo("Displays not found");
        return;
    }

    float totalCurrentOutput = 0.0f;
    float totalMaxOutput = 0.0f;

    outputLCD.WriteText("", false);
    foreach (KeyValuePair<string,  List<float>> powerSource in data) {
        totalCurrentOutput = totalCurrentOutput + powerSource.Value[0];
        totalMaxOutput = totalMaxOutput + powerSource.Value[1];

        outputLCD.WriteText($"{powerSource.Key}: {powerSource.Value[0]:F2}/{powerSource.Value[1]:F2} MW\n", true);
    }
    outputLCD.WriteText($"Total: {totalCurrentOutput:F2}/{totalMaxOutput:F2} MW", true);
}

public IMyTextPanel GetPanel() {
    // find panel with $"{CURRENT_PREFIX}.{OUTPUT_LCD}"
    Echo("Try to found panel...");

    List<IMyTextPanel> textProviders = new List<IMyTextPanel>();

    GridTerminalSystem.GetBlocksOfType<IMyTextPanel>(textProviders);
    IMyTextPanel lcd = textProviders.Find(lcdSurface => lcdSurface.CustomName.EndsWith($"{CURRENT_PREFIX}.{OUTPUT_LCD}"));

    if (lcd != null) {
        Echo("Found panel.");
    } else {
        Echo("Can not found suitable panel.");
    }

    return lcd;
}

public IMyTextSurface GetSurfaceFromCockpit() {
    Echo("Try to found cockpit...");

    IMyCockpit cockpit = (IMyCockpit) GridTerminalSystem.GetBlockWithName($"{CURRENT_PREFIX}.{OUTPUT_LCD}");
    

    if (cockpit != null) {
        Echo("Found panel.");
        
        IMyTextSurface outputLCD = (IMyTextSurface) cockpit.GetSurface(COCKPIT_DISPLAY);
        
        return outputLCD;
    } else {
        Echo("Can not found suitable panel.");
        return null;
    }
}

public bool _TextSurfacesOutputPredicate(IMyTextPanel lcdSurface){
    return !lcdSurface.CustomName.StartsWith(CURRENT_PREFIX);
}

}