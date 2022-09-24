public class ProgramKlass { // для вставки в программируемый блок в игре, убрать эту строку и последнюю

public const string SUBCOMMAND_SEPARATOR = ".";
public const string LADDER_CONTROL = "LADDER";
public const string LADDER_UP_CMD = "UP";
public const string LADDER_DOWN_CMD = "DOWN";

public Program() {
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}

public void Save() {}

public void Main(string argument, UpdateType updateSource) {
    // TODO check grid configuration

    string[] command = argument.Split('.');
    // TODO validate
    if (argument.Length == 0) {
        return;
    }

    switch (command[0]) {
        case LADDER_CONTROL:
            LadderControl(command);
            break;
    }    
}

public void LadderControl(string[] command) {
    string subCommand = command[1];
    List<IMyMotorAdvancedStator> hinges = GetLadderHingesGroup();

    switch (subCommand) {
        case LADDER_UP_CMD:
            LadderUp(hinges);
            break;
        case LADDER_DOWN_CMD:
            LadderDown(hinges);
            break;
    }
}

public void LadderUp(List<IMyMotorAdvancedStator> hinges) {
    foreach (IMyMotorAdvancedStator hinge in hinges) {
        hinge.TargetVelocityRPM = 3;
    }
}

public void LadderDown(List<IMyMotorAdvancedStator> hinges) {
    foreach (IMyMotorAdvancedStator hinge in hinges) {
        hinge.TargetVelocityRPM = -3;
    }
}

public List<IMyMotorAdvancedStator> GetLadderHingesGroup() {
    IMyBlockGroup group = GridTerminalSystem.GetBlockGroupWithName("ladder hinges");

    List<IMyMotorAdvancedStator> hinges = new List<IMyMotorAdvancedStator>();
    group.GetBlocksOfType(hinges);

    return hinges;
}

}