// Interfaces
public interface IPistonController
{
    void ExtendPiston(IMyPistonBase piston, float limit, float speed);
    void RetractPiston(IMyPistonBase piston, float speed);
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
// Define state machine states
enum MiningState
{
    Idle,
    Initialize,
    PrintConveyor,
    PrintDrill,
    Retract,
    Complete
}

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
IMyProjector projector;
IMyProjector conveyorProjector;
IMyShipMergeBlock topMergeBlock;
IMyShipMergeBlock grabMergeBlock;
IMyTextPanel lcdScreen;

// Controllers
PistonController pistonController;
MergeController mergeController;
ProjectionController projectionController;
WelderController welderController;

// State and flags
MiningState state = MiningState.Idle;
bool initialized = false;

// Program Setup
public Program()
{
    Runtime.UpdateFrequency = UpdateFrequency.Update100; // Update every 100 ticks
    InitializeSystem();
}

// Main Entry Point
public void Main(string argument)
{
    switch (argument)
    {
        case "DigConveyor":
            state = MiningState.PrintConveyor;
            break;
        case "DigDrill":
            state = MiningState.PrintDrill;
            break;
        case "Retract":
            state = MiningState.Retract;
            break;
        case "Stop":
            state = MiningState.Idle;
            StopAll();
            break;
    }

    ExecuteState();
}

// System Initialization
void InitializeSystem()
{
    // Fetch and validate blocks
    lcdScreen = GridTerminalSystem.GetBlockWithName(lcdScreenName) as IMyTextPanel;
    topPiston = GridTerminalSystem.GetBlockWithName(topPistonName) as IMyPistonBase;
    grabPiston = GridTerminalSystem.GetBlockWithName(grabPistonName) as IMyPistonBase;
    welderGroup = GridTerminalSystem.GetBlockGroupWithName(welderGroupName);
    projector = GridTerminalSystem.GetBlockWithName(drillProjectorName) as IMyProjector;
    conveyorProjector = GridTerminalSystem.GetBlockWithName(conveyorProjectorName) as IMyProjector;
    topMergeBlock = GridTerminalSystem.GetBlockWithName(topMergeBlockName) as IMyShipMergeBlock;
    grabMergeBlock = GridTerminalSystem.GetBlockWithName(grabMergeBlockName) as IMyShipMergeBlock;

    // Initialize controllers
    pistonController = new PistonController(this);
    mergeController = new MergeController(this);
    projectionController = new ProjectionController(this);
    welderController = new WelderController(this);

    // Ensure system is in the correct starting state
    ResetSystemState();

    initialized = true;
    WriteToScreen("System initialized successfully.");
}

// Ensures all components are in the default state at the start
void ResetSystemState()
{
    mergeController.ToggleMergeBlock(topMergeBlock, false);
    mergeController.ToggleMergeBlock(grabMergeBlock, true);
    
    pistonController.RetractPiston(topPiston, pistonRetractSpeed);
    mergeController.ToggleMergeBlock(topMergeBlock, true);
    
    projectionController.EnableProjector(projector, false);
    projectionController.EnableProjector(conveyorProjector, false);
    
    welderController.ToggleWelders(welderGroup, false);
}

// Executes the current state action
void ExecuteState()
{
    if (!initialized) return;

    switch (state)
    {
        case MiningState.PrintConveyor:
            PrintConveyorPiece();
            break;
        case MiningState.PrintDrill:
            PrintDrillPiece();
            break;
        case MiningState.Retract:
            RetractSystem();
            break;
        case MiningState.Complete:
            StopAll();
            state = MiningState.Idle;
            break;
    }
}

// Refactored PrintConveyorPiece logic
void PrintConveyorPiece()
{
    WriteToScreen("Printing conveyor piece...");
    if (!mergeController.CheckMergeEngaged(grabMergeBlock))
    {
        projectionController.EnableProjector(conveyorProjector, true);
        welderController.ToggleWelders(welderGroup, true);
        if (projectionController.IsProjectionComplete(conveyorProjector))
        {
            welderController.ToggleWelders(welderGroup, false);
            projectionController.EnableProjector(conveyorProjector, false);
            pistonController.ExtendPiston(topPiston, pistonTopConnectLimit, drillPistonSpeed);
            mergeController.ToggleMergeBlock(grabMergeBlock, false);
            pistonController.ExtendPiston(topPiston, pistonTopExtendLimit, drillPistonSpeed);
            mergeController.ToggleMergeBlock(topMergeBlock, false);
            pistonController.RetractPiston(topPiston, pistonRetractSpeed);
            state = MiningState.Complete;
        }
    }
}

// Refactored PrintDrillPiece logic
void PrintDrillPiece()
{
    WriteToScreen("Printing drill piece...");
    if (!mergeController.CheckMergeEngaged(grabMergeBlock))
    {
        projectionController.EnableProjector(projector, true);
        welderController.ToggleWelders(welderGroup, true);
        if (projectionController.IsProjectionComplete(projector))
        {
            welderController.ToggleWelders(welderGroup, false);
            projectionController.EnableProjector(projector, false);
            pistonController.ExtendPiston(topPiston, pistonTopExtendLimit, topPistonSpeed);
            pistonController.ExtendPiston(grabPiston, pistonGrabExtendLimit, grabPistonSpeed);
            mergeController.ToggleMergeBlock(topMergeBlock, false);
            pistonController.RetractPiston(topPiston, pistonRetractSpeed);
            state = MiningState.Complete;
        }
    }
}

// Retract System to initial position
void RetractSystem()
{
    WriteToScreen("Retracting system...");
    pistonController.RetractPiston(topPiston, pistonRetractSpeed);
    mergeController.ToggleMergeBlock(grabMergeBlock, true);
    mergeController.ToggleMergeBlock(topMergeBlock, false);
    state = MiningState.Complete;
}

// Stop all actions and reset components
void StopAll()
{
    WriteToScreen("Stopping all actions...");
    projectionController.EnableProjector(projector, false);
    projectionController.EnableProjector(conveyorProjector, false);
    welderController.ToggleWelders(welderGroup, false);
    pistonController.RetractPiston(topPiston, pistonRetractSpeed);
}

// Write messages to the LCD
void WriteToScreen(string message)
{
    if (lcdScreen != null)
    {
        lcdScreen.WriteText(message + "\n", true);
    }
    else
    {
        Echo("LCD Screen not found");
    }
}
