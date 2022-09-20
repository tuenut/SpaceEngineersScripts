public class ProgramKlass { // для вставки в программируемый блок в игре, убрать эту строку и последнюю

public const string FONT_FAMILY = "Monospace";
public const float FONT_SIZE = 1.5f;

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save() {}

public void Main(string argument, UpdateType updateSource) {
    IMyCockpit cockpit = (IMyCockpit) GridTerminalSystem.GetBlockWithName("cockpit.main");
    IMyTextSurface lcd = (IMyTextSurface) cockpit.GetSurface(0);

    lcd.Font = FONT_FAMILY;
    lcd.FontSize = FONT_SIZE;
    lcd.WriteText("", false);

    float charge = 0.0f;
    float maxCharge = 0.0f;
    float input = 0.0f;
    float output = 0.0f;

    List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
    GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batteries);

    foreach (IMyBatteryBlock battery in batteries) {
        if (battery.IsWorking && (battery.CubeGrid.DisplayName ==  "tu.worker.moduled.1")) {
            charge = charge + battery.CurrentStoredPower;
            maxCharge = maxCharge + battery.MaxStoredPower;
            input = input + battery.CurrentInput;
            output = output + battery.CurrentOutput;
        }
    }

    float remainingTime = (charge / Math.Abs(output - input) * 60);

    if (input > output) {
        lcd.WriteText($"Status: Charging\n", true);
        remainingTime = ((maxCharge - charge) / Math.Abs(output - input) * 60);
    } else if (input == output) {
        lcd.WriteText($"Status: Equilibrium\n", true);
    } else {
        lcd.WriteText($"Status: Discharging\n", true);
    }
    
    lcd.WriteText($"Remaining:\n", true);
    lcd.WriteText($"{(charge/maxCharge*100):F2}%\n", true);
    lcd.WriteText($"{charge:F2}/{maxCharge:F2}MW\n", true);
    lcd.WriteText($"{remainingTime:F2} min", true);
}

}
