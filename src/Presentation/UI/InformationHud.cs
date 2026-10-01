using DevAncientNaval.Core.Battle;
using DevAncientNaval.Core.Grid;
using DevAncientNaval.Core.Units;
using DevAncientNaval.Core.Vision;
using System.Linq;
using Godot;
using Side = DevAncientNaval.Core.Units.Side;

namespace DevAncientNaval.Presentation.UI;
public partial class DebugHud
{
    private PanelContainer _informationPanel = null !;
    private Label _informationTitle = null !, _informationText = null !;
    private SectorButton _information = null !;
    private GridPosition? _inspectionCell;
    private BattleState? _informationBattle;
    private string _loreTitle = "", _loreText = "";
    public bool InformationVisible => _informationPanel?.Visible == true;
    public string InformationText => _loreText;

    private void BuildInformation()
    {
        _information = IconButton("ActionInformation", ActionSymbol.Information, "Read the chart", OpenInformation);
        _informationPanel = Panel(_root);
        _informationPanel.Name = "InformationScroll";
        _informationPanel.CustomMinimumSize = new(368, 0);
        _informationPanel.Resized += () =>
        {
            _layoutSizes = null;
            Layout();
        };
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        _informationPanel.AddChild(column);
        var heading = new HBoxContainer();
        column.AddChild(heading);
        _informationTitle = Label("", 20);
        _informationTitle.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        heading.AddChild(_informationTitle);
        var close = TextButton("×", () => _informationPanel.Hide());
        close.Name = "CloseInformation";
        close.CustomMinimumSize = new(34, 30);
        heading.AddChild(close);
        _informationText = Label("", 15);
        _informationText.CustomMinimumSize = new(336, 0);
        _informationText.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_informationText);
        _informationPanel.Hide();
    }

    private void UpdateInformation(BattleState battle, Ship? selected, Village? village)
    {
        _informationBattle = battle;
        if (selected is not null)
        {
            _loreTitle = selected.Name;
            _loreText = AncientLore.Ship(battle, selected);
        }
        else if (village is not null)
        {
            _loreTitle = "Coastal village";
            _loreText = AncientLore.Village(battle, village);
        }
        else if (_inspectionCell is { } cell && battle.Board.Contains(cell) && battle.Vision.IsExplored(Side.Player, cell))
        {
            if (battle.Vision.State(Side.Player, cell) == VisibilityState.RadarContact)
            {
                _loreTitle = "Distant contact";
                _loreText = "A mark on the chart tells you where, never who. Bring a lookout nearer before judging the vessel's strength.";
            }
            else
                (_loreTitle, _loreText) = AncientLore.Cell(battle, cell);
            _ship.Text = _loreTitle;
            _health.Text = "";
            _details.Text = "Read the chart for the keeper's counsel.";
            _shipCard.Show();
        }
        else
        {
            _loreTitle = "";
            _loreText = "";
        }

        _information.SetMeta("applicable", _loreText.Length > 0);
        Availability(_information, _loreText.Length > 0, "Info");
        if (_informationPanel.Visible)
        {
            _informationTitle.Text = _loreTitle;
            _informationText.Text = _loreText;
            if (_loreText.Length == 0)
                _informationPanel.Hide();
        }
    }

    private void OpenInformation()
    {
        if (_loreText.Length == 0)
            return;
        _informationTitle.Text = _loreTitle;
        _informationText.Text = _loreText;
        _informationPanel.Visible = !_informationPanel.Visible;
        _informationPanel.ResetSize();
        _layoutSizes = null;
        Layout();
    }

    public void ShowInformation(BattleState battle, GridPosition cell)
    {
        ShowTile(battle, cell);
        UpdateInformation(battle, battle.ObservedAt(Side.Player, cell), battle.ObservedVillages(Side.Player).FirstOrDefault(v => v.Position == cell));
        OpenInformation();
    }
}
