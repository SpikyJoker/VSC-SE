
// Component Names and Settings
string topPistonName = "[MINE] Piston Top";
string grabPistonName = "[MINE] Piston Grab";
string welderGroupName = "[MINE] Welder Top";
string drillProjectorName = "[MINE] Projector Top";
string conveyorProjectorName = "[MINE] Projector Conveyor";
string topMergeBlockName = "[MINE] Merge Top";
string grabMergeBlockName = "[MINE] Merge Grab";
string lcdScreenName = "[MINE] LCD Screen";

float topPistonSpeed = 5.0f;
float grabPistonSpeed = 2.0f;
float pistonTopExtendLimit = 9.9f;
float pistonTopConnectLimit = 2.5f;
float pistonRetractSpeed = 5.0f;
float pistonGrabExtendLimit = 2.3f;
float drillPistonSpeed = 0.1f;

// System Components
IMyPistonBase topPiston;
IMyPistonBase grabPiston;
IMyBlockGroup welderGroup;
IMyProjector drillprojector;
IMyProjector conveyorProjector;
IMyShipMergeBlock topMergeBlock;
IMyShipMergeBlock grabMergeBlock;
IMyTextPanel lcdScreen;

// Controllers
PistonController pistonController;
MergeController mergeController;
ProjectionController projectionController;
WelderController welderController;


//State variables
bool running = false
bool direction = ""

// Interfaces
public interface IPistonController
{
    void ExtendPiston(IMyPistonBase piston, float limit, float speed);
    void RetractPiston(IMyPistonBase piston, float speed);
    void IsPistonExtended(IMyPistonBase piston, float limit);
}

public interface IMergeController
{
    bool CheckMergeEngaged(IMyShipMergeBlock mergeBlock);
    void ToggleMergeBlock(IMyShipMergeBlock mergeBlock, bool status);
}

public interface IProjectionController
{
    bool IsProjectionComplete(IMyProjector projector);
    void EnableProjector(IMyProjector projector, bool status);
}

public interface IWelderController
{
    void ToggleWelders(IMyBlockGroup welderGroup, bool status);
}

// Service Implementations
public class PistonController : IPistonController
{
    private Program _program;

    public PistonController(Program program)
    {
        _program = program;
    }

    public void ExtendPiston(IMyPistonBase piston, float limit, float speed)
    {
        piston.MaxLimit = limit;
        piston.Velocity = speed;
        _program.WriteToScreen($"Extending piston {piston.CustomName} to {limit}m at {speed}m/s");
    }

    public void RetractPiston(IMyPistonBase piston, float speed)

    {
        piston.Velocity = -speed;
        _program.WriteToScreen($"Retracting piston {piston.CustomName} at {speed}m/s");
    }
    public void IsPistonExtended(IMyPistonBase piston, float limit)
    {
        if (piston.CurrentPosition == limit)
        {
            _program.WriteToScreen($"Piston {piston.CustomName} is extended");
        }
    }
}

public class MergeController : IMergeController
{
    private Program _program;

    public MergeController(Program program)
    {
        _program = program;
    }

    public bool CheckMergeEngaged(IMyShipMergeBlock mergeBlock)
    {
        bool engaged = !mergeBlock.IsConnected;
        _program.WriteToScreen($"Merge block {mergeBlock.CustomName} is {(engaged ? "engaged" : "disengaged")}");
        return engaged;
    }

    public void ToggleMergeBlock(IMyShipMergeBlock mergeBlock, bool status)
    {
        mergeBlock.Enabled = status;
        _program.WriteToScreen($"Merge block {mergeBlock.CustomName} is now {(status ? "engaged" : "disengaged")}");
    }
}

public class ProjectionController : IProjectionController
{
    private Program _program;

    public ProjectionController(Program program)
    {
        _program = program;
    }

    public bool IsProjectionComplete(IMyProjector projector)
    {
        bool isComplete = projector.IsProjecting && projector.RemainingBlocks == 0;
        _program.WriteToScreen($"Projection on {projector.CustomName} is complete: {isComplete}");
        return isComplete;
    }

    public void EnableProjector(IMyProjector projector, bool status)
    {
        projector.Enabled = status;
        _program.WriteToScreen($"Projector {projector.CustomName} is now {(status ? "enabled" : "disabled")}");
    }
}

public class WelderController : IWelderController
{
    private Program _program;

    public WelderController(Program program)
    {
        _program = program;
    }

    public void ToggleWelders(IMyBlockGroup welderGroup, bool status)
    {
        // Ensure welderGroup is not null
        if (welderGroup != null)
        {
            List<IMyTerminalBlock> welders = new List<IMyTerminalBlock>();
            welderGroup.GetBlocks(welders);  // Populate welders list with blocks in the group

            // Iterate through the blocks and enable/disable them
            foreach (IMyTerminalBlock block in welders)
            {
                IMyShipWelder welder = block as IMyShipWelder;
                if (welder != null)  // Check if block is a welder
                {
                    welder.Enabled = status;  // Enable or disable the welder
                }
            }

            _program.WriteToScreen($"Welders in group {welderGroup.Name} are now {(status ? "enabled" : "disabled")}");
        }
        else
        {
            _program.WriteToScreen($"No group found with name {welderGroup.Name}");
        }
    }
}

enum MiningState
{
    Idle,
    Initialize,
    PrintDrill,
    PrintConveyor
}

MiningState currentMiningState = MiningState.Idle;
int currentStep = 0;

void PrintDrillSteps()
{
    switch (currentStep)
    {
        case 0:
            WriteToScreen("Checking if grab merge is necessary");
            // Check if the grab merge has already been engaged and therefore the drill doesn't need to be printed
            if (mergeController.IsMergeBlockEngaged(grabMergeBlock))
            {
                currentMiningState = MiningState.PrintConveyor;
            }
            else
            {
                currentStep++;
            }
            break;

        case 1:
            // Create drill section
            WriteToScreen("Enable projector, and welder");
            projectionController.EnableProjector(drillProjector, true);
            welderController.ToggleWelders(welderGroup, true);
            currentStep++;
            break;

        case 2:
            // Check drill section is complete
            WriteToScreen("Checking if drill section is complete");
            if (projectionController.IsProjectionComplete(conveyorProjector))
            {
                currentStep++;
            }
            else
            {
                projectionController.EnableProjector(conveyorProjector, false);
                welderController.ToggleWelders(welderGroup, false);
                break;
            }
            break;

        case 3:
            // Extend top piston to connect drill section
            WriteToScreen("Extending top piston to connect drill section");
            pistonController.ExtendPiston(topPiston, pistonTopExtendLimit, topPistonSpeed);
            currentStep++;
            break;

        case 4:
            // Check if top piston is extended
            WriteToScreen("Checking if top piston is extended");
            if (pistonController.IsPistonExtended(topPiston, pistonTopExtendLimit))
            {
                currentStep++;
            }
            else
            {
                break;
            }
            break;

        default:
            // Reset or move to the next state
            currentMiningState = MiningState.PrintConveyor;
            currentStep = 0;
            break;
    }
    // This section is run every iteration no matter what, great place for status updates and integrity checks
}

void PrintConveyorSteps()
{
    switch (currentStep)
    {
        case 0:
            WriteToScreen("Moving all blocks to starting positions");
            pistonController.RetractPiston(topPiston, pistonRetractSpeed);
            pistonController.ExtendPiston(grabPiston, pistonGrabExtendLimit, grabPistonSpeed);
            currentStep++;
            break;

        case 1:
            // Check if grab piston is extended
            WriteToScreen("Checking if pistons are extended");
            if (pistonController.IsPistonExtended(grabPiston, pistonGrabExtendLimit) && pistonController.IsPistonExtended(topPiston, 0.0f))
            {
                mergeController.ToggleMergeBlock(topMergeBlock, true);
                currentStep++;
            }
            else
            {
                break;
            }
            break;

        case 2:
            // Engage grab merge block
            WriteToScreen("Ensuring grab merge block is still on");
            mergeController.ToggleMergeBlock(grabMergeBlock, true);
            currentStep++;
            break;

        case 3:
            // Check if grab merge block is engaged
            WriteToScreen("Checking if grab merge block is engaged");
            if (mergeController.CheckMergeEngaged(grabMergeBlock))
            {
                currentStep++;
            }
            else
            {
                break;
            }
            break;

        case 4:
            // Drill projector is off, conveyor projector is on
            WriteToScreen("Turning off drill projector and turning on conveyor projector");
            projectionController.EnableProjector(drillProjector, false);
            projectionController.EnableProjector(conveyorProjector, true);
            currentStep++;
            break;

        case 5:
            // Create drill section
            WriteToScreen("Enable projector, and welder");
            projectionController.EnableProjector(drillProjector, true);
            welderController.ToggleWelders(welderGroup, true);
            currentStep++;
            break;

        case 6:
            // Check drill section is complete
            WriteToScreen("Checking if conveyor section is complete");
            if (projectionController.IsProjectionComplete(conveyorProjector))
            {
                currentStep++;
            }
            else
            {
                projectionController.EnableProjector(conveyorProjector, false);
                welderController.ToggleWelders(welderGroup, false);
                break;
            }
            break;

        case 7:
            // Extend top piston to connect drill section
            WriteToScreen("Extending top piston to connect drill section");
            pistonController.ExtendPiston(topPiston, pistonTopConnectLimit, drillPistonSpeed);
            currentStep++;
            break;

        case 8:
            // Check if top piston is extended
            WriteToScreen("Waiting for top piston to be extended");
            if (pistonController.IsPistonExtended(topPiston, pistonTopConnectLimit))
            {
                currentStep++;
            }
            else
            {
                break;
            }
            break;

        case 9:
            // Disconnect grab merge block
            WriteToScreen("Disengaging grab merge block");
            mergeController.ToggleMergeBlock(grabMergeBlock, false);
            currentStep++;
            break;

        case 10:
            // Extend top piston deeper
            WriteToScreen("Extending top piston deeper");
            pistonController.ExtendPiston(topPiston, pistonTopExtendLimit, topPistonSpeed);
            currentStep++;
            break;

        case 11:
            // Check if top piston is extended
            WriteToScreen("Checking if top piston is extended");
            if (pistonController.IsPistonExtended(topPiston, pistonTopExtendLimit))
            {
                currentStep++;
            }
            else
            {
                break;
            }
            break;

        case 12:
            // Reconnect grab merge
            WriteToScreen("Reconnecting grab merge block");
            mergeController.ToggleMergeBlock(grabMergeBlock, true);
            currentStep++;
            break;

        default:
            // Reset or move to the next state
            currentMiningState = MiningState.PrintConveyor;
            currentStep = 0;
            break;
    }
    // This section is run every iteration no matter what, great place for status updates and integrity checks
}

// Main Entry Point
public void Main(string argument)
{
    switch (MiningState)
    {
        case MiningState.Idle:
            break;
        case MiningState.Initialize:
            pistonLeft.Velocity = 0.1f;

        case MiningState.PrintDrill:
            break;
        case MiningState.PrintConveyor:
            break;
    }
}

public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100;
}


// Helper Methods
void WriteToScreen(string message)
{
    int maxLines = 23; // Maximum number of lines to display on the LCD
    string statusText = $"Running: {running}\nDirection: {direction}\n"; // Status lines for the top of the screen

    if (lcdScreen != null)
    {
        // Reset the screen if the message is "reset"
        if (message == "reset")
        {
            lcdScreen.WriteText(statusText, false); // Clear and add only status lines
        }
        else
        {
            // Retrieve current text without the status lines
            string currentText = lcdScreen.GetText();
            var lines = currentText.Split(new[] { '\n' }, StringSplitOptions.None).Skip(3).ToList();

            // If there are more lines than maxLines - 3 (account for status lines), remove the oldest lines
            if (lines.Count >= maxLines - 3)
            {
                lines = lines.Skip(lines.Count - (maxLines - 3) + 1).ToList();
            }

            // Add the new message to the log
            if (!string.IsNullOrEmpty(message))
            {
                lines.Add(message);
            }

            // Combine status lines and message log
            string updatedText = statusText + string.Join("\n", lines);
            lcdScreen.WriteText(updatedText);
        }
    }
    else
    {
        Echo("LCD Screen is not initialized."); // Debugging statement
    }
}