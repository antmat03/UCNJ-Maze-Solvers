using Godot;

public partial class AgentWeightsSelector : VBoxContainer
{
    [Export]
    public string AgentName = "";

    [Export]
    public float DefaultCoinWeight = 15.0f;
    [Export]
    public float DefaultFloorWeight = 40.0f;
    [Export]
    public float DefaultHazardWeight = 80.0f;

    [ExportGroup("UIRefs")]
    [Export]
    public Label NameLabel;
    [Export]
    public SpinBox CoinBox;
    [Export]
    public SpinBox FloorBox;
    [Export]
    public SpinBox HazardBox;
    

    public override void _Ready()
    {
        base._Ready();
        NameLabel.Text = $"{AgentName} AGENT";

        CoinBox.Value = DefaultCoinWeight;
        FloorBox.Value = DefaultFloorWeight;
        HazardBox.Value = DefaultHazardWeight;
    }

    public void Restore(MainMenu.Weights weights)
    {
        CoinBox.Value = weights.Coin;
        FloorBox.Value = weights.Floor;
        HazardBox.Value = weights.Hazard;
    }
}